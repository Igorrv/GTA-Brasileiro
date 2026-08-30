using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Veículo dirigível (docs/04). Física real: Rigidbody + 4 <see cref="WheelCollider"/> com
    /// tração traseira, e cada modelo do catálogo dirige diferente de verdade:
    ///
    ///  • <b>Curvas de atrito</b> por eixo — a traseira segura menos que a dianteira, então dá pra
    ///    sair de traseira; a rigidez lateral escala com a <c>dirigibilidade</c> do JSON.
    ///  • <b>Direção Ackermann</b> (a roda de dentro esterça mais que a de fora) e <b>limite de esterço
    ///    por velocidade</b> — a 120 km/h o volante fica leve, como num carro de verdade.
    ///  • <b>Barra estabilizadora</b> nos dois eixos: transfere carga entre as rodas e evita o capotar
    ///    fácil que todo protótipo de WheelCollider tem.
    ///  • <b>Câmbio de 5 marchas + ré</b> com curva de torque por RPM — o torque cai no fim da marcha,
    ///    e o RPM alimenta o som do motor.
    ///  • <b>Freio de mão</b> (Ctrl/C) que trava só a traseira e derruba o atrito lateral: derrapagem.
    ///  • <b>Downforce</b> e arrasto aerodinâmico crescendo com o quadrado da velocidade.
    ///  • <b>Desvira sozinho</b> se ficar de rodas pra cima parado — sem isso o jogo trava ali.
    ///
    /// Combustível continua real e econômico: consome L/km do catálogo e o reabastecimento passa por
    /// <see cref="EconomyService.TrySpend"/> com <see cref="EconomyService.PriceFor"/> (IPC-Caos).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        private const int Wheels = 4;
        private const int FL = 0, FR = 1, RL = 2, RR = 3;

        [Header("Geometria (auto-constrói as rodas)")]
        [SerializeField] private float halfWheelBase  = 1.2f;
        [SerializeField] private float halfTrack      = 0.85f;
        [SerializeField] private float wheelRadius    = 0.38f;
        [SerializeField] private float suspensionDist = 0.22f;

        [Header("Motor e transmissão")]
        [SerializeField] private float torqueMax      = 1200f;   // N·m na roda, no pico
        [SerializeField] private float rpmMin         = 900f;
        [SerializeField] private float rpmMax         = 6200f;
        [SerializeField] private float[] marchas      = { 3.6f, 2.1f, 1.45f, 1.1f, 0.85f };
        [SerializeField] private float relacaoFinal   = 3.4f;
        [SerializeField] private float freioMax       = 3200f;
        [SerializeField] private float balancoFreio   = 0.62f;   // fração no eixo dianteiro

        [Header("Direção")]
        [SerializeField] private float esterçoMax     = 30f;     // grau, parado
        [SerializeField] private float esterçoMin     = 9f;      // grau, em alta
        [SerializeField] private float taxaEsterço    = 140f;    // grau/s

        [Header("Estabilidade")]
        [SerializeField] private float rigidezBarra   = 9000f;   // barra estabilizadora (N/m)
        [SerializeField] private float downforce      = 90f;
        [SerializeField] private float arrasto        = 0.34f;

        [Header("Combustível")]
        [SerializeField] private float tankLiters      = 40f;
        [SerializeField] private float litersPerKm     = 0.10f;
        [SerializeField] private float fuelPriceRsPerL = 6.5f;
        // reserva/lâmpada amarela do painel (docs/04 §4.2)
        [SerializeField] private float reserva01       = 0.15f;
        // motor começa a engasgar antes de morrer — o carro "chia" nos últimos goles
        [SerializeField] private float engasque01      = 0.05f;
        // consumo parado (engarrafamento, docs/04 §4.3 "ocioso"): L/h de ar em marcha lenta
        [SerializeField] private float ociosoLitrosPorHora = 0.6f;

        [Header("Toque (mobile)")]
        // zona morta do joystick: no dedo tremido um eixo de 0,05 fica parecendo ruído
        [SerializeField] private float zonaMorta      = 0.08f;
        // abaixo desta velocidade o "trás" vira ré em vez de freio (km/h)
        [SerializeField] private float toleranciaReKmh = 3.5f;
        // freio-motor quando solta o acelerador: a inércia segura o carro, não deixa rolar igual sabão
        [SerializeField] private float freioMotor      = 220f;
        // quanto o carro "arranha" parado em marcha: dá a sensação de câmbio automático (km/h de arraste)
        [SerializeField] private float arrastoMarchaKmh = 1.2f;

        private readonly WheelCollider[] _wheels = new WheelCollider[Wheels];
        private readonly Transform[]     _meshes = new Transform[Wheels];
        private Rigidbody        _rb;
        private EconomyService   _econ;
        private PlayerAttributes _attrs;
        private VehicleHealth    _health;
        private float _fuel, _steer, _topKmh = 120f;
        private float _rpm, _tempoCapotado;
        private int   _marcha = 1;            // 0 = ré · 1..N = marchas à frente
        private bool  _built;
        private float _aderenciaLateral = 1.6f;

        // ---- diferenças por classe (moto/carro/caminhão) ----
        // moto é leve e frágil, caminhão é pesado e duro, carro é o meio-termo. Estes fatores
        // multiplicam consumo/freio/dano recebido e a velocidade de esterço — é o que faz a CG
        // responder num toque e a jamanta precisar antecipar a curva.
        private float _fatorCombustivelClasse = 1f;
        private float _fatorFreioClasse       = 1f;
        private float _fatorDanoClasse         = 1f;   // repassado ao VehicleHealth
        private float _taxaEstercoEfetiva;             // preenchido em ConfigureFromCatalog
        private bool  _ehCaminhao;                     // caminhão/ônibus/van: freio longo, esterço lento

        // ---- tração por superfície (asfalto procedural vs grama/areia/calçada) ----
        // a cidade é uma placa de asfalto; o que está em cima (grama do Sítio, areia da Itaúna,
        // paralelepípedo do Centro) reduz o grip. Lemos o material sob a roda e escalamos a
        // rigidez lateral — é o que faz o jogador "sentir" que saiu do asfalto.
        private float _gripSuperficie = 1f;
        private float _proximoGripScan;

        /// <summary>Qual eixo recebe torque. Muda completamente o comportamento na saída de curva.</summary>
        private enum Tracao { Traseira, Dianteira, Integral }
        private Tracao _tracao = Tracao.Traseira;
        private bool   _ehMoto;

        private static Tracao TracaoDe(VehicleDto dto)
        {
            if (dto.classe == "Caminhonete" && dto.id != null && dto.id.Contains("4x4")) return Tracao.Integral;
            if (dto.classe == "Caminhonete" || dto.classe == "Caminhao" ||
                dto.classe == "Onibus"      || dto.classe == "Rural")   return Tracao.Traseira;
            if (dto.classe == "Esportivo")                               return Tracao.Traseira;
            if (dto.classe == "Moto" || dto.classe == "Bicicleta")      return Tracao.Traseira;
            // popular/táxi/app: os nacionais de hoje são dianteira
            return dto.raridade >= 3 ? Tracao.Traseira : Tracao.Dianteira;
        }

        public float SpeedKmh => _rb ? _rb.linearVelocity.magnitude * 3.6f : 0f;
        public float Fuel01   => tankLiters > 0f ? _fuel / tankLiters : 0f;
        public bool  IsEmpty  => _fuel <= 0.001f;
        public bool  Controlled { get; set; }   // ligado pelo PlayerVehicleLink

        // ---- combustível: aviso de reserva e engasque (docs/04 §4.2/§4.3) ----
        /// <summary>Tanque na reserva (≤15%): acende a lâmpada amarela do painel.</summary>
        public bool  Reserva       => tankLiters > 0f && _fuel <= tankLiters * reserva01;
        /// <summary>Sem gasolina: motor cortado, o carro rola até parar.</summary>
        public bool  SemCombustivel => _fuel <= 0f;
        /// <summary>Motor engasgando nos últimos litros — o torque falha em rajada.</summary>
        public bool  Cortando { get; private set; }
        /// <summary>Velocidade com sinal: negativa em ré (o HUD/missionador usa para sentido).</summary>
        public float SpeedKmhComSinal => _rb ? Vector3.Dot(_rb.linearVelocity, transform.forward) * 3.6f : 0f;

        // ---- telemetria p/ HUD e áudio ----
        public float Rpm        => _rpm;
        public float Rpm01      => Mathf.InverseLerp(rpmMin, rpmMax, _rpm);
        public int   Marcha     => _marcha;
        public string MarchaTxt => _marcha == 0 ? "R" : _marcha.ToString();
        public bool  Derrapando { get; private set; }
        /// <summary>Câmbio manual ligado (o jogador tocou no câmbio pelo menos uma vez).</summary>
        public bool  Manual  { get; private set; }
        /// <summary>Motor no corte de giro — o painel acende e o som "engasga".</summary>
        public bool  NoCorte { get; private set; }

        /// <summary>
        /// Timbre do motor: 1 = tom de referência. Moto sobe quase uma oitava, caminhão desce meia.
        /// É o que faz a CG soar como CG e a jamanta soar como jamanta usando o mesmo clipe.
        /// </summary>
        public float Timbre
        {
            get
            {
                if (Modelo == null) return 1f;
                switch (Modelo.classe)
                {
                    case "Moto":        return 1.85f;
                    case "Bicicleta":   return 1f;
                    case "Caminhao":    return 0.55f;
                    case "Onibus":      return 0.62f;
                    case "Van":         return 0.80f;
                    case "Caminhonete": return 0.78f;
                    case "Esportivo":   return 1.25f;
                    case "Rural":       return 0.60f;
                    default:            return 1f;      // popular
                }
            }
        }

        // ---- acesso p/ HUD/scanner ----
        public float TankLiters => tankLiters;
        public float Fuel       => _fuel;
        public void  FillTank() => _fuel = tankLiters;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        /// <summary>Ajusta os parâmetros físicos a partir do modelo do catálogo.</summary>
        public void ConfigureFromCatalog(VehicleDto dto)
        {
            if (dto == null) return;
            Modelo      = dto;                       // timbre do motor e HUD leem daqui
            bool ehMotoAqui = dto.carroceria == "Moto" || dto.classe == "Moto";
            // moto é leve de verdade (CG ~118 kg); piso de 400 kg a faria virar um tanque. O piso
            // só protege contra catálogo mal preenchido.
            _rb.mass    = Mathf.Max(ehMotoAqui ? 80f : 400f, dto.massa);
            _rb.linearDamping    = arrasto * 0.1f;
            tankLiters  = Mathf.Max(8f, dto.tanqueL);
            litersPerKm = dto.consumoKmPorL > 0f ? 1f / dto.consumoKmPorL : litersPerKm;

            // cv → torque na roda: cv × 7 é a base, corrigida pela massa (caminhão precisa de mais)
            torqueMax = Mathf.Clamp(dto.potencia * 7f + dto.massa * 0.25f, 700f, 9000f);

            _topKmh = dto.velMaxKmh > 20f
                ? dto.velMaxKmh
                : (dto.zeroACem > 0f ? Mathf.Lerp(90f, 200f, Mathf.InverseLerp(12f, 4f, dto.zeroACem)) : 120f);

            // dirigibilidade 1..5 → esterço e aderência lateral
            float dir01 = Mathf.InverseLerp(1, 5, Mathf.Clamp(dto.dirigibilidade, 1, 5));
            esterçoMax = Mathf.Lerp(24f, 36f, dir01);
            _aderenciaLateral = Mathf.Lerp(1.15f, 2.10f, dir01);

            // entre-eixos/bitola/roda saem das dimensões: a Kombi não pode ter a pegada do Fusca,
            // e o busão precisa de 12 m entre os eixos.
            if (dto.comprimento > 0.5f) halfWheelBase = Mathf.Max(0.6f, dto.comprimento * 0.33f);
            if (dto.largura     > 0.3f) halfTrack     = Mathf.Max(0.35f, dto.largura * 0.46f);
            if (dto.altura      > 0.3f) wheelRadius   = Mathf.Clamp(dto.altura * 0.26f, 0.28f, 0.75f);

            // veículo alto e pesado rola mais: reforça a barra estabilizadora
            rigidezBarra = Mathf.Max(6000f, _rb.mass * 8f);
            freioMax     = Mathf.Max(2400f, _rb.mass * 2.4f);

            // ---- tração por época/classe ----
            // Popular moderno é dianteira (puxa e é estável); clássico e esportivo são traseiros
            // (empurram e saem de traseira); picape e caminhão, traseira também. Isso muda como o
            // carro sai da curva mais do que qualquer número de potência.
            _tracao = TracaoDe(dto);
            _ehMoto = dto.carroceria == "Moto" || dto.classe == "Moto";
            _ehCaminhao = dto.classe == "Caminhao" || dto.classe == "Onibus" ||
                          dto.classe == "Van"      || dto.classe == "Caminhonete" ||
                          dto.carroceria == "Onibus" || dto.carroceria == "Caminhao" ||
                          dto.carroceria == "Van";

            ConfigurarClasse();
        }

        /// <summary>
        /// Ajusta os fatores de classe (moto/carro/caminhão) a partir dos flags já detectados. É
        /// separado para poder rodar de novo no <see cref="TrocarModelo"/> sem recalcular a tração.
        /// </summary>
        private void ConfigurarClasse()
        {
            // taxa de esterço efetiva: moto responde num piparote, jamanta demora a virar o volante
            _taxaEstercoEfetiva = taxaEsterço;
            _fatorCombustivelClasse = 1f;
            _fatorFreioClasse       = 1f;
            _fatorDanoClasse         = 1f;

            if (_ehMoto)
            {
                _taxaEstercoEfetiva   *= 1.45f;   // ágil no toque
                _fatorCombustivelClasse = 0.65f;  // CG faz 38 km/L, consome pouco
                _fatorFreioClasse       = 0.95f;
                _fatorDanoClasse        = 1.45f;  // frágil em colisão (docs §2.7.2)
            }
            else if (_ehCaminhao)
            {
                _taxaEstercoEfetiva   *= 0.65f;   // volante pesado
                _fatorCombustivelClasse = 1.7f;   // bebe muito
                _fatorFreioClasse       = 0.82f;  // freio longo (30 m a 60 km/h, docs §2.7.4)
                _fatorDanoClasse        = 0.55f;  // durão — bate e amassa pouco
            }

            // repassa a durabilidade para a saúde (se já existir; senão Start cuida)
            if (_health != null) _health.ConfigurarClasse(_fatorDanoClasse, _ehMoto, _ehCaminhao);
        }

        /// <summary>Modelo atual — o HUD e o roubo de carro consultam.</summary>
        public VehicleDto Modelo { get; private set; }

        /// <summary>
        /// Troca o veículo por outro modelo <b>sem recriar a física</b>: a carroceria é substituída,
        /// as rodas são reposicionadas e os parâmetros vêm do novo catálogo. É isso que faz o roubo de
        /// carro funcionar — você continua sendo o mesmo Rigidbody, só que agora é uma Kombi.
        /// </summary>
        public void TrocarModelo(VehicleDto dto, Color cor)
        {
            if (dto == null) return;

            VehicleFactory.LimparCarroceria(transform);
            ConfigureFromCatalog(dto);
            VehicleFactory.BuildBodyRemovivel(transform, dto, cor, rodasVisuais: false);
            Modelo = dto;

            // reposiciona as rodas com a nova geometria (entre-eixos e bitola mudaram)
            Vector3[] pos =
            {
                new Vector3(-halfTrack, 0f,  halfWheelBase),
                new Vector3( halfTrack, 0f,  halfWheelBase),
                new Vector3(-halfTrack, 0f, -halfWheelBase),
                new Vector3( halfTrack, 0f, -halfWheelBase),
            };
            for (int i = 0; i < Wheels; i++)
            {
                if (_wheels[i] == null) continue;
                _wheels[i].transform.localPosition = pos[i];
                _wheels[i].radius = wheelRadius;
                if (_meshes[i] != null)
                    _meshes[i].localScale = new Vector3(wheelRadius * 2f, wheelRadius * 0.32f, wheelRadius * 2f);
            }

            _rb.centerOfMass = new Vector3(0f, -wheelRadius * 0.35f, halfWheelBase * 0.06f);
            _fuel   = tankLiters * Random.Range(0.25f, 0.9f);   // carro roubado vem com o tanque que vier
            _marcha = 1;
            _rpm    = rpmMin;
            Debug.Log($"[Veículo] Agora dirigindo: {dto.nome}.");
        }

        private void Start()
        {
            BuildWheels();
            // centro de massa baixo e um pouco à frente: menos capotagem, menos rabeta solta
            _rb.centerOfMass = new Vector3(0f, -wheelRadius * 0.35f, halfWheelBase * 0.06f);
            _rb.angularDamping  = 1.2f;
            _fuel = tankLiters;
            ServiceLocator.TryGet(out _econ);
            ServiceLocator.TryGet(out _attrs);
            _health = GetComponent<VehicleHealth>();

            // ConfigureFromCatalog roda antes do VehicleHealth existir (WorldBuilder monta o
            // controlador, depois a saúde), então repassamos a durabilidade da classe aqui.
            if (_health != null && _taxaEstercoEfetiva > 0f)
                _health.ConfigurarClasse(_fatorDanoClasse, _ehMoto, _ehCaminhao);
            if (_taxaEstercoEfetiva <= 0f) _taxaEstercoEfetiva = taxaEsterço;
        }

        private void FixedUpdate()
        {
            SyncWheelMeshes();
            BarraEstabilizadora(FL, FR);
            BarraEstabilizadora(RL, RR);
            Aerodinamica();

            // dirigir também cansa: marca o protagonista como ativo
            if (_attrs != null) _attrs.Ativo = Controlled && SpeedKmh > 1f;

            if (!Controlled) { Coast(); return; }

            DesviraSePrecisar();

            Vector2 m = GameInput.Move;
            // zona morta no eixo Y: no toque o dedo tremido vira ruído sem ela
            float acelerador = AplicarZonaMorta(m.y);
            float direcaoRaw = AplicarZonaMorta(m.x);
            bool  freioMao   = GameInput.Handbrake;
            bool  freioBotao = GameInput.Brake;   // pedaleira dedicada (espaço / botão mobile)

            Direcao(direcaoRaw);
            AjustarAderenciaPorSuperficie();

            // ---- intenção do piloto: acelerar, frear ou dar ré ----
            // No mobile o joystick Y faz os três papéis conforme a velocidade, e a pedaleira
            // separa o freio forte. É o esquema arcade que funciona com o polegar só.
            float velSinal = SpeedKmhComSinal;
            bool parado = Mathf.Abs(velSinal) < toleranciaReKmh;
            bool querAcelerar = acelerador > 0f;
            bool querTras     = acelerador < 0f;          // joystick pra baixo
            bool querRe      = (querTras && parado) || (freioBotao && parado && !querAcelerar);

            Transmissao(querAcelerar, querRe, velSinal);

            bool semCombustivel = _fuel <= 0f;
            bool motorQuebrado  = _health != null && _health.Broken;

            // ---- engasque: nos últimos litros o motor falha em rajada ----
            bool engasgando = !semCombustivel && !motorQuebrado && Reserva && _fuel <= tankLiters * engasque01;
            Cortando = engasgando && Random.value < 0.35f;

            if (semCombustivel || motorQuebrado)
            {
                // motor morto: NÃO trava o carro de uma vez — ele rola com o freio-motor e para sozinho,
                // como qualquer carro que ficou sem gasolina na vida real (docs/04 §4.6). Os freios
                // continuam funcionando (são mecânicos, não dependem do motor).
                _fuel = Mathf.Max(0f, _fuel);
                ApplyMotor(0f);
                if (freioBotao || (querTras && velSinal > toleranciaReKmh))
                    Frear(freioMax);
                else
                    ApplyBrake(freioMotor * (_ehCaminhao ? 1.3f : 1f));
                _rpm = Mathf.Lerp(_rpm, 0f, Time.fixedDeltaTime * 2f);
            }
            else
            {
                float fatorMotor = _health != null ? _health.FatorMotor : 1f;

                // pedaleira de freio: trava tudo, independente do sentido (é o freio de verdade)
                if (freioBotao)
                {
                    Frear(freioMax);
                    ApplyMotor(0f);
                }
                else if (querAcelerar)
                {
                    // saindo da ré: freia a marcha-atrás antes de ir pra frente
                    if (_marcha == 0 && velSinal < -0.5f) Frear(freioMax * 0.6f);
                    else
                    {
                        ApplyBrake(0f);
                        float torque = SpeedKmh > _topKmh ? 0f : acelerador * TorqueNoRpm() * fatorMotor;
                        if (Cortando) torque *= 0.15f;     // engasque: o carro "corta" o giro
                        ApplyMotor(torque);
                    }
                }
                else if (querRe)
                {
                    // ré: torque negativo na roda, proporcional ao quanto o piloto empurra pra trás
                    ApplyBrake(0f);
                    float torqueRe = -Mathf.Abs(acelerador) * TorqueNoRpm() * fatorMotor * 0.8f;
                    if (freioBotao) torqueRe = -TorqueNoRpm() * fatorMotor * 0.8f; // pedaleira = ré cheia
                    ApplyMotor(torqueRe);
                }
                else if (querTras && velSinal > toleranciaReKmh)
                {
                    // joystick pra baixo andando pra frente: freio (freio-motor + freio de atrito)
                    Frear(freioMax * 0.7f);
                    ApplyMotor(0f);
                }
                else
                {
                    // nenhum pedal: rola livre com leve freio-motor (inércia, não sabão)
                    ApplyMotor(0f);
                    ApplyBrake(freioMotor * 0.4f);
                }

                // consumo só conta com motor vivo
                ConsumeFuel(querAcelerar || querRe ? Mathf.Abs(acelerador) : 0f);
            }

            FreioDeMao(freioMao);
            InclinarSeMoto();

            if (GameInput.Refuel) Refuel();
        }

        /// <summary>Aplica zona morta simétrica num eixo -1..1 (toque não vira ruído perto do centro).</summary>
        private float AplicarZonaMorta(float v)
        {
            float a = Mathf.Abs(v);
            if (a <= zonaMorta) return 0f;
            return Mathf.Sign(v) * Mathf.InverseLerp(zonaMorta, 1f, a);
        }

        /// <summary>Freia com balanço dianteiro/traseiro e escala pela classe (caminhão freia mais devagar).</summary>
        private void Frear(float intensidade)
        {
            float f = intensidade * _fatorFreioClasse * (_health != null ? _health.FatorFreio : 1f);
            _wheels[FL].brakeTorque = _wheels[FR].brakeTorque = f * balancoFreio;
            _wheels[RL].brakeTorque = _wheels[RR].brakeTorque = f * (1f - balancoFreio);
        }

        // ------------------------------------------------------------------ direção
        private void Direcao(float entrada)
        {
            // limite de esterço cai com a velocidade: evita o "carrinho de controle remoto"
            float limite = Mathf.Lerp(esterçoMax, esterçoMin, Mathf.Clamp01(SpeedKmh / 120f));
            // dano na direção puxa o volante pra um lado (docs/04 §3.3) — o jogador precisa
            // compensar, é o feedback de que a direção foi atingida
            float puxa = _health != null ? _health.PuxaDirecao : 0f;
            float alvo = (Mathf.Clamp(entrada, -1f, 1f) + puxa) * limite;
            _steer = Mathf.MoveTowards(_steer, alvo, _taxaEstercoEfetiva * Time.fixedDeltaTime);

            // Ackermann: a roda de dentro descreve um raio menor, então esterça mais
            float rad = Mathf.Abs(_steer) * Mathf.Deg2Rad;
            if (rad > 0.001f)
            {
                float raio = halfWheelBase * 2f / Mathf.Tan(rad);
                float dentro = Mathf.Atan(halfWheelBase * 2f / (raio - halfTrack)) * Mathf.Rad2Deg;
                float fora   = Mathf.Atan(halfWheelBase * 2f / (raio + halfTrack)) * Mathf.Rad2Deg;
                float sinal  = Mathf.Sign(_steer);
                _wheels[FL].steerAngle = sinal * (sinal > 0f ? dentro : fora);
                _wheels[FR].steerAngle = sinal * (sinal > 0f ? fora   : dentro);
            }
            else
            {
                _wheels[FL].steerAngle = _wheels[FR].steerAngle = 0f;
            }
        }

        // ------------------------------------------------------------------ transmissão
        private void Transmissao(bool querAcelerar, bool querRe, float velSinal)
        {
            float velRoda = 0f;
            for (int i = 0; i < Wheels; i++) velRoda += Mathf.Abs(_wheels[i].rpm);
            velRoda /= Wheels;

            // ré quando o piloto pede trás com o carro quase parado; volta pra frente ao acelerar
            if (querRe && _marcha != 0 && velSinal < toleranciaReKmh)            _marcha = 0;
            else if (querAcelerar && _marcha == 0 && velSinal < toleranciaReKmh) _marcha = 1;

            // troca manual: assim que o jogador toca no câmbio, o automático se cala
            if (GameInput.MarchaAcima)  { Manual = true; if (_marcha < marchas.Length) _marcha++; }
            if (GameInput.MarchaAbaixo) { Manual = true; if (_marcha > 0)              _marcha--; }

            if (_marcha > 0)
            {
                float rel = marchas[Mathf.Clamp(_marcha - 1, 0, marchas.Length - 1)];
                _rpm = Mathf.Lerp(_rpm, Mathf.Clamp(velRoda * rel * relacaoFinal + rpmMin, rpmMin, rpmMax),
                                  Time.fixedDeltaTime * 6f);

                if (!Manual)
                {
                    if (_rpm > rpmMax * 0.93f && _marcha < marchas.Length) _marcha++;    // sobe marcha
                    else if (_rpm < rpmMin * 1.35f && _marcha > 1)         _marcha--;    // reduz
                }
                // no manual, esquecer de trocar bate no corte: o giro trava e o torque some
                else if (_rpm >= rpmMax * 0.985f) NoCorte = true;
                else NoCorte = false;
            }
            else
            {
                _rpm = Mathf.Lerp(_rpm, Mathf.Clamp(velRoda * 4f + rpmMin, rpmMin, rpmMax * 0.6f), Time.fixedDeltaTime * 6f);
            }
        }

        /// <summary>Curva de torque: fraco embaixo, pico no meio, cai perto do corte.</summary>
        private float TorqueNoRpm()
        {
            float t = Rpm01;
            float curva = Mathf.Clamp01(-2.6f * (t - 0.55f) * (t - 0.55f) + 1f);
            float rel = _marcha == 0 ? 3.2f : marchas[Mathf.Clamp(_marcha - 1, 0, marchas.Length - 1)];
            return torqueMax * curva * (rel / marchas[0]);
        }

        // ------------------------------------------------------------------ freio de mão / derrapagem
        private void FreioDeMao(bool ativo)
        {
            Derrapando = false;
            // a rigidez lateral da traseira combina: base × superfície × dano × (freio de mão?)
            float baseLat = _aderenciaLateral * _gripSuperficie * (_health != null ? _health.FatorAderencia : 1f);
            for (int i = RL; i <= RR; i++)
            {
                var atrito = _wheels[i].sidewaysFriction;
                atrito.stiffness = ativo ? baseLat * 0.28f : baseLat * 0.92f;
                _wheels[i].sidewaysFriction = atrito;
            }
            if (!ativo) return;

            _wheels[RL].brakeTorque = _wheels[RR].brakeTorque = freioMax * 0.9f;
            _wheels[RL].motorTorque = _wheels[RR].motorTorque = 0f;
            Derrapando = SpeedKmh > 12f;
        }

        // ------------------------------------------------------------------ estabilidade
        /// <summary>
        /// Barra estabilizadora: mede quanto cada roda do eixo está comprimida e aplica uma força
        /// proporcional à diferença. É o que impede o carro de tombar na primeira curva fechada.
        /// </summary>
        private void BarraEstabilizadora(int esq, int dir)
        {
            float compEsq = Compressao(_wheels[esq], out bool chaoEsq);
            float compDir = Compressao(_wheels[dir], out bool chaoDir);
            float forca = (compEsq - compDir) * rigidezBarra;

            if (chaoEsq) _rb.AddForceAtPosition(_wheels[esq].transform.up * -forca, _wheels[esq].transform.position);
            if (chaoDir) _rb.AddForceAtPosition(_wheels[dir].transform.up *  forca, _wheels[dir].transform.position);
        }

        private float Compressao(WheelCollider w, out bool noChao)
        {
            noChao = w.GetGroundHit(out WheelHit hit);
            if (!noChao) return 1f;
            return (-w.transform.InverseTransformPoint(hit.point).y - w.radius) / w.suspensionDistance;
        }

        private void Aerodinamica()
        {
            float v = _rb.linearVelocity.magnitude;
            if (v < 0.5f) return;
            _rb.AddForce(-transform.up * downforce * v * 0.06f);              // cola no chão em alta
            _rb.AddForce(-_rb.linearVelocity.normalized * arrasto * v * v * 0.02f); // arrasto ~ v²
        }

        /// <summary>
        /// Tração por superfície: lê o material sob a roda e escala a rigidez do <see cref="WheelCollider"/>.
        /// O asfalto procedural da cidade (placa "Pista" com <c>CaosTex_Asfalto</c>) entrega grip cheio;
        /// grama do Sítio, areia da Itaúna e paralelepípedo do Centro derrubam a aderência — é o que
        /// faz o jogador "sentir" que saiu do asfalto sem um só collider a mais. Lê só uma roda e
        /// reamostra a cada ~0,15 s: mexer em friction todo frame deixa o WheelCollider instável.
        /// </summary>
        private void AjustarAderenciaPorSuperficie()
        {
            if (Time.time < _proximoGripScan) return;
            _proximoGripScan = Time.time + 0.15f;

            float novoGrip = GripDaSuperficie(_wheels[RL]);
            if (Mathf.Approximately(novoGrip, _gripSuperficie)) return;
            _gripSuperficie = novoGrip;

            float fatorAder = _health != null ? _health.FatorAderencia : 1f;
            // atrito longitudinal: patina mais na grama/areia (arrancada "queima-pneu" fora do asfalto)
            for (int i = 0; i < Wheels; i++)
            {
                var fw = _wheels[i].forwardFriction;
                fw.stiffness = 1.6f * _gripSuperficie * fatorAder;
                _wheels[i].forwardFriction = fw;
            }
            // dianteira: a traseira é reescrita por FreioDeMao() logo abaixo, que já incorpora o grip
            for (int i = FL; i <= FR; i++)
            {
                var sw = _wheels[i].sidewaysFriction;
                sw.stiffness = _aderenciaLateral * _gripSuperficie * fatorAder;
                _wheels[i].sidewaysFriction = sw;
            }
        }

        /// <summary>Lê o material sob a roda e devolve um multiplicador de grip (1 = asfalto).</summary>
        private float GripDaSuperficie(WheelCollider w)
        {
            if (!w.GetGroundHit(out WheelHit hit) || hit.collider == null) return 1f;
            // o nome do material texturizado é "CaosTex_{Superficie}_{tx}x{ty}"; o asfalto da cidade
            // é exatamente "CaosTex_Asfalto_...". Sem renderer (pista invisível?) assume asfalto.
            var rend = hit.collider.GetComponentInParent<Renderer>();
            if (rend == null || rend.sharedMaterial == null) return 1f;
            string nome = rend.sharedMaterial.name;
            if (nome == null) return 1f;
            int i = nome.IndexOf("CaosTex_");
            if (i < 0) return 1f;
            string resto = nome.Substring(i + 8);
            int fim = resto.IndexOf('_');
            string sup = fim < 0 ? resto : resto.Substring(0, fim);
            switch (sup)
            {
                case "Asfalto":  return 1.00f;
                case "Calcada":  return 0.88f;   // paralelepípedo do Centro
                case "Grama":    return 0.78f;    // Sítio do Capim
                case "Areia":   return 0.60f;    // praia da Itaúna — patina e derrapa fácil
                case "Madeira":  return 0.85f;
                default:        return 1.00f;    // desconhecido: assume asfalto (não pune o jogador)
            }
        }

        /// <summary>
        /// Passou num buraco. O tranco é proporcional à velocidade — buraco a 20 km/h é susto, a
        /// 90 km/h quebra a suspensão. Bate no motor e joga o carro pra cima e pro lado, que é o
        /// que faz o jogador aprender a desviar (ou a xingar a prefeitura).
        /// </summary>
        public void PassarNoBuraco(float profundidade)
        {
            if (Time.time < _proximoBuraco) return;
            _proximoBuraco = Time.time + 0.35f;

            float v01 = Mathf.Clamp01(SpeedKmh / 90f);
            float forca = _rb.mass * Mathf.Lerp(0.9f, 4.5f, v01) * profundidade;

            _rb.AddForce(Vector3.up * forca, ForceMode.Impulse);
            _rb.AddTorque(transform.right * forca * 0.35f + transform.forward * Random.Range(-forca, forca) * 0.12f, ForceMode.Impulse);
            _rb.linearVelocity *= Mathf.Lerp(1f, 0.93f, v01);   // buraco também tira velocidade

            if (_health != null && v01 > 0.25f) _health.AplicarDano(Mathf.Lerp(1f, 9f, v01) * profundidade);
            BuracoSentido = Time.time;
        }

        /// <summary>Instante do último buraco — a câmera usa para dar o solavanco.</summary>
        public float BuracoSentido { get; private set; } = -99f;
        private float _proximoBuraco;

        /// <summary>De rodas pra cima e parado por 3 s: desvira. Sem isso o jogo acaba ali.</summary>
        private void DesviraSePrecisar()
        {
            if (Vector3.Dot(transform.up, Vector3.up) > 0.1f || SpeedKmh > 3f)
            {
                _tempoCapotado = 0f;
                return;
            }
            _tempoCapotado += Time.fixedDeltaTime;
            if (_tempoCapotado < 3f) return;

            _tempoCapotado = 0f;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            transform.position += Vector3.up * 1.2f;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            Debug.Log("[Veículo] Desvirado.");
        }

        private void Coast()
        {
            ApplyMotor(0f);
            ApplyBrake(freioMax * 0.05f);
            _wheels[FL].steerAngle = _wheels[FR].steerAngle = 0f;
            _rpm = Mathf.Lerp(_rpm, 0f, Time.fixedDeltaTime * 2f);
        }

        /// <summary>Distribui o torque conforme a tração do modelo.</summary>
        private void ApplyMotor(float t)
        {
            switch (_tracao)
            {
                case Tracao.Dianteira:
                    _wheels[FL].motorTorque = t; _wheels[FR].motorTorque = t;
                    _wheels[RL].motorTorque = 0f; _wheels[RR].motorTorque = 0f;
                    break;
                case Tracao.Integral:
                    // 40% na frente, 60% atrás: sai firme e ainda dá pra soltar a traseira
                    _wheels[FL].motorTorque = t * 0.4f; _wheels[FR].motorTorque = t * 0.4f;
                    _wheels[RL].motorTorque = t * 0.6f; _wheels[RR].motorTorque = t * 0.6f;
                    break;
                default:
                    _wheels[FL].motorTorque = 0f; _wheels[FR].motorTorque = 0f;
                    _wheels[RL].motorTorque = t;  _wheels[RR].motorTorque = t;
                    break;
            }
        }

        /// <summary>
        /// Moto inclina para dentro da curva. Sem isso ela faz curva "de pé", que é a coisa que mais
        /// denuncia moto de protótipo. É inclinação visual aplicada ao corpo — a física de duas rodas
        /// de verdade exigiria outro modelo, e para o ritmo deste jogo isso basta.
        /// </summary>
        private void InclinarSeMoto()
        {
            if (!_ehMoto) return;
            float lateral = Vector3.Dot(_rb.linearVelocity, transform.right);
            float alvo = Mathf.Clamp(-lateral * 2.4f, -32f, 32f) * Mathf.Clamp01(SpeedKmh / 25f);
            _inclinacao = Mathf.MoveTowards(_inclinacao, alvo, 90f * Time.fixedDeltaTime);

            var e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(e.x, e.y, _inclinacao);
        }
        private float _inclinacao;
        private void ApplyBrake(float b)
        {
            // freio de atrito uniforme escala com a classe (jamanta freia mais devagar) e o dano
            float f = b * _fatorFreioClasse * (_health != null ? _health.FatorFreio : 1f);
            for (int i = 0; i < Wheels; i++) _wheels[i].brakeTorque = f;
        }

        private void ConsumeFuel(float throttle)
        {
            float distKm = (SpeedKmh * Time.fixedDeltaTime) / 3600f;
            // consumo base + aceleração (pisar fundo bebe mais) × fator de classe (moto economiza, jamanta bebe)
            float consumo = (litersPerKm * distKm) + Mathf.Abs(throttle) * litersPerKm * distKm * 1.5f;
            consumo *= _fatorCombustivelClasse;
            // marcha-lenta: parado no engarrafamento ainda gasta (docs/04 §4.3 "ocioso")
            if (SpeedKmh < arrastoMarchaKmh && _marcha != 0)
                consumo += ociosoLitrosPorHora * _fatorCombustivelClasse * (Time.fixedDeltaTime / 3600f);
            _fuel -= consumo;
            if (_fuel < 0f) _fuel = 0f;
        }

        /// <summary>Reabastece via EconomyService (preço inflacionado pelo IPC-Caos).</summary>
        private void Refuel()
        {
            if (_econ == null || _fuel >= tankLiters - 0.01f) return;
            float unit     = _econ.PriceFor(fuelPriceRsPerL);
            float needed   = tankLiters - _fuel;
            float costFull = unit * needed;

            if (_econ.TrySpend(costFull)) { _fuel = tankLiters; Debug.Log($"[Veículo] Tanque cheio (−R${costFull:F2})."); return; }

            float litersAfford = _econ.Rs / Mathf.Max(0.01f, unit);
            if (litersAfford > 0.1f && _econ.TrySpend(unit * litersAfford))
            {
                _fuel = Mathf.Min(tankLiters, _fuel + litersAfford);
                Debug.Log($"[Veículo] Abasteceu {litersAfford:F1}L (−R${unit * litersAfford:F2}).");
            }
            else Debug.Log("[Veículo] Sem dinheiro para abastecer.");
        }

        private void SyncWheelMeshes()
        {
            for (int i = 0; i < Wheels; i++)
            {
                if (_meshes[i] == null) continue;
                _wheels[i].GetWorldPose(out Vector3 p, out Quaternion q);
                _meshes[i].SetPositionAndRotation(p, q * Quaternion.Euler(0f, 0f, 90f));
            }
        }

        private void BuildWheels()
        {
            if (_built) return;
            _built = true;

            Vector3[] pos =
            {
                new Vector3(-halfTrack, 0f,  halfWheelBase), // FL
                new Vector3( halfTrack, 0f,  halfWheelBase), // FR
                new Vector3(-halfTrack, 0f, -halfWheelBase), // RL
                new Vector3( halfTrack, 0f, -halfWheelBase), // RR
            };
            string[] names = { "Roda_DE", "Roda_DD", "Roda_TE", "Roda_TD" };

            // suspensão dimensionada pela massa: 1 g de compressão estática em cada roda
            float carga = _rb.mass * 9.81f / Wheels;
            float mola  = carga / (suspensionDist * 0.35f);
            float amort = 2f * Mathf.Sqrt(mola * (_rb.mass / Wheels)) * 0.45f;   // ~45% do crítico

            for (int i = 0; i < Wheels; i++)
            {
                var go = new GameObject(names[i]);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = pos[i];

                var wc = go.AddComponent<WheelCollider>();
                wc.radius             = wheelRadius;
                wc.suspensionDistance = suspensionDist;
                wc.mass               = Mathf.Max(15f, _rb.mass * 0.02f);
                wc.wheelDampingRate   = 0.3f;
                wc.forceAppPointDistance = wheelRadius * 0.6f;   // ponto de aplicação abaixo do CoM

                var s = wc.suspensionSpring;
                s.spring = mola; s.damper = amort; s.targetPosition = 0.4f;
                wc.suspensionSpring = s;

                bool traseira = i >= RL;

                // atrito longitudinal: firme, para acelerar/frear sem patinar demais
                var fw = wc.forwardFriction;
                fw.extremumSlip = 0.35f; fw.extremumValue = 1.0f;
                fw.asymptoteSlip = 0.85f; fw.asymptoteValue = 0.62f;
                fw.stiffness = 1.6f;
                wc.forwardFriction = fw;

                // atrito lateral: a traseira segura um pouco menos → o carro sai de traseira em vez
                // de andar sobre trilhos, e a dirigibilidade do catálogo pesa aqui
                var sw = wc.sidewaysFriction;
                sw.extremumSlip = 0.25f; sw.extremumValue = 1.0f;
                sw.asymptoteSlip = 0.55f; sw.asymptoteValue = 0.72f;
                sw.stiffness = _aderenciaLateral * (traseira ? 0.92f : 1.0f);
                wc.sidewaysFriction = sw;

                _wheels[i] = wc;

                var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(mesh.GetComponent<Collider>());
                mesh.name = names[i] + "_Pneu";
                mesh.transform.SetParent(transform, false);
                mesh.transform.localScale = new Vector3(wheelRadius * 2f, wheelRadius * 0.32f, wheelRadius * 2f);
                mesh.GetComponent<MeshRenderer>().sharedMaterial =
                    CityPalette.MatTex(Superficie.Pneu, Color.white, wheelRadius * 6f, wheelRadius * 2f, 0.12f, 0f);
                _meshes[i] = mesh.transform;

                // calota: gira junto porque é filha da malha do pneu
                var calota = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(calota.GetComponent<Collider>());
                calota.name = "Calota";
                calota.transform.SetParent(mesh.transform, false);
                calota.transform.localScale = new Vector3(0.55f, 1.06f, 0.55f);
                calota.GetComponent<MeshRenderer>().sharedMaterial =
                    CityPalette.Mat(new Color(0.62f, 0.63f, 0.66f), 0.65f, 0.85f);
            }
        }
    }
}
