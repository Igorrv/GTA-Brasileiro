using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.World;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Polícia reativa ao nível de procurado (docs/10.7). O efetivo cresce com as estrelas — de uma
    /// viatura isolada até o camburão do Choque — e as unidades perseguem <b>pelas vias</b>: quando o
    /// caminho reto sairia da rua (morro, rio, quarteirão), elas se alinham à faixa do
    /// <see cref="CityLayout"/> que mais aponta pro alvo.
    ///
    /// Carros kinematic (baratos), giroflex piscando de verdade e cerco por offset para não empilharem.
    /// Bater neles conta crime dobrado (<see cref="VehicleHealth"/>).
    /// </summary>
    public class PoliceSystem : MonoBehaviour
    {
        [SerializeField] private int   maxUnits    = 8;
        [SerializeField] private float spawnRadius = 46f;
        [SerializeField] private float turnRate    = 3.5f;

        private Transform             _player;
        private PlayerVehicleLink     _link;
        private Transform             _vehicle;
        private GameCatalogs          _catalogs;
        private CityLayout            _layout;
        private WorldStateService     _world;
        private ObjectPool<PoliceCar> _pool;
        private readonly List<PoliceCar> _active = new List<PoliceCar>();
        private float _blink;

        /// <summary>Como o rádio da corporação chama cada nível (usado no HUD).</summary>
        public static string NomeDoNivel(int stars)
        {
            switch (stars)
            {
                case 0:  return "";
                case 1:  return "PM avisada";
                case 2:  return "Rádio-patrulha";
                case 3:  return "ROTA na área";
                case 4:  return "Batalhão de Choque";
                default: return "Águia no ar";
            }
        }

        public void Init(Transform player, PlayerVehicleLink link, Transform vehicle, GameCatalogs catalogs)
        {
            _player   = player;
            _link     = link;
            _vehicle  = vehicle;
            _catalogs = catalogs;
            _layout   = CityRuntime.Layout;
            ServiceLocator.TryGet(out _world);
            _pool = new ObjectPool<PoliceCar>(Factory, prewarm: maxUnits);
        }

        private PoliceCar Factory()
        {
            VehicleDto dto = null;
            if (_catalogs != null)
            {
                bool camburao = _active.Count >= 4;   // reforço pesado nos níveis altos
                string id = camburao ? "camburao" : "viatura_pm";
                if (!_catalogs.VehicleById.TryGetValue(id, out dto))
                    _catalogs.VehicleById.TryGetValue("viatura_pm", out dto);
            }

            var go = new GameObject("Viatura");
            VehicleFactory.BuildBody(go.transform, dto, new Color(0.10f, 0.10f, 0.12f), rodasVisuais: true);

            // faixa branca lateral + giroflex
            float L = dto != null && dto.comprimento > 1f ? dto.comprimento : 4.6f;
            float W = dto != null && dto.largura     > 1f ? dto.largura     : 1.8f;
            CityPalette.Box(go.transform, "FaixaE", new Vector3(-W * 0.51f, 0.75f, 0f), new Vector3(0.06f, 0.3f, L * 0.75f), CityPalette.Mat(Color.white), 0f, false);
            CityPalette.Box(go.transform, "FaixaD", new Vector3( W * 0.51f, 0.75f, 0f), new Vector3(0.06f, 0.3f, L * 0.75f), CityPalette.Mat(Color.white), 0f, false);

            CaosLayers.Marcar(go, CaosLayers.Veiculo);
            var car = go.AddComponent<PoliceCar>();
            var giroA = CityPalette.Box(go.transform, "GiroA", new Vector3(-0.35f, 1.62f, 0.2f), new Vector3(0.45f, 0.18f, 0.3f), CityPalette.Mat(new Color(0.15f, 0.35f, 1f)), 0f, false);
            var giroB = CityPalette.Box(go.transform, "GiroB", new Vector3( 0.35f, 1.62f, 0.2f), new Vector3(0.45f, 0.18f, 0.3f), CityPalette.Mat(new Color(0.55f, 0.05f, 0.05f)), 0f, false);
            car.giroA = giroA.GetComponent<MeshRenderer>();
            car.giroB = giroB.GetComponent<MeshRenderer>();

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            go.SetActive(false);
            return car;
        }

        private void Update()
        {
            if (_world == null || _pool == null) return;

            int stars   = _world.Stars;
            int desired = Mathf.Min(maxUnits, stars * 2);

            while (_active.Count < desired && _pool.InPool > 0)
            {
                var c = _pool.Get();
                Vector3 baseP = _player != null ? _player.position : Vector3.zero;
                Vector2 ring = Random.insideUnitCircle.normalized * spawnRadius;
                Vector3 pos = baseP + new Vector3(ring.x, 0.05f, ring.y);
                if (_layout != null) pos = _layout.RandomDrivablePoint(pos, 25f);
                c.transform.position = pos;
                // setor fixo por unidade: distribui o cerco em volta do alvo em vez de empilhar
                float setor = (_active.Count / Mathf.Max(1f, desired)) * Mathf.PI * 2f;
                float raio  = 7f + _active.Count * 1.5f;
                c.encircle = new Vector3(Mathf.Cos(setor) * raio, 0f, Mathf.Sin(setor) * raio);
                _active.Add(c);
            }
            while (_active.Count > desired)
            {
                var c = _active[_active.Count - 1];
                _active.RemoveAt(_active.Count - 1);
                _pool.Release(c);
            }
            if (_active.Count == 0) return;

            // giroflex: pisca alternado (o azul e vermelho que aparece no retrovisor)
            _blink += Time.deltaTime;
            bool ladoA = ((int)(_blink * 6f) & 1) == 0;
            var azul   = CityPalette.Mat(new Color(0.15f, 0.35f, 1f));
            var verm   = CityPalette.Mat(new Color(0.90f, 0.10f, 0.10f));
            var apagado= CityPalette.Mat(new Color(0.20f, 0.20f, 0.22f));

            Transform target = (_link != null && !_link.OnFoot && _vehicle != null) ? _vehicle : _player;
            if (target == null) return;

            float dt = Time.deltaTime;
            float velocidade = 20f + stars * 2.2f;   // quanto mais estrela, mais pressão

            for (int i = 0; i < _active.Count; i++)
            {
                var c = _active[i];
                if (c == null) continue;

                if (c.giroA != null) c.giroA.sharedMaterial = ladoA ? azul : apagado;
                if (c.giroB != null) c.giroB.sharedMaterial = ladoA ? apagado : verm;

                // Cerco: em vez de todas irem ao mesmo ponto (fila indiana atrás do jogador), cada
                // unidade tem um setor do círculo e tenta ocupá-lo. Quem já está perto se posiciona
                // à frente para cortar o caminho — é o que faz a perseguição fechar de verdade.
                float dAtual = Vector3.Distance(c.transform.position, target.position);
                Vector3 antecipa = Vector3.zero;
                var rbAlvo = target.GetComponent<Rigidbody>();
                if (rbAlvo != null && dAtual < 45f)
                    antecipa = rbAlvo.linearVelocity * Mathf.Clamp(dAtual / 22f, 0.3f, 1.6f);

                Vector3 to = (target.position + antecipa + c.encircle) - c.transform.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.01f) continue;

                Vector3 dir = to / dist;
                Vector3 passo = c.transform.position + dir * velocidade * dt;

                // se o caminho reto sai da rua, segue a via que mais aponta pro alvo
                if (_layout != null && !_layout.IsDrivable(passo))
                {
                    Vector3 alt = Mathf.Abs(dir.x) > Mathf.Abs(dir.z)
                        ? new Vector3(Mathf.Sign(dir.x), 0f, 0f)
                        : new Vector3(0f, 0f, Mathf.Sign(dir.z));
                    Vector3 tentativa = c.transform.position + alt * velocidade * dt;
                    if (_layout.IsDrivable(tentativa)) { passo = tentativa; dir = alt; }
                }

                c.transform.position = passo;
                c.transform.rotation = Quaternion.Slerp(c.transform.rotation, Quaternion.LookRotation(dir), turnRate * dt);
            }
        }

        /// <summary>Distância horizontal (m) da unidade de polícia mais próxima do alvo.</summary>
        public float NearestDistanceTo(Transform t)
        {
            if (t == null) return float.MaxValue;
            Vector3 p = t.position; p.y = 0f;
            float best = float.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                var c = _active[i];
                if (c == null) continue;
                Vector3 cp = c.transform.position; cp.y = 0f;
                float d = (cp - p).sqrMagnitude;
                if (d < best) best = d;
            }
            return best < float.MaxValue ? Mathf.Sqrt(best) : float.MaxValue;
        }
    }

    /// <summary>Unidade de polícia. <c>encircle</c> = offset para circundar o alvo em vez de empilhar.</summary>
    public class PoliceCar : MonoBehaviour
    {
        [HideInInspector] public Vector3 encircle;
        [HideInInspector] public MeshRenderer giroA, giroB;
    }
}
