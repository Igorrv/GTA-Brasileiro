using Caos.Core;
using Caos.Gameplay;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Locomoção do protagonista a pé (CharacterController), relativa à câmera.
    ///
    /// Física de andar de verdade, e não "posição += direção":
    ///  • <b>arrancada e freada separadas</b> (sai devagar, para rápido) e inércia ao mudar de direção;
    ///  • <b>controle no ar</b> reduzido — quem pulou não vira de lado como um drone;
    ///  • <b>pulo</b> com altura definida em metros (a gravidade é derivada dela) e <b>coyote time</b>,
    ///    a janelinha em que ainda dá pra pular depois de sair da beirada;
    ///  • <b>escorrega em rampa íngreme</b> acima do limite do CharacterController;
    ///  • <b>empurra corpos leves</b> (cone, lixeira) ao esbarrar.
    ///
    /// Correr marca <see cref="PlayerAttributes.Ativo"/>=true — o backend já aplica o decaimento
    /// acelerado de Energia nesse estado, sem duplicar regra aqui. É habilitado/desabilitado pelo
    /// <see cref="PlayerVehicleLink"/> ao entrar/sair do veículo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomoção")]
        [SerializeField] private float velCaminhada = 3.2f;    // m/s
        [SerializeField] private float velCorrida   = 6.5f;    // m/s
        [SerializeField] private float arrancada    = 16f;     // m/s² ao acelerar
        [SerializeField] private float freada       = 26f;     // m/s² ao soltar/inverter
        [SerializeField] private float controleNoAr = 0.35f;   // fração da aceleração em voo
        [SerializeField] private float giroGraus    = 900f;    // grau/s de alinhamento à direção

        [Header("Pulo e gravidade")]
        [SerializeField] private float alturaPulo   = 1.05f;   // m — a gravidade sai daqui
        [SerializeField] private float gravidade    = -26f;    // m/s² (mais forte que a real = melhor game feel)
        [SerializeField] private float coyote       = 0.12f;   // s de tolerância após sair do chão
        [SerializeField] private float forcaEmpurrao= 2.2f;

        private CharacterController _cc;
        private Camera              _cam;
        private PlayerAttributes    _attrs;
        private PlayerActions       _acoes;
        private Vector3             _vel;         // velocidade horizontal suavizada
        private float               _vy;          // velocidade vertical
        private float               _ultimoChao;  // Time.time em que tocou o chão pela última vez
        private Vector3             _normalChao = Vector3.up;

        /// <summary>Velocidade horizontal atual (m/s) — usada pelo áudio/animação.</summary>
        public float VelocidadeHorizontal => _vel.magnitude;
        public bool  NoChao => _cc != null && _cc.isGrounded;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cam = Camera.main;
            _acoes = GetComponent<PlayerActions>();
        }

        private void OnEnable()
        {
            // zera impulso residual ao retomar o controle (ex.: saiu do carro)
            _vel = Vector3.zero;
            _vy  = 0f;
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out _attrs) == false)
                Debug.LogWarning("[Player] PlayerAttributes ainda não registrado — corrida não afetará Energia.");
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            float dt = Time.deltaTime;

            Vector2 m = GameInput.Move;
            if (m.sqrMagnitude > 1f) m.Normalize();

            // direção relativa à câmera (plano XZ)
            Vector3 fwd   = _cam ? Vector3.Scale(_cam.transform.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
            Vector3 right = _cam ? Vector3.Scale(_cam.transform.right,   new Vector3(1, 0, 1)).normalized : Vector3.right;
            Vector3 desejo = fwd * m.y + right * m.x;

            bool agachado = _acoes != null && _acoes.Agachado;
            bool correndo = GameInput.Run && !agachado && m.sqrMagnitude > 0.01f;
            float alvoVel = correndo ? velCorrida : velCaminhada;
            if (agachado) alvoVel *= 0.45f;   // agachado anda devagar (e faz menos barulho)

            // atributos: só marca atividade; quem decide o custo é o backend
            if (_attrs != null) _attrs.Ativo = correndo;

            // ---- aceleração horizontal ----
            bool noChao = _cc.isGrounded;
            if (noChao) { _ultimoChao = Time.time; _vy = -2f; }

            Vector3 alvo = desejo * alvoVel;
            bool acelerando = desejo.sqrMagnitude > 0.01f && Vector3.Dot(alvo, _vel) >= 0f;
            float taxa = (acelerando ? arrancada : freada) * (noChao ? 1f : controleNoAr);
            _vel = Vector3.MoveTowards(_vel, alvo, taxa * dt);

            // ---- pulo (com coyote time; agachado não pula) ----
            if (GameInput.Jump && !agachado && Time.time - _ultimoChao <= coyote)
            {
                _vy = Mathf.Sqrt(2f * Mathf.Abs(gravidade) * alturaPulo);   // v = √(2·g·h)
                _ultimoChao = -999f;
            }

            _vy += gravidade * dt;
            if (_vy < -55f) _vy = -55f;   // velocidade terminal

            // ---- rampa íngreme: escorrega em vez de subir na parede ----
            Vector3 escorrego = Vector3.zero;
            if (noChao && Vector3.Angle(_normalChao, Vector3.up) > _cc.slopeLimit)
            {
                Vector3 declive = Vector3.ProjectOnPlane(Vector3.down, _normalChao).normalized;
                escorrego = declive * 6f;
            }

            _cc.Move((_vel + escorrego + new Vector3(0f, _vy, 0f)) * dt);

            // ---- alinha a rotação ao movimento (gira mais rápido em baixa velocidade) ----
            if (_vel.sqrMagnitude > 0.05f)
            {
                Quaternion olhar = Quaternion.LookRotation(new Vector3(_vel.x, 0f, _vel.z).normalized);
                float giro = giroGraus * Mathf.Lerp(1.4f, 0.6f, Mathf.Clamp01(_vel.magnitude / velCorrida));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, olhar, giro * dt);
            }
        }

        /// <summary>Guarda a normal do chão e dá um empurrão nos corpos leves que o jogador esbarra.</summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _normalChao = hit.normal;

            var rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic || rb.mass > 60f) return;
            if (hit.moveDirection.y < -0.3f) return;    // não empurra o que está sob os pés

            Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            rb.AddForceAtPosition(dir * forcaEmpurrao * Mathf.Max(1f, _vel.magnitude), hit.point, ForceMode.Impulse);
        }
    }
}
