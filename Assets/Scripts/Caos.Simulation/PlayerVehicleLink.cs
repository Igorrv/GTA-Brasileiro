using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Orquestra entrar/sair do veículo (tecla E, docs/04 §4.5). Alterna qual controlador está
    /// ativo, esconde o personagem enquanto dirige e repassa o alvo da câmera. Mantém o estado
    /// dos sistemas de jogo intacto (atributos/economia continuam ticking).
    /// </summary>
    public class PlayerVehicleLink : MonoBehaviour
    {
        [SerializeField] private float            enterRadius = 4.5f;
        [SerializeField] private Transform        player;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform        vehicle;
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private ThirdPersonCamera cam;

        private CharacterController _cc;
        private Renderer[]          _visuals;
        private bool _onFoot = true;
        private bool _exitSide;   // alterna o lado de saída

        // ---- acesso p/ HUD/scanner ----
        public bool OnFoot       => _onFoot;
        public bool IsNearVehicle => NearVehicle();

        private void Awake()
        {
            if (player != null) Cache();
            ApplyControlState();
        }

        /// <summary>Conecta as referências em runtime (chamado pelo <see cref="WorldBuilder"/>).</summary>
        public void Configure(Transform p, PlayerController pc, Transform v, VehicleController vc, ThirdPersonCamera c)
        {
            player = p; playerController = pc; vehicle = v; vehicleController = vc; cam = c;
            Cache();
            ApplyControlState();
        }

        private void Cache()
        {
            _cc = player.GetComponent<CharacterController>();
            _visuals = player.GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            if (!GameInput.Interact) return;

            if (_onFoot && NearVehicle()) Enter();
            else if (!_onFoot)            Exit();
        }

        /// <summary>
        /// Dirigindo, o transform do protagonista acompanha o veículo (invisível, sem colisor).
        /// Assim o <b>jogador</b> continua sendo a única fonte de verdade de "onde eu estou" — é o que o
        /// radar, o bairro do HUD e a chegada das missões consultam.
        /// </summary>
        private void LateUpdate()
        {
            if (_onFoot || player == null || vehicle == null) return;
            player.position = vehicle.position;
        }

        private bool NearVehicle()
        {
            if (vehicle == null) return false;
            Vector3 a = player.position;   a.y = 0f;
            Vector3 b = vehicle.position;  b.y = 0f;
            return Vector3.Distance(a, b) <= enterRadius;
        }

        /// <summary>
        /// Entrar não é teleporte: o personagem <b>contorna até a porta mais próxima</b>, fica de frente
        /// pra ela e só então some para dentro. São ~0,35 s — o suficiente para a ação ter peso sem
        /// tirar o controle do jogador por muito tempo.
        /// </summary>
        private void Enter()
        {
            if (_entrando) return;
            StartCoroutine(EntrarPelaPorta());
        }

        private bool _entrando;

        private System.Collections.IEnumerator EntrarPelaPorta()
        {
            _entrando = true;

            // porta do lado em que o jogador já está (motorista ou carona)
            float lado = Vector3.Dot(player.position - vehicle.position, vehicle.right) >= 0f ? 1f : -1f;
            Vector3 porta = vehicle.position + vehicle.right * lado * 1.25f;
            porta.y = player.position.y;

            if (playerController != null) playerController.enabled = false;

            Vector3 inicio = player.position;
            Quaternion giroInicial = player.rotation;
            Quaternion giroPorta = Quaternion.LookRotation(-vehicle.right * lado, Vector3.up);

            const float dur = 0.35f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                if (_cc != null) _cc.enabled = false;
                player.position = Vector3.Lerp(inicio, porta, k);
                player.rotation = Quaternion.Slerp(giroInicial, giroPorta, k);
                yield return null;
            }

            _onFoot = false;
            _entrando = false;
            ApplyControlState();
        }

        private void Exit()
        {
            // só permite sair a baixa velocidade (evita atropelar a si mesmo ao sair)
            if (vehicleController != null && vehicleController.SpeedKmh > 6f) return;

            Vector3 side = vehicle.right * ((_exitSide ^= true) ? 2.4f : -2.4f);
            player.position = vehicle.position + side + Vector3.up * 0.6f;

            _onFoot = true;
            ApplyControlState();
        }

        private void ApplyControlState()
        {
            if (_cc != null)               _cc.enabled = _onFoot;
            if (playerController != null)  playerController.enabled = _onFoot;
            if (vehicleController != null) vehicleController.Controlled = !_onFoot;

            if (_visuals != null)
                for (int i = 0; i < _visuals.Length; i++)
                    if (_visuals[i] != null) _visuals[i].enabled = _onFoot;

            if (cam != null) cam.Bind(_onFoot ? player : vehicle);
        }
    }
}
