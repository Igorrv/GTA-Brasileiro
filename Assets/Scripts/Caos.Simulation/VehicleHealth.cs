using Caos.Core;
using Caos.Gameplay;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Integridade do veículo (docs/04 §3). Colisões fortes tiram vida do motor; a 0 o carro para
    /// (o <see cref="VehicleController"/> corta o torque) <b>e fere o jogador</b> (caminho para WASTED via
    /// <see cref="PlayerAttributes"/>). Reparo só na oficina (<see cref="InteractionScanner"/>) por R$.
    ///
    /// O dano é <b>visual + funcional</b>: cada estágio degrada a performance do carro — o motor perde
    /// potência, o freio fica mais longo, os pneus seguram menos e a direção puxa pra um lado. É o que
    /// faz o jogador sentir que está dirigindo uma sucata antes de ela parar de vez.
    ///
    /// Crime: colidir em alta velocidade com tráfego, polícia ou pedestre sobe o procurado
    /// (<see cref="CrimeSystem"/>); polícia e pedestre contam dobrado (gravidade).
    /// </summary>
    public class VehicleHealth : MonoBehaviour
    {
        private const float kMax = 100f;
        private const float kImpactThreshold = 5f;       // m/s — abaixo disso ignora (toques leves)
        private const float kPlayerHurtOnWreck = 25f;

        /// <summary>Estágios de dano (docs/04 §3.2) — o HUD e o som lêem para mudar o feedback.</summary>
        public enum EstadoDano { Impecavel, AmassadoLeve, AmassadoMedio, Critico, Incendio }

        [SerializeField] private float _hp = kMax;
        private PlayerAttributes _attrs;
        private bool _broken;

        // ---- durabilidade por classe (repassada pelo VehicleController) ----
        // moto é frágil (bate e amassa muito), caminhão é durão. O fator multiplica o dano recebido.
        private float _fatorDanoRecebido = 1f;
        private bool  _ehMoto;
        private bool  _ehCaminhao;
        // dano na direção: puxa o volante pra um lado fixo (sorteado ao atingir o estágio médio)
        private float _puxaDirecao;
        private float _puxaDirecaoAlvo;

        // ---- feedback do último impacto (câmera/HUD) ----
        public float  UltimoImpactoTempo { get; private set; } = -99f;
        public float  UltimoImpactoForca { get; private set; }

        private ParticleSystem _fumaca;
        private ParticleSystem _faisca;

        public float Hp   => _hp;
        public float Hp01 => _hp / kMax;
        public bool  Broken => _hp <= 0f;

        /// <summary>Estágio atual de dano (docs/04 §3.2).</summary>
        public EstadoDano Estado
        {
            get
            {
                float h = Hp01;
                if (h >= 0.95f) return EstadoDano.Impecavel;
                if (h >= 0.75f) return EstadoDano.AmassadoLeve;
                if (h >= 0.50f) return EstadoDano.AmassadoMedio;
                if (h >= 0.20f) return EstadoDano.Critico;
                return EstadoDano.Incendio;
            }
        }

        public bool EmIncendio => Hp01 < 0.20f;

        /// <summary>Multiplicador de torque do motor (1 = saudável, ~0,4 = critical). Aplicado pelo VehicleController.</summary>
        public float FatorMotor
        {
            get
            {
                float h = Hp01;
                if (h >= 0.50f) return Mathf.Lerp(0.92f, 1f, Mathf.InverseLerp(0.50f, 1f, h));
                if (h >= 0.20f) return Mathf.Lerp(0.65f, 0.92f, Mathf.InverseLerp(0.20f, 0.50f, h));
                return Mathf.Lerp(0.40f, 0.65f, Mathf.InverseLerp(0f, 0.20f, h));
            }
        }

        /// <summary>Multiplicador de eficiência do freio (1 = saudável, ~0,6 = critical).</summary>
        public float FatorFreio => Mathf.Lerp(0.60f, 1f, Hp01);

        /// <summary>Multiplicador de aderência dos pneus (1 = saudável, ~0,75 = critical).</summary>
        public float FatorAderencia => Mathf.Lerp(0.75f, 1f, Hp01);

        /// <summary>
        /// Viés de direção: 0 saudável; a partir do estágio médio, um valor fixo (sorteado) que puxa o
        /// volante pra um lado. O jogador precisa compensar — é o feedback de que a direção foi atingida
        /// (docs/04 §3.3). Sinal e magnitude escalam com o dano.
        /// </summary>
        public float PuxaDirecao => _puxaDirecao;

        private void Start()
        {
            ServiceLocator.TryGet(out _attrs);
            MontarFumaca();
            MontarFaisca();
        }

        /// <summary>
        /// Configura a durabilidade por classe. Chamado pelo <see cref="VehicleController"/> quando o
        /// modelo é definido (e de novo no Start, pois a saúde pode não existir ainda no ConfigureFromCatalog).
        /// </summary>
        public void ConfigurarClasse(float fatorDanoRecebido, bool ehMoto, bool ehCaminhao)
        {
            _fatorDanoRecebido = Mathf.Clamp(fatorDanoRecebido, 0.3f, 2.5f);
            _ehMoto = ehMoto;
            _ehCaminhao = ehCaminhao;
        }

        private void Update()
        {
            AtualizarFumaca();
            AtualizarFaisca();
            // a direção puxa suavemente em direção ao alvo (definido ao atingir o estágio médio)
            _puxaDirecao = Mathf.MoveTowards(_puxaDirecao, _puxaDirecaoAlvo, Time.deltaTime * 0.5f);
        }

        private void AtualizarFumaca()
        {
            if (_fumaca == null) return;
            var emissao = _fumaca.emission;
            bool fumegando = Hp01 < 0.42f;
            emissao.enabled = fumegando;
            if (!fumegando) return;

            // quanto mais batido, mais fumaça e mais escura: leve cinza no médio, preta grossa no incêndio
            float t = Mathf.InverseLerp(0.42f, 0f, Hp01);
            emissao.rateOverTime = Mathf.Lerp(8f, 40f, t);
            var main = _fumaca.main;
            main.startColor = Color.Lerp(new Color(0.72f, 0.72f, 0.74f, 0.55f),
                                         new Color(0.06f, 0.06f, 0.06f, 0.92f), t);
            main.startSize = Mathf.Lerp(0.6f, 1.1f, t);
        }

        private void AtualizarFaisca()
        {
            if (_faisca == null) return;
            var emissao = _faisca.emission;
            bool faiscando = EmIncendio && !Broken;
            emissao.enabled = faiscando;
            if (!faiscando) return;
            // faísca intermitente: o motor pegando fogo pisca, não é contínuo
            emissao.rateOverTime = (Mathf.Sin(Time.time * 18f) > 0.6f) ? 22f : 0f;
        }

        /// <summary>
        /// Fumaça do capô: nasce desligada e só liga quando o motor cai de 42%. É um
        /// <see cref="ParticleSystem"/> minúsculo (sem textura), barato o suficiente para o celular.
        /// </summary>
        private void MontarFumaca()
        {
            var go = new GameObject("Fumaca");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, 1.4f);

            _fumaca = go.AddComponent<ParticleSystem>();
            var main = _fumaca.main;
            main.startLifetime = 1.6f;
            main.startSpeed    = 1.4f;
            main.startSize     = 0.75f;
            main.maxParticles  = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor    = new Color(0.7f, 0.7f, 0.7f, 0.5f);

            var forma = _fumaca.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 18f; forma.radius = 0.18f;

            var tamanho = _fumaca.sizeOverLifetime;
            tamanho.enabled = true;
            tamanho.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.8f));

            var emissao = _fumaca.emission;
            emissao.enabled = false;

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.material = CityPalette.Mat(new Color(0.6f, 0.6f, 0.62f));
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
        }

        /// <summary>
        /// Faíscas do incêndio: só ligam abaixo de 20% de HP. Partículas amarelas curtas e rápidas,
        /// baratas — é o sinal visual de "esse carro vai explodir" antes do motor morrer.
        /// </summary>
        private void MontarFaisca()
        {
            var go = new GameObject("Faisca");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.85f, 1.35f);

            _faisca = go.AddComponent<ParticleSystem>();
            var main = _faisca.main;
            main.startLifetime = 0.35f;
            main.startSpeed    = 4.5f;
            main.startSize     = 0.05f;
            main.maxParticles  = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(1f, 0.78f, 0.25f, 1f);
            main.gravityModifier = 1.5f;

            var emissao = _faisca.emission;
            emissao.enabled = false;

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.material = CityPalette.Mat(new Color(1f, 0.8f, 0.3f));
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
        }

        /// <summary>Dano externo (buraco, incêndio, tiro) — a colisão continua com caminho próprio.</summary>
        public void AplicarDano(float quantidade)
        {
            if (quantidade <= 0f) return;
            AplicarDanoInterno(quantidade * _fatorDanoRecebido);
        }

        /// <summary>Aplica dano já escalado pela classe e cuida dos efeitos colaterais (direção, wreck).</summary>
        private void AplicarDanoInterno(float dano)
        {
            float antes = Hp01;
            _hp = Mathf.Max(0f, _hp - dano);
            UltimoImpactoTempo = Time.time;
            UltimoImpactoForca = Mathf.Max(UltimoImpactoForca, dano);
            ReagirMudancaDeEstado(antes);
            QuebrarSePrecisar();
        }

        /// <summary>Quando o dano cruza o estágio médio, sorteia um viés de direção fixo (docs/04 §3.3).</summary>
        private void ReagirMudancaDeEstado(float hp01Antes)
        {
            float agora = Hp01;
            if (hp01Antes > 0.50f && agora <= 0.50f && _puxaDirecaoAlvo == 0f)
                _puxaDirecaoAlvo = Random.Range(-0.16f, 0.16f);
            // magnitude do puxo cresce com o dano
            _puxaDirecaoAlvo = Mathf.Clamp(_puxaDirecaoAlvo, -0.22f, 0.22f);
        }

        private void OnCollisionEnter(Collision c)
        {
            float impact = c.relativeVelocity.magnitude;
            if (impact < kImpactThreshold) return;

            // dano proporcional ao excesso de velocidade, escalado pela classe (moto dobra, caminhão amassa pouco)
            float bruto = (impact - kImpactThreshold) * 2f;
            AplicarDanoInterno(bruto * _fatorDanoRecebido);
            // a câmera/HUD quer a velocidade do impacto (m/s) para o solavanco, não o dano em HP
            UltimoImpactoTempo = Time.time;
            UltimoImpactoForca = Mathf.Max(UltimoImpactoForca, impact);

            // crime: tráfego / polícia (×2) / pedestre (×2 — atropelo é grave)
            var col = c.collider;
            if (col != null && impact > kImpactThreshold + 4f)
            {
                var traffic = col.GetComponentInParent<TrafficCar>();
                var police  = col.GetComponentInParent<PoliceCar>();
                var ped     = col.GetComponentInParent<Pedestrian>();
                if (traffic != null || police != null || ped != null)
                {
                    int sev = Mathf.RoundToInt(impact);
                    if (police != null || ped != null) sev *= 2;
                    CrimeSystem.Instance?.ReportCrime(sev);
                }
            }
        }

        private void QuebrarSePrecisar()
        {
            if (_hp > 0f || _broken) return;
            _broken = true;
            // moto é mais branda com o piloto (cartoon, docs §2.7.2); caminhão/onibus é pior (massa)
            float danoJogador = kPlayerHurtOnWreck;
            if (_ehMoto)      danoJogador *= 0.8f;
            else if (_ehCaminhao) danoJogador *= 1.2f;
            if (_attrs != null) _attrs.Apply("saude", -danoJogador);
            Debug.Log("[Veículo] Motor inutilizado — leve à oficina (F).");
        }

        public void RepairFull()
        {
            _hp = kMax;
            _broken = false;
            _puxaDirecao = 0f;
            _puxaDirecaoAlvo = 0f;
            UltimoImpactoForca = 0f;
            Debug.Log("[Veículo] Reparado.");
        }
    }
}
