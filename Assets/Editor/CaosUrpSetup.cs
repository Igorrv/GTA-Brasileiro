#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Caos.EditorTools
{
    /// <summary>
    /// Cria e liga o <b>Universal Render Pipeline</b> no projeto, por código.
    ///
    /// O projeto nasceu no pipeline padrão (Built-in) e todos os materiais são gerados em runtime por
    /// <c>CityPalette</c>, que já procura o shader do URP antes de cair no Standard. Ou seja: com o
    /// pacote instalado e o pipeline apontado, a cidade inteira migra sozinha — não há material de
    /// asset para reatribuir, que costuma ser a parte dolorosa de uma migração dessas.
    ///
    /// O que o URP entrega aqui e o Built-in não entregava: sombra em cascata configurável por
    /// perfil, iluminação por vértice barata para os props distantes, e principalmente um caminho
    /// pronto para pós-processamento (bloom, color grading, oclusão de ambiente) sem trocar de
    /// arquitetura de novo.
    ///
    /// Uso: menu <b>Caos ▸ Configurar URP</b>, ou <c>-executeMethod Caos.EditorTools.CaosUrpSetup.Run</c>.
    /// </summary>
    public static class CaosUrpSetup
    {
        private const string kPasta   = "Assets/Settings";
        private const string kRenderer= kPasta + "/CaosUrpRenderer.asset";
        private const string kPipeline= kPasta + "/CaosUrpAsset.asset";

        [MenuItem("Caos/Configurar URP")]
        public static void Run()
        {
            if (!Directory.Exists(kPasta)) Directory.CreateDirectory(kPasta);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(kRenderer);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "CaosUrpRenderer";
                AssetDatabase.CreateAsset(renderer, kRenderer);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(kPipeline);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "CaosUrpAsset";
                AssetDatabase.CreateAsset(pipeline, kPipeline);
            }

            Configurar(pipeline);

            // aponta o pipeline no Graphics e em todos os níveis de qualidade
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int atual = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(atual, false);

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[URP] Pipeline ligado: {kPipeline} (renderer {kRenderer}).");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Perfil de qualidade. Os números vêm do mesmo raciocínio do <c>MobilePerf</c>: sombra longa
        /// e em quatro cascatas no PC, curta e em duas no celular. Aqui eles passam a valer de verdade
        /// — no URP o <c>QualitySettings.shadowDistance</c> é ignorado, quem manda é este asset.
        /// </summary>
        private static void Configurar(UniversalRenderPipelineAsset p)
        {
            var so = new SerializedObject(p);

            Set(so, "m_ShadowDistance",        150f);
            Set(so, "m_ShadowCascadeCount",    4);
            Set(so, "m_Cascade4Split",         new Vector3(0.06f, 0.16f, 0.38f));
            Set(so, "m_SoftShadowsSupported",  true);
            Set(so, "m_MainLightShadowsSupported", true);
            Set(so, "m_MainLightShadowmapResolution", 2048);
            Set(so, "m_AdditionalLightsRenderingMode", 1);   // por pixel
            Set(so, "m_AdditionalLightShadowsSupported", false);
            Set(so, "m_MSAA",                  4);
            Set(so, "m_SupportsHDR",           true);
            Set(so, "m_RenderScale",           1f);
            Set(so, "m_UseSRPBatcher",         true);
            Set(so, "m_SupportsCameraDepthTexture",  true);
            Set(so, "m_SupportsCameraOpaqueTexture", false); // não usamos refração: economiza uma cópia

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(SerializedObject so, string caminho, float v)
        { var pr = so.FindProperty(caminho); if (pr != null) pr.floatValue = v; }
        private static void Set(SerializedObject so, string caminho, int v)
        { var pr = so.FindProperty(caminho); if (pr != null) pr.intValue = v; }
        private static void Set(SerializedObject so, string caminho, bool v)
        { var pr = so.FindProperty(caminho); if (pr != null) pr.boolValue = v; }
        private static void Set(SerializedObject so, string caminho, Vector3 v)
        { var pr = so.FindProperty(caminho); if (pr != null) pr.vector3Value = v; }
    }
}
#endif
