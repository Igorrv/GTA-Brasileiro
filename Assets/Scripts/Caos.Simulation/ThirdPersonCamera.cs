using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Câmera de terceira pessoa (docs/12 §12.2). Segue o jogador a pé ou o veículo, e o que separa
    /// uma câmera de protótipo de uma câmera de jogo está tudo aqui:
    ///
    ///  • <b>Enquadramento por contexto</b> — a pé fica perto e no ombro; dirigindo recua e sobe, para
    ///    caber o carro e a rua à frente.
    ///  • <b>Colisão com o cenário</b> (SphereCast): encosta num muro e a câmera chega mais perto em
    ///    vez de atravessar a parede — sem isso, dirigir num beco da favela é ver o mundo por dentro.
    ///  • <b>FOV por velocidade</b>: abre até +12° na alta e fecha ao parar. É o truque mais barato de
    ///    sensação de velocidade que existe.
    ///  • <b>Olhar à frente</b>: mira um pouco adiante na direção do movimento.
    ///  • <b>Solavanco</b> ao cair num buraco.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Enquadramento")]
        [SerializeField] private Vector3 offsetAPe = new Vector3(0.62f, 0.72f, -3.25f);
        [SerializeField] private Vector3 offsetCarro = new Vector3(0f, 1.8f, -6.2f);
        [SerializeField] private Vector3 offsetCarroEmVelocidade = new Vector3(0f, 0.35f, -1.2f);
        [SerializeField] private float alturaOlhar = 1.25f;
        [SerializeField] private float alturaOlharCarro = 1.05f;
        [SerializeField] private float transicaoContexto = 7.5f;

        [Header("Suavização")]
        [SerializeField] private float seguirLerp = 11f;
        [SerializeField] private float orbitSens  = 2.5f;
        [SerializeField] private float minPitch   = -18f;
        [SerializeField] private float maxPitch   = 68f;
        [SerializeField] private float atrasoRecentralizar = 0.9f;
        [SerializeField] private float recentralizarLento = 2.2f;
        [SerializeField] private float recentralizarRapido = 5.2f;

        [Header("Campo de visão")]
        [SerializeField] private float fovBase   = 62f;
        [SerializeField] private float fovMax    = 74f;
        [SerializeField] private float velRef    = 130f;   // km/h para o FOV cheio

        [Header("Colisão")]
        [SerializeField] private float raioCamera = 0.36f;
        [SerializeField] private float folga      = 0.16f;

        private float _yaw;
        private float _pitch = 10f;
        private Camera _cam;
        private PlayerVehicleLink _link;
        private VehicleController _veiculo;
        private Vector3 _posAnterior;
        private Vector3 _adianteSuave;
        private bool _temPosAnterior;
        private bool _inicializada;
        private float _contextoCarro;
        private float _olharAtras;
        private float _ultimoOrbit = -999f;
        private float _tremor;
        // Folga para os colliders do próprio carro + quarteirão/props sem alocar em becos densos.
        private readonly RaycastHit[] _hitsCamera = new RaycastHit[64];

        /// <summary>
        /// Base estável da órbita para a locomoção a pé. Diferente de <c>transform.forward</c>, ela
        /// não vira durante a animação de olhar para trás.
        /// </summary>
        public Vector3 FrenteMovimento => Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        public Vector3 DireitaMovimento => Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;

        public void Bind(Transform t)
        {
            if (target == t) return;

            target = t;
            _temPosAnterior = t != null;
            _posAnterior = t != null ? t.position : Vector3.zero;
            _adianteSuave = Vector3.zero;

            // O primeiro Bind acontece antes do primeiro frame. Posicionar aqui evita a câmera viajar
            // desde a origem do mundo durante os segundos mais importantes da primeira sessão.
            if (!_inicializada && t != null) InicializarPosicao();
        }

        /// <summary>Ligado pelo WorldBuilder para a câmera saber quando está no carro.</summary>
        public void Contexto(PlayerVehicleLink link, VehicleController veiculo)
        {
            _link = link; _veiculo = veiculo;
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null) _cam.fieldOfView = fovBase;
        }

        private void LateUpdate()
        {
            IFonteDeEntrada entrada = EntradaLocal.Instancia;
            bool orbitando = entrada.CameraOrbit;
            // Consome o delta mesmo pausado/sem alvo para ele nunca reaparecer como salto ao retomar.
            Vector2 orbitaFrame = orbitando ? entrada.Orbit : Vector2.zero;

            if (target == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (!_inicializada) InicializarPosicao();

            if (orbitando)
            {
                float sens = orbitSens * SettingsMenu.Sensibilidade;
                _yaw   += orbitaFrame.x * sens;
                _pitch -= orbitaFrame.y * sens * (SettingsMenu.InverterY ? -1f : 1f);
                _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
                _ultimoOrbit = Time.unscaledTime;
            }

            bool dirigindo = _link != null && !_link.OnFoot;
            float kmh = _veiculo != null && dirigindo ? Mathf.Abs(_veiculo.SpeedKmh) : 0f;
            float velocidade01 = Mathf.Clamp01(kmh / Mathf.Max(1f, velRef));
            _contextoCarro = Mathf.Lerp(_contextoCarro, dirigindo ? 1f : 0f, Fator(transicaoContexto, dt));

            bool olharAtras = entrada.LookBehind;
            _olharAtras = Mathf.Lerp(_olharAtras, olharAtras ? 1f : 0f, Fator(12f, dt));
            if (olharAtras) _ultimoOrbit = Time.unscaledTime;

            // Depois de um swipe, preserva a vista por um instante; em velocidade a perseguição volta
            // mais firme para a traseira do carro, e devagar deixa espaço para manobrar a câmera.
            if (dirigindo && !orbitando && !olharAtras &&
                Time.unscaledTime - _ultimoOrbit >= atrasoRecentralizar)
            {
                float taxa = Mathf.Lerp(recentralizarLento, recentralizarRapido, velocidade01);
                _yaw = Mathf.LerpAngle(_yaw, target.eulerAngles.y, Fator(taxa, dt));
            }

            float curvaAtras = _olharAtras * _olharAtras * (3f - 2f * _olharAtras);
            float yawAtras = (dirigindo ? target.eulerAngles.y : _yaw) + 179.9f;
            float yawVisual = Mathf.LerpAngle(_yaw, yawAtras, curvaAtras);
            Quaternion rot = Quaternion.Euler(_pitch, yawVisual, 0f);

            Vector3 offsetCarroAtual = offsetCarro + offsetCarroEmVelocidade * velocidade01;
            Vector3 offset = Vector3.Lerp(offsetAPe, offsetCarroAtual, _contextoCarro);
            float altura = Mathf.Lerp(alturaOlhar, alturaOlharCarro, _contextoCarro);
            Vector3 foco = target.position + Vector3.up * altura;
            Vector3 desejada = foco + rot * offset;

            // ---- solavanco de buraco ----
            if (_veiculo != null && Time.time - _veiculo.BuracoSentido < 0.35f) _tremor = 1f;
            _tremor = Mathf.MoveTowards(_tremor, 0f, dt * 3.2f);
            if (_tremor > 0f)
            {
                float amp = _tremor * 0.16f;
                float nX = Mathf.PerlinNoise(7.1f, Time.time * 24f) * 2f - 1f;
                float nY = Mathf.PerlinNoise(19.7f, Time.time * 24f) * 2f - 1f;
                desejada += rot * new Vector3(nX * amp, nY * amp, 0f);
            }

            // Resolve antes e depois do amortecimento. O segundo passe impede que o lerp atravesse a
            // quina de um prédio quando o alvo dobra a esquina, mesmo que ambos os extremos sejam válidos.
            Vector3 segura = ResolverColisao(foco, desejada, out bool bloqueada);
            float taxaSeguir = bloqueada ? seguirLerp * 2.4f : seguirLerp;
            Vector3 candidata = Vector3.Lerp(transform.position, segura, Fator(taxaSeguir, dt));
            transform.position = ResolverColisao(foco, candidata, out _);

            // ---- olhar um pouco à frente do movimento ----
            Vector3 vel = _temPosAnterior ? (target.position - _posAnterior) / dt : Vector3.zero;
            _posAnterior = target.position;
            _temPosAnterior = true;
            if (vel.sqrMagnitude > 6400f) vel = Vector3.zero; // teleporte/troca de alvo não vira chicote

            Vector3 velPlana = new Vector3(vel.x, 0f, vel.z);
            Vector3 adianteAlvo;
            if (dirigindo)
            {
                Vector3 frentePlana = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
                Vector3 direcao = velPlana.sqrMagnitude > 0.04f ? velPlana.normalized : frentePlana;
                adianteAlvo = direcao * Mathf.Lerp(0.55f, 4.2f, velocidade01);
            }
            else
            {
                adianteAlvo = Vector3.ClampMagnitude(velPlana * 0.12f, 0.8f);
            }

            // Ao olhar para trás, desloca a mira para a rua que ficou para trás, em vez de continuar
            // mostrando espaço vazio à frente do capô.
            adianteAlvo *= Mathf.Lerp(1f, -0.6f, curvaAtras);
            _adianteSuave = Vector3.Lerp(_adianteSuave, adianteAlvo, Fator(6.5f, dt));

            Vector3 direcaoMira = (foco + _adianteSuave) - transform.position;
            if (direcaoMira.sqrMagnitude > 0.0001f)
            {
                Quaternion miraAlvo = Quaternion.LookRotation(direcaoMira, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, miraAlvo, Fator(seguirLerp * 1.6f, dt));
            }

            // ---- FOV pela velocidade ----
            if (_cam != null)
            {
                float velocidadeFov = dirigindo ? kmh : velPlana.magnitude * 3.6f;
                float alvo = Mathf.Lerp(fovBase, fovMax, Mathf.Clamp01(velocidadeFov / Mathf.Max(1f, velRef)));
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, alvo, Fator(4f, dt));
            }
        }

        private void InicializarPosicao()
        {
            if (target == null) return;

            _yaw = target.eulerAngles.y;
            _posAnterior = target.position;
            _temPosAnterior = true;

            Vector3 foco = target.position + Vector3.up * alturaOlhar;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = ResolverColisao(foco, foco + rot * offsetAPe, out _);

            Vector3 direcao = foco - transform.position;
            if (direcao.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direcao, Vector3.up);

            _inicializada = true;
        }

        /// <summary>
        /// SphereCast sem alocação que escolhe o obstáculo válido mais próximo. O cast simples antigo
        /// parava no próprio carro (primeiro hit) e nunca enxergava a parede logo atrás dele.
        /// </summary>
        private Vector3 ResolverColisao(Vector3 origem, Vector3 destino, out bool bloqueada)
        {
            bloqueada = false;
            Vector3 delta = destino - origem;
            float distancia = delta.magnitude;
            if (distancia <= 0.01f) return destino;

            Vector3 direcao = delta / distancia;
            int quantidade = Physics.SphereCastNonAlloc(origem, raioCamera, direcao, _hitsCamera,
                distancia, CaosLayers.MascaraCamera, QueryTriggerInteraction.Ignore);

            float distanciaSegura = distancia;
            for (int i = 0; i < quantidade; i++)
            {
                Collider col = _hitsCamera[i].collider;
                if (col == null || PertenceAoAlvo(col.transform)) continue;

                float candidata = Mathf.Max(0.08f, _hitsCamera[i].distance - folga);
                if (candidata >= distanciaSegura) continue;
                distanciaSegura = candidata;
                bloqueada = true;
            }

            // Saturação significa que há geometria demais no corredor para garantir qual hit ficou
            // de fora. Prioriza uma vista muito próxima por um frame em vez de arriscar mostrar o
            // interior de um quarteirão.
            if (quantidade == _hitsCamera.Length)
            {
                distanciaSegura = Mathf.Min(distanciaSegura, Mathf.Max(0.08f, raioCamera * 0.5f));
                bloqueada = true;
            }

            return bloqueada ? origem + direcao * distanciaSegura : destino;
        }

        private bool PertenceAoAlvo(Transform t)
        {
            if (target == null || t == null) return false;
            return t == target || t.IsChildOf(target) || target.IsChildOf(t);
        }

        private static float Fator(float taxa, float dt)
        {
            return taxa <= 0f ? 1f : 1f - Mathf.Exp(-taxa * dt);
        }
    }
}
