using Caos.Core;
using Caos.World;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Conecta o relógio do jogo (<see cref="TimeOfDayService.Hour"/>) à luz solar.
    /// Um dia de jogo = 48 min reais (constante no backend). Aqui apenas *lê* o estado —
    /// nunca avança o tempo, que continua sendo responsabilidade do tick do serviço.
    ///
    /// Mapa: 06h nascer (horizonte Leste), 12h meio-dia (zênite), 18h pôr (Oeste),
    /// noite abaixo do horizonte. Intensidade e cor ambiente interpolam suavemente.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class DayNightLighting : MonoBehaviour
    {
        [Header("Sol")]
        [SerializeField] private float dayIntensity   = 1.3f;
        [SerializeField] private float duskIntensity  = 0.55f;
        [SerializeField] private float nightIntensity = 0.12f;

        [SerializeField] private Color dayColor   = new Color(1.00f, 0.96f, 0.86f);
        [SerializeField] private Color duskColor  = new Color(1.00f, 0.62f, 0.36f);
        [SerializeField] private Color nightColor = new Color(0.30f, 0.36f, 0.55f);

        [Header("Céu")]
        [SerializeField] private Color skyDia   = new Color(0.52f, 0.72f, 0.92f);
        [SerializeField] private Color skyDusk  = new Color(0.90f, 0.52f, 0.30f);
        [SerializeField] private Color skyNoite = new Color(0.06f, 0.08f, 0.15f);

        private Light             _light;
        private TimeOfDayService  _time;
        private Camera            _cam;
        private bool              _postesAcesos;
        private bool              _primeiraVez = true;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Directional;
            _light.shadows = LightShadows.Soft;
        }

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _time))
                Debug.LogWarning("[DayNight] TimeOfDayService ausente — luz ficará estática.");
        }

        private void Update()
        {
            if (_time == null) return;

            float h = _time.Hour; // 0..24

            // ângulo do sol: 6h→0° (nascer), 12h→90° (zênite), 18h→180° (pôr), 0/24h→-90° (meia-noite)
            float sunAngle = (h / 24f) * 360f - 90f;
            transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

            // fator de "dia" 0..1 a partir da altura do sol (clamp inferior em 0)
            float elev = Mathf.Clamp01((sunAngle + 12f) / 90f); // ~0 antes das 5h, 1 ao meio-dia

            // três faixas: noite → aurora/crepúsculo → dia, com fusão por elev
            float t = Mathf.SmoothStep(0f, 0.5f, elev);          // noite→meia-luz
            float d = Mathf.SmoothStep(0.4f, 0.95f, elev);        // meia-luz→dia

            _light.intensity = Mathf.Lerp(nightIntensity, duskIntensity, t) * (1f - d) + dayIntensity * d;
            _light.color     = Color.Lerp(nightColor, duskColor, t);
            _light.color     = Color.Lerp(_light.color, dayColor, d);

            RenderSettings.ambientLight = _light.color * Mathf.Lerp(0.35f, 0.9f, Mathf.Max(t, d));
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;

            // céu e névoa acompanham a hora: sem isso, a madrugada fica com céu de meio-dia
            Color ceu = Color.Lerp(skyNoite, skyDusk, t);
            ceu = Color.Lerp(ceu, skyDia, d);
            if (_cam == null) _cam = Camera.main;
            if (_cam != null) _cam.backgroundColor = ceu;
            RenderSettings.fogColor = ceu;

            // céu procedural: o sol é a própria luz direcional, então basta reger exposição e tintura
            if (MobilePerf.Ceu != null)
            {
                MobilePerf.Ceu.SetFloat("_Exposure", Mathf.Lerp(0.22f, 1.35f, Mathf.Max(t * 0.5f, d)));
                MobilePerf.Ceu.SetColor("_SkyTint", Color.Lerp(new Color(0.16f, 0.19f, 0.30f), new Color(0.52f, 0.62f, 0.78f), d));
                MobilePerf.Ceu.SetFloat("_AtmosphereThickness", Mathf.Lerp(2.1f, 1.05f, d));  // pôr do sol mais denso
            }

            // ambiente segue o céu (senão a noite fica com sombra "de dia" nos objetos)
            RenderSettings.ambientSkyColor     = ceu * 1.05f;
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.10f, 0.11f, 0.15f), new Color(0.42f, 0.42f, 0.42f), Mathf.Max(t * 0.6f, d));
            RenderSettings.ambientGroundColor  = Color.Lerp(new Color(0.05f, 0.05f, 0.06f), new Color(0.20f, 0.19f, 0.17f), d);

            // janela acesa: uma troca de material acende a cidade inteira
            CityPalette.AcenderJanelas(Mathf.Lerp(1.15f, 0f, d));

            // postes: acendem no fim da tarde, apagam de manhã (só troca no instante da virada)
            bool noite = h < 6f || h >= 18f;
            if (noite != _postesAcesos || _primeiraVez)
            {
                if (AcenderPostes(noite))
                {
                    _postesAcesos = noite;
                    _primeiraVez  = false;
                }
            }
        }

        /// <summary>Troca o material das luminárias. Falso se a cidade ainda não foi gerada.</summary>
        private bool AcenderPostes(bool acesos)
        {
            var gen = CityRuntime.Generator;
            if (gen == null || gen.Luminarias.Count == 0) return false;
            var mat = acesos ? CityPalette.LuzAcesa : CityPalette.MetalEscuro;
            for (int i = 0; i < gen.Luminarias.Count; i++)
                if (gen.Luminarias[i] != null) gen.Luminarias[i].sharedMaterial = mat;
            return true;
        }
    }
}
