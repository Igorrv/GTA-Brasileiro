using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Buraco de asfalto — instituição nacional. É um gatilho raso na pista: quem passa por cima leva
    /// tranco proporcional à velocidade e estraga um pouco a suspensão
    /// (<see cref="VehicleController.PassarNoBuraco"/>).
    ///
    /// Visualmente é um remendo escuro afundado, com a borda irregular; alguns ganham o galho fincado
    /// dentro, que no Brasil é sinalização oficial.
    /// </summary>
    public class Buraco : MonoBehaviour
    {
        [SerializeField] private float profundidade = 1f;

        private void OnTriggerEnter(Collider other)
        {
            var vc = other.GetComponentInParent<VehicleController>();
            if (vc != null) vc.PassarNoBuraco(profundidade);
        }

        /// <summary>Monta o buraco (visual + gatilho) na posição dada.</summary>
        public static Buraco Criar(Transform pai, Vector3 pos, float raio, float fundura)
        {
            var go = new GameObject("Buraco");
            go.transform.SetParent(pai, false);
            go.transform.position = pos;

            // cava: disco escuro afundado + borda esfarelada
            CityPalette.Cyl(go.transform, "Cava", new Vector3(0f, 0.01f, 0f), raio * 2f, 0.10f,
                            CityPalette.Mat(new Color(0.05f, 0.05f, 0.06f), 0.02f, 0f));
            int lascas = Random.Range(3, 6);
            for (int i = 0; i < lascas; i++)
            {
                float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float r = raio * Random.Range(0.55f, 0.95f);
                CityPalette.Box(go.transform, "Lasca",
                    new Vector3(Mathf.Cos(a) * r, 0.03f, Mathf.Sin(a) * r),
                    new Vector3(Random.Range(0.25f, 0.6f), 0.06f, Random.Range(0.25f, 0.6f)),
                    CityPalette.Mat(new Color(0.14f, 0.14f, 0.15f), 0.05f, 0f), Random.Range(0f, 180f), false);
            }

            // o galho fincado: sinalização informal, e serve de aviso visual pro jogador
            if (Random.value < 0.35f)
            {
                var galho = CityPalette.Cyl(go.transform, "Galho", new Vector3(0f, 0.6f, 0f), 0.08f, 1.2f, CityPalette.Tronco);
                galho.transform.localRotation = Quaternion.Euler(Random.Range(-18f, 18f), 0f, Random.Range(-18f, 18f));
            }

            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size   = new Vector3(raio * 2f, 1.2f, raio * 2f);
            trigger.center = new Vector3(0f, 0.5f, 0f);

            CaosLayers.Marcar(go, CaosLayers.Gatilho);
            var b = go.AddComponent<Buraco>();
            b.profundidade = fundura;
            return b;
        }
    }
}
