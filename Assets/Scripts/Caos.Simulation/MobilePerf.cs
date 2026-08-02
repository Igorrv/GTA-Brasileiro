using UnityEngine;
using UnityEngine.Rendering;

namespace Caos.Simulation
{
    /// <summary>
    /// Perfil gráfico e de performance aplicado no boot (docs/12 §12.7). Roda uma vez, antes de
    /// qualquer cena, independente do GameManager.
    ///
    /// O projeto está no <b>pipeline padrão (Built-in)</b>, e o QualitySettings do projeto vinha com
    /// <b>sombra desligada</b> e nenhuma luz por pixel — daí o visual chapado. Aqui é onde isso é
    /// corrigido, com dois perfis:
    ///
    ///  • <b>Celular</b> (alvo do projeto): sombra suave só perto, 1 luz por pixel, sem MSAA, névoa
    ///    curta. Segura 60 fps num aparelho intermediário.
    ///  • <b>PC</b>: sombra em 4 cascatas até 150 m, 3 luzes por pixel, MSAA 4×, anisotropia — é o
    ///    caminho para a versão robusta de PC sem trocar de código.
    ///
    /// Também liga o <b>céu procedural</b> (com sol de verdade no horizonte) e o <b>reflexo pelo
    /// skybox</b>, que é o que dá brilho no vidro e no metal do Standard shader de graça.
    /// </summary>
    public static class MobilePerf
    {
        /// <summary>Material do céu — o <see cref="DayNightLighting"/> muda a cor conforme a hora.</summary>
        public static Material Ceu { get; private set; }

        public static bool Mobile { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Mobile = Application.isMobilePlatform;

            // ---- quadro e física ----
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount  = 0;
            Screen.sleepTimeout         = SleepTimeout.NeverSleep;
            Time.fixedDeltaTime         = 1f / 50f;

            // ---- sombras (estavam desligadas no perfil do projeto) ----
            QualitySettings.shadows            = ShadowQuality.All;
            QualitySettings.shadowResolution   = Mobile ? ShadowResolution.Medium : ShadowResolution.High;
            QualitySettings.shadowProjection   = ShadowProjection.StableFit;
            QualitySettings.shadowCascades     = Mobile ? 2 : 4;
            QualitySettings.shadowDistance     = Mobile ? 65f : 150f;
            QualitySettings.shadowNearPlaneOffset = 2f;

            // ---- luzes e filtros ----
            QualitySettings.pixelLightCount       = Mobile ? 1 : 3;
            QualitySettings.antiAliasing          = Mobile ? 0 : 4;
            QualitySettings.anisotropicFiltering  = Mobile ? AnisotropicFiltering.Disable : AnisotropicFiltering.ForceEnable;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles         = !Mobile;
            QualitySettings.lodBias               = Mobile ? 0.8f : 1.4f;

            // ---- céu procedural + reflexo ambiente ----
            MontarCeu();

            // qual pipeline está de fato ativo — a cidade monta materiais em runtime e precisa casar
            var rp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            string pipeline = rp != null ? rp.GetType().Name : "Built-in";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Debug.Log($"[Perf] Pipeline: {pipeline} · shader dos materiais: {(shader != null ? shader.name : "Standard")}");

            Debug.Log($"[Perf] Perfil {(Mobile ? "CELULAR" : "PC")}: 60 fps, sombra {QualitySettings.shadowDistance:F0} m " +
                      $"em {QualitySettings.shadowCascades} cascatas, {QualitySettings.pixelLightCount} luz(es) por pixel, " +
                      $"MSAA {QualitySettings.antiAliasing}×, fixedDt={Time.fixedDeltaTime:F3}.");
        }

        private static void MontarCeu()
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                // sem o shader de céu (build enxuta), cai no ambiente plano e segue o jogo
                RenderSettings.ambientMode = AmbientMode.Flat;
                return;
            }

            Ceu = new Material(shader) { name = "CeuDoCaos" };
            Ceu.SetFloat("_SunSize", 0.045f);
            Ceu.SetFloat("_SunSizeConvergence", 6f);
            Ceu.SetFloat("_AtmosphereThickness", 1.15f);
            Ceu.SetColor("_SkyTint", new Color(0.52f, 0.62f, 0.78f));
            Ceu.SetColor("_GroundColor", new Color(0.32f, 0.30f, 0.28f));
            Ceu.SetFloat("_Exposure", 1.25f);

            RenderSettings.skybox = Ceu;

            // ambiente em gradiente: céu claro em cima, chão escuro embaixo. É isso que dá volume
            // às primitivas — com ambiente plano tudo parece recortado em cartolina.
            RenderSettings.ambientMode         = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.55f, 0.62f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.42f, 0.42f);
            RenderSettings.ambientGroundColor  = new Color(0.20f, 0.19f, 0.17f);

            // reflexo vindo do próprio céu: vidro e metal ganham brilho sem probe nenhuma
            RenderSettings.defaultReflectionMode       = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = Mobile ? 64 : 128;
            RenderSettings.reflectionIntensity         = 0.75f;
            DynamicGI.UpdateEnvironment();
        }
    }
}
