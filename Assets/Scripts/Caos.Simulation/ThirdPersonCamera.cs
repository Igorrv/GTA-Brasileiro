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
        // A pé: câmera sobre o ombro direito, perto e baixa — o personagem ocupa a tela e dá pra ver
        // o que ele está fazendo (agachado, sentado, bebendo). Longe demais vira formiga andando.
        [SerializeField] private Vector3 offsetAPe    = new Vector3(0.55f, 1.35f, -3.1f);
        [SerializeField] private Vector3 offsetCarro  = new Vector3(0f, 2.9f, -6.6f);
        [SerializeField] private float   alturaOlhar  = 1.25f;
        [SerializeField] private float   alturaOlharCarro = 1.05f;

        [Header("Suavização")]
        [SerializeField] private float seguirLerp = 11f;
        [SerializeField] private float orbitSens  = 2.5f;
        [SerializeField] private float minPitch   = -18f;
        [SerializeField] private float maxPitch   = 68f;

        [Header("Campo de visão")]
        [SerializeField] private float fovBase   = 62f;
        [SerializeField] private float fovMax    = 74f;
        [SerializeField] private float velRef    = 130f;   // km/h para o FOV cheio

        [Header("Colisão")]
        [SerializeField] private float raioCamera = 0.32f;
        [SerializeField] private float folga      = 0.22f;

        private float _yaw;
        private float _pitch = 12f;
        private Camera _cam;
        private PlayerVehicleLink _link;
        private VehicleController _veiculo;
        private Vector3 _posAnterior;
        private float _tremor;

        public void Bind(Transform t) => target = t;

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
            if (target == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            if (GameInput.CameraOrbit)
            {
                Vector2 o = GameInput.Orbit;
                _yaw   += o.x * orbitSens;
                _pitch -= o.y * orbitSens;
                _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            bool dirigindo = _link != null && !_link.OnFoot;
            Vector3 offset = dirigindo ? offsetCarro : offsetAPe;

            // dirigindo, a câmera se alinha atrás do carro sozinha (só solta se o jogador orbitar)
            if (dirigindo && !GameInput.CameraOrbit)
                _yaw = Mathf.LerpAngle(_yaw, target.eulerAngles.y, 3.2f * dt);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 foco = target.position + Vector3.up * (dirigindo ? alturaOlharCarro : alturaOlhar);
            Vector3 desejada = foco + rot * offset;

            // ---- colisão: puxa a câmera para perto se houver parede no caminho ----
            Vector3 dir = desejada - foco;
            float dist = dir.magnitude;
            if (dist > 0.01f && Physics.SphereCast(foco, raioCamera, dir / dist, out RaycastHit hit,
                                                   dist, CaosLayers.MascaraCamera, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(target))
                    desejada = foco + (dir / dist) * Mathf.Max(0.8f, hit.distance - folga);
            }

            // ---- solavanco de buraco ----
            if (_veiculo != null && Time.time - _veiculo.BuracoSentido < 0.35f) _tremor = 1f;
            _tremor = Mathf.MoveTowards(_tremor, 0f, dt * 3.2f);
            if (_tremor > 0f)
            {
                float amp = _tremor * 0.16f;
                desejada += new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp), 0f);
            }

            // Suavização SEPARADA: a posição segue mais devagar que a mira. Se as duas usam o mesmo
            // lerp, a câmera "escorrega" nas curvas; separando, ela acompanha o alvo com firmeza e
            // ainda assim amortece o solavanco do terreno.
            transform.position = Vector3.Lerp(transform.position, desejada, seguirLerp * dt);

            // ---- olhar um pouco à frente do movimento ----
            Vector3 vel = (target.position - _posAnterior) / dt;
            _posAnterior = target.position;
            Vector3 adiante = Vector3.ClampMagnitude(new Vector3(vel.x, 0f, vel.z) * 0.18f, 4f);

            Quaternion miraAlvo = Quaternion.LookRotation((foco + adiante) - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, miraAlvo, (seguirLerp * 1.6f) * dt);

            // ---- FOV pela velocidade ----
            if (_cam != null)
            {
                float kmh = _veiculo != null && dirigindo ? Mathf.Abs(_veiculo.SpeedKmh) : vel.magnitude * 3.6f;
                float alvo = Mathf.Lerp(fovBase, fovMax, Mathf.Clamp01(kmh / velRef));
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, alvo, 4f * dt);
            }
        }
    }
}
