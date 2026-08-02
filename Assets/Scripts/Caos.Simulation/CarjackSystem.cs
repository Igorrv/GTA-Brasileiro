using Caos.Core;
using Caos.Data;
using Caos.World;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Roubo de carro (docs/10) — a mecânica que define o gênero.
    ///
    /// A pé, perto de um carro do trânsito e com ele em baixa velocidade, o <b>E</b> vira "roubar":
    /// o motorista é puxado pela porta, sai correndo a pé, e você assume o volante daquele modelo.
    ///
    /// O truque de implementação é não duplicar física: em vez de transformar o carro do trânsito num
    /// veículo dirigível (que exigiria WheelCollider, Rigidbody dinâmico e todo o resto em runtime),
    /// o <b>veículo do jogador troca de modelo</b> e é teleportado para a posição do carro roubado,
    /// que volta ao pool do tráfego. Do lado de fora é indistinguível; do lado de dentro é um objeto
    /// só, que já estava afinado.
    ///
    /// E tem consequência: roubar é crime na frente de testemunhas, então sobe o procurado.
    /// </summary>
    public class CarjackSystem : MonoBehaviour
    {
        private const float kAlcance      = 4.2f;   // distância pra alcançar a porta
        private const float kVelMaxAlvo   = 22f;    // km/h — não dá pra roubar carro em alta
        private const float kDuracaoPuxao = 0.55f;

        private Transform         _player;
        private PlayerVehicleLink _link;
        private VehicleController _veiculo;
        private Transform         _veiculoT;
        private WorldStateService _world;
        private bool              _ocupado;

        /// <summary>Texto que o HUD mostra quando dá pra roubar (vazio quando não dá).</summary>
        public string Prompt { get; private set; } = "";

        public void Init(Transform player, PlayerVehicleLink link, VehicleController veiculo, Transform veiculoT)
        {
            _player   = player;
            _link     = link;
            _veiculo  = veiculo;
            _veiculoT = veiculoT;
            ServiceLocator.TryGet(out _world);
        }

        private void Update()
        {
            Prompt = "";
            if (_ocupado || _player == null || _link == null || !_link.OnFoot) return;

            var alvo = AlvoMaisProximo();
            if (alvo == null) return;

            string nome = alvo.dto != null ? alvo.dto.nome : "carro";
            Prompt = $"{nome} — [E] roubar";

            if (GameInput.Interact) StartCoroutine(Roubar(alvo));
        }

        /// <summary>Carro do trânsito ao alcance da porta, andando devagar o bastante.</summary>
        private TrafficCar AlvoMaisProximo()
        {
            TrafficCar melhor = null;
            float melhorD = kAlcance;

            foreach (var c in FindObjectsOfType<TrafficCar>())
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                if (c.velocidade * 3.6f > kVelMaxAlvo) continue;

                Vector3 d = c.transform.position - _player.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist < melhorD) { melhorD = dist; melhor = c; }
            }
            return melhor;
        }

        private System.Collections.IEnumerator Roubar(TrafficCar alvo)
        {
            _ocupado = true;

            Vector3 posCarro = alvo.transform.position;
            Quaternion rotCarro = alvo.transform.rotation;
            VehicleDto dto = alvo.dto;

            // ---- 1. o puxão: o jogador vai até a porta do motorista ----
            float lado = Vector3.Dot(_player.position - posCarro, alvo.transform.right) >= 0f ? 1f : -1f;
            Vector3 porta = posCarro + alvo.transform.right * lado * 1.25f;
            porta.y = _player.position.y;

            var pc = _player.GetComponent<PlayerController>();
            var cc = _player.GetComponent<CharacterController>();
            if (pc != null) pc.enabled = false;

            Vector3 inicio = _player.position;
            for (float t = 0f; t < kDuracaoPuxao; t += Time.deltaTime)
            {
                if (cc != null) cc.enabled = false;
                _player.position = Vector3.Lerp(inicio, porta, Mathf.SmoothStep(0f, 1f, t / kDuracaoPuxao));
                _player.rotation = Quaternion.Slerp(_player.rotation,
                                    Quaternion.LookRotation(-alvo.transform.right * lado, Vector3.up), t / kDuracaoPuxao);
                yield return null;
            }
            if (cc != null) cc.enabled = true;

            // ---- 2. o motorista desce correndo ----
            SoltarMotorista(porta + alvo.transform.right * lado * 0.9f, alvo.transform.forward);

            // ---- 3. o carro do trânsito some e o veículo do jogador assume o modelo ----
            alvo.gameObject.SetActive(false);

            if (_veiculo != null && dto != null) _veiculo.TrocarModelo(dto, VehicleFactory.CorDe(dto));
            if (_veiculoT != null)
            {
                var rb = _veiculoT.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                _veiculoT.SetPositionAndRotation(posCarro + Vector3.up * 0.35f, rotCarro);
            }

            // ---- 4. crime: roubar na frente de todo mundo custa caro ----
            CrimeSystem.Instance?.ReportCrime(14);
            if (_world != null) _world.ApplyCaos(10f);

            if (pc != null) pc.enabled = true;
            _ocupado = false;

            // entra no carro logo em seguida
            _link?.ForcarEntrada();
            Debug.Log($"[Roubo] Levou o {(dto != null ? dto.nome : "carro")}.");
        }

        /// <summary>
        /// Cospe o motorista pela porta como um pedestre em fuga. Reaproveita o pool de pedestres —
        /// ele vira só mais um na rua, correndo na direção oposta ao carro.
        /// </summary>
        private void SoltarMotorista(Vector3 pos, Vector3 direcaoCarro)
        {
            var peds = FindObjectOfType<PedestrianSystem>();
            if (peds == null) return;

            Vector3 fuga = -direcaoCarro + Vector3.Cross(Vector3.up, direcaoCarro) * Random.Range(-0.6f, 0.6f);
            peds.SoltarFugitivo(pos, fuga.normalized);
        }
    }
}
