using Caos.Core;
using Caos.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Caos.Simulation
{
    /// <summary>
    /// Pós-processamento (o que a migração para URP destravou).
    ///
    /// O perfil é criado <b>em runtime</b>, como todo o resto do projeto — nenhum asset de Volume no
    /// disco para alguém precisar reatribuir. Quatro efeitos, escolhidos por retorno visual:
    ///
    ///  • <b>Tonemapping</b> — sem ele o céu procedural estoura em branco chapado ao meio-dia;
    ///  • <b>Bloom</b> — é o que faz o letreiro do boteco e o poste de sódio brilharem à noite;
    ///  • <b>Color grading</b> — puxa a paleta pro quente no fim de tarde e pro frio de madrugada,
    ///    que é metade da sensação de "hora do dia";
    ///  • <b>Vinheta</b> — fecha o canto e concentra o olho no centro.
    ///
    /// Tudo acompanha o relógio: de dia é discreto, à noite o bloom sobe e a imagem esfria.
    /// No celular o custo é real, então o bloom entra em resolução menor e a vinheta é mais barata.
    /// </summary>
    public class PostProcessing : MonoBehaviour
    {
        private Bloom            _bloom;
        private ColorAdjustments _cor;
        private Vignette         _vinheta;
        private TimeOfDayService _time;
        private float            _accum;

        public void Init()
        {
            ServiceLocator.TryGet(out _time);
            Montar();
        }

        private void Montar()
        {
            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            perfil.name = "CaosPosProcessamento";

            // ---- tonemapping: comprime o alto-alcance em algo que a tela mostra ----
            var tone = perfil.Add<Tonemapping>(true);
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.Neutral;   // ACES é mais bonito e bem mais caro no celular

            // ---- bloom ----
            _bloom = perfil.Add<Bloom>(true);
            _bloom.threshold.overrideState = true; _bloom.threshold.value = 1.05f;
            _bloom.intensity.overrideState = true; _bloom.intensity.value = 0.35f;
            _bloom.scatter.overrideState   = true; _bloom.scatter.value   = 0.62f;
            _bloom.tint.overrideState      = true; _bloom.tint.value      = new Color(1f, 0.94f, 0.82f);
            _bloom.highQualityFiltering.overrideState = true;
            _bloom.highQualityFiltering.value = !MobilePerf.Mobile;
            _bloom.downscale.overrideState = true;
            _bloom.downscale.value = MobilePerf.Mobile ? BloomDownscaleMode.Half : BloomDownscaleMode.Quarter;

            // ---- ajuste de cor ----
            _cor = perfil.Add<ColorAdjustments>(true);
            _cor.postExposure.overrideState = true; _cor.postExposure.value = 0.05f;
            _cor.contrast.overrideState     = true; _cor.contrast.value     = 8f;
            _cor.saturation.overrideState   = true; _cor.saturation.value   = 6f;
            _cor.colorFilter.overrideState  = true; _cor.colorFilter.value  = Color.white;

            // ---- vinheta ----
            _vinheta = perfil.Add<Vignette>(true);
            _vinheta.intensity.overrideState = true; _vinheta.intensity.value = 0.22f;
            _vinheta.smoothness.overrideState = true; _vinheta.smoothness.value = 0.45f;
            _vinheta.rounded.overrideState = true; _vinheta.rounded.value = false;

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.profile  = perfil;

            // a câmera precisa ser avisada de que quer pós-processamento
            var cam = Camera.main;
            if (cam != null)
            {
                var dados = cam.GetComponent<UniversalAdditionalCameraData>() ??
                            cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                dados.renderPostProcessing = true;
                dados.antialiasing = MobilePerf.Mobile ? AntialiasingMode.None : AntialiasingMode.FastApproximateAntialiasing;
            }

            Debug.Log($"[Pós] Bloom, tonemapping, color grading e vinheta ativos (perfil {(MobilePerf.Mobile ? "celular" : "PC")}).");
        }

        private void Update()
        {
            if (_time == null || _bloom == null) return;

            // 4 Hz basta: a luz do dia muda devagar, e isto é ajuste de imagem, não de gameplay
            _accum += Time.deltaTime;
            if (_accum < 0.25f) return;
            _accum = 0f;

            float h = _time.Hour;
            // 0 no meio-dia, 1 na madrugada — a mesma curva que o sol usa
            float noite = Mathf.Clamp01(1f - Mathf.Cos((h - 12f) / 12f * Mathf.PI) * 0.5f - 0.5f);
            noite = Mathf.Clamp01((Mathf.Abs(h - 13f) - 4f) / 6f);

            // à noite o bloom sobe (poste e letreiro estouram) e a imagem esfria
            _bloom.intensity.value = Mathf.Lerp(0.28f, 0.95f, noite);
            _bloom.threshold.value = Mathf.Lerp(1.15f, 0.75f, noite);

            // fim de tarde puxa pro âmbar; madrugada, pro azul
            bool fimDeTarde = h > 16.5f && h < 19f;
            var quente = new Color(1.06f, 0.98f, 0.88f);
            var frio   = new Color(0.86f, 0.92f, 1.08f);
            var alvo   = fimDeTarde ? quente : Color.Lerp(Color.white, frio, noite);
            _cor.colorFilter.value = Color.Lerp(_cor.colorFilter.value, alvo, 0.15f);
            _cor.saturation.value  = Mathf.Lerp(6f, -12f, noite);   // noite dessatura, como o olho vê

            _vinheta.intensity.value = Mathf.Lerp(0.20f, 0.34f, noite);
        }
    }
}
