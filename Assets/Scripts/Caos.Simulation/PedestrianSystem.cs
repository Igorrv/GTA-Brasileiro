using System.Collections.Generic;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Gente na rua (docs/12 §12.5). Os pedestres andam <b>na calçada</b> — ao longo das vias do
    /// <see cref="CityLayout"/>, com o recuo do meio-fio —, param de vez em quando (conversa de esquina)
    /// e viram nas quadras. Mesmo padrão de custo do tráfego: pool, Rigidbody kinematic e reciclagem por
    /// distância.
    ///
    /// São <b>alvos de crime</b>: atropelar sobe o procurado com severidade dobrada (<see cref="VehicleHealth"/>).
    /// </summary>
    public class PedestrianSystem : MonoBehaviour
    {
        [SerializeField] private int   maxActive     = 18;
        [SerializeField] private float recycleRadius = 95f;
        [SerializeField] private float spawnRadius   = 70f;
        [SerializeField] private float spawnInterval = 0.5f;

        private Transform                 _player;
        private CityLayout                _layout;
        private ObjectPool<Pedestrian>    _pool;
        private readonly List<Pedestrian> _active = new List<Pedestrian>();
        private float _spawnAccum;

        public void Init(Transform player)
        {
            _player = player;
            _layout = CityRuntime.Layout;
            _pool   = new ObjectPool<Pedestrian>(Factory, prewarm: maxActive);
            Fill();
        }

        private Pedestrian Factory()
        {
            var go = new GameObject("Pedestre");

            var cc = go.AddComponent<CapsuleCollider>();
            cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.88f, 0f);

            // mesmo rig e mesma passada procedural do protagonista — o pedestre anda de verdade.
            // O rig tem origem na cintura, então sobe 0,95 m para o pé encostar no chão.
            var corpo = new GameObject("Corpo");
            corpo.transform.SetParent(go.transform, false);
            corpo.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            var rig = CharacterRig.Construir(corpo.transform,
                camisa: CityPalette.CorViva(),
                calca:  kCalcas[Random.Range(0, kCalcas.Length)],
                pele:   kPeles[Random.Range(0, kPeles.Length)],
                bone:   CityPalette.CorViva());
            go.AddComponent<CharacterAnimator>().Init(rig);

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var p = go.AddComponent<Pedestrian>();
            go.SetActive(false);
            return p;
        }

        private static readonly Color[] kPeles =
        {
            new Color(0.36f, 0.24f, 0.17f), new Color(0.52f, 0.36f, 0.25f),
            new Color(0.68f, 0.50f, 0.36f), new Color(0.85f, 0.70f, 0.56f),
        };
        private static readonly Color[] kCalcas =
        {
            new Color(0.18f, 0.22f, 0.35f), new Color(0.22f, 0.22f, 0.24f),
            new Color(0.45f, 0.40f, 0.32f), new Color(0.30f, 0.35f, 0.30f),
        };

        private void Update()
        {
            if (_player == null || _pool == null) return;
            float dt = Time.deltaTime;
            Vector3 ppos = _player.position;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var c = _active[i];
                if (c == null) { _active.RemoveAt(i); continue; }

                c.pausaAte -= dt;
                if (c.pausaAte <= 0f)
                {
                    c.transform.position += c.dir * c.speed * dt;
                    if (c.dir.sqrMagnitude > 0.001f)
                        c.transform.rotation = Quaternion.LookRotation(c.dir, Vector3.up);

                    // parada de esquina / vira a quadra
                    if (Random.value < 0.0015f) c.pausaAte = Random.Range(1.5f, 4f);
                    else if (Random.value < 0.0025f) c.dir = Quaternion.Euler(0f, Random.value < 0.5f ? 90f : -90f, 0f) * c.dir;
                }

                if ((c.transform.position - ppos).sqrMagnitude > recycleRadius * recycleRadius)
                {
                    _pool.Release(c);
                    _active.RemoveAt(i);
                }
            }

            _spawnAccum += dt;
            if (_spawnAccum >= spawnInterval) { _spawnAccum = 0f; Fill(); }
        }

        /// <summary>
        /// Solta um pedestre correndo a partir de um ponto — é o motorista que acabou de ser tirado
        /// do carro. Reaproveita o pool: ele vira só mais um na rua, mas assustado e rápido.
        /// </summary>
        public void SoltarFugitivo(Vector3 pos, Vector3 direcao)
        {
            if (_pool == null || _pool.InPool == 0) return;

            var p = _pool.Get();
            p.transform.position = pos;
            p.dir      = new Vector3(direcao.x, 0f, direcao.z).normalized;
            p.speed    = Random.Range(4.2f, 5.6f);   // corre, não passeia
            p.pausaAte = 0f;
            p.transform.rotation = Quaternion.LookRotation(p.dir, Vector3.up);
            _active.Add(p);
        }

        private void Fill()
        {
            while (_active.Count < maxActive && _pool.InPool > 0)
            {
                var p = _pool.Get();
                if (!Posicionar(p)) { _pool.Release(p); break; }
                _active.Add(p);
            }
        }

        private bool Posicionar(Pedestrian p)
        {
            for (int k = 0; k < 16; k++)
            {
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.4f, spawnRadius);
                Vector3 alvo = _player.position + new Vector3(r.x, 0f, r.y);

                Vector3 pos;
                Vector3 dir;
                if (_layout != null)
                {
                    int eixo  = Random.value < 0.5f ? 0 : 1;
                    int linha = _layout.NearestLine(eixo == 0 ? alvo.x : alvo.z);
                    float lado = Random.value < 0.5f ? 1f : -1f;
                    float recuo = _layout.HalfWidth(linha) + 1.9f;   // em cima da calçada
                    float along = Mathf.Clamp(eixo == 0 ? alvo.z : alvo.x, -_layout.Extent + 15f, _layout.Extent - 15f);

                    pos = eixo == 0
                        ? new Vector3(_layout.LinePos(linha) + recuo * lado, 0.16f, along)
                        : new Vector3(along, 0.16f, _layout.LinePos(linha) + recuo * lado);
                    dir = eixo == 0 ? new Vector3(0f, 0f, Random.value < 0.5f ? 1f : -1f)
                                    : new Vector3(Random.value < 0.5f ? 1f : -1f, 0f, 0f);

                    if (Mathf.Abs(pos.x) > _layout.Extent || Mathf.Abs(pos.z) > _layout.Extent) continue;
                }
                else
                {
                    pos = alvo + new Vector3(0f, 0.16f, 0f);
                    Vector2 d = Random.insideUnitCircle.normalized;
                    dir = new Vector3(d.x, 0f, d.y);
                }

                if ((pos - _player.position).sqrMagnitude < 12f * 12f) continue;

                p.transform.position = pos;
                p.dir      = dir;
                p.speed    = Random.Range(1.0f, 1.9f);
                p.pausaAte = 0f;
                p.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                return true;
            }
            return false;
        }
    }

    /// <summary>Marcador de pedestre (estado volátil).</summary>
    public class Pedestrian : MonoBehaviour
    {
        [HideInInspector] public Vector3 dir;
        [HideInInspector] public float   speed;
        [HideInInspector] public float   pausaAte;
    }
}
