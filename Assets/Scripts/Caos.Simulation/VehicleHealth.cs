using Caos.Core;
using Caos.Gameplay;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Integridade do veículo (docs/04 §4.3). Colisões fortes tiram vida do motor; a 0 o carro para
    /// (o <see cref="VehicleController"/> corta o torque) <b>e fere o jogador</b> (caminho para WASTED via
    /// <see cref="PlayerAttributes"/>). Reparo só na oficina (<see cref="InteractionScanner"/>) por R$.
    ///
    /// Crime: colidir em alta velocidade com tráfego, polícia ou pedestre sobe o procurado
    /// (<see cref="CrimeSystem"/>); polícia e pedestre contam dobrado (gravidade).
    /// </summary>
    public class VehicleHealth : MonoBehaviour
    {
        private const float kMax = 100f;
        private const float kImpactThreshold = 5f;       // m/s — abaixo disso ignora (toques leves)
        private const float kPlayerHurtOnWreck = 25f;

        [SerializeField] private float _hp = kMax;
        private PlayerAttributes _attrs;
        private bool _broken;

        public float Hp   => _hp;
        public float Hp01 => _hp / kMax;
        public bool  Broken => _hp <= 0f;

        private ParticleSystem _fumaca;

        private void Start()
        {
            ServiceLocator.TryGet(out _attrs);
            MontarFumaca();
        }

        private void Update()
        {
            if (_fumaca == null) return;
            // motor ruim solta fumaça; motor fundido solta fumaça preta e grossa
            var emissao = _fumaca.emission;
            bool fumegando = Hp01 < 0.42f;
            emissao.enabled = fumegando;
            if (!fumegando) return;

            emissao.rateOverTime = Mathf.Lerp(28f, 4f, Hp01 / 0.42f);
            var main = _fumaca.main;
            main.startColor = Color.Lerp(new Color(0.10f, 0.10f, 0.10f, 0.85f),
                                         new Color(0.75f, 0.75f, 0.78f, 0.5f), Hp01 / 0.42f);
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

        /// <summary>Dano externo (buraco, incêndio, tiro) — a colisão continua com caminho próprio.</summary>
        public void AplicarDano(float quantidade)
        {
            if (quantidade <= 0f) return;
            _hp = Mathf.Max(0f, _hp - quantidade);
            if (_hp <= 0f && !_broken)
            {
                _broken = true;
                if (_attrs != null) _attrs.Apply("saude", -kPlayerHurtOnWreck * 0.5f);
                Debug.Log("[Veículo] Motor inutilizado — leve à oficina (F).");
            }
        }

        private void OnCollisionEnter(Collision c)
        {
            float impact = c.relativeVelocity.magnitude;
            if (impact < kImpactThreshold) return;

            _hp = Mathf.Max(0f, _hp - (impact - kImpactThreshold) * 2f);

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

            // virar sucata fere o jogador (caminho para WASTED)
            if (_hp <= 0f && !_broken)
            {
                _broken = true;
                if (_attrs != null) _attrs.Apply("saude", -kPlayerHurtOnWreck);
                Debug.Log("[Veículo] Motor inutilizado — leve à oficina (F).");
            }
        }

        public void RepairFull()
        {
            _hp = kMax;
            _broken = false;
            Debug.Log("[Veículo] Reparado.");
        }
    }
}
