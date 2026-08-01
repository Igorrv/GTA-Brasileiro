#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Caos.EditorTools
{
    /// <summary>
    /// Pré-configura os <b>Player Settings</b> para build iOS/Android (docs/12). Roda uma vez ao importar o
    /// projeto e fica disponível no menu <c>Caos/Build/Configurar iOS + Android</c>.
    ///
    /// IMPORTANTE: no <b>Windows</b> você pode DEFINIR todos esses settings, mas só pode BUILDAR Android —
    /// o módulo de plataforma iOS não existe no Editor Windows. Gerar IPA exige macOS + Xcode
    /// (ver README, seção "Build &amp; Deploy"). Este script apenas deixa o projeto pronto para quando você
    /// tiver um Mac (próprio, emprestado ou na nuvem).
    /// </summary>
    public static class CaosBuildSetup
    {
        private const string kBundleId    = "com.caosstudio.cidadedocaos";
        private const string kAppliedKey  = "Caos.BuildSetup.Applied.v1";

        [InitializeOnLoadMethod]
        private static void AutoApply()
        {
            // Aplica uma vez por sessão do Editor (não recalça a cada domain reload).
            if (!SessionState.GetBool(kAppliedKey, false))
            {
                Apply();
                SessionState.SetBool(kAppliedKey, true);
            }
        }

        [MenuItem("Caos/Build/Configurar iOS + Android")]
        public static void Apply()
        {
            PlayerSettings.companyName  = "CaosStudio";
            PlayerSettings.productName  = "Cidade do Caos";

            // Mundo aberto + direção: landscape.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            // Identificador (bundle id) igual em iOS e Android.
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,     kBundleId);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, kBundleId);

            // ---- iOS ----
            PlayerSettings.iOS.targetOSVersionString = "14.0";

            // ---- Android ----
            // Unity 6 exige API mínimo 26 (Android 8.0 Oreo). API 24 (7.0) gera erro de vermelho no Console.
            PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel26; // 8.0 Oreo
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            AssetDatabase.SaveAssets();
            Debug.Log("[Build] Player Settings configurados: " + kBundleId + " · landscape · iOS 14+ · Android 8.0+.");
        }
    }
}
#endif
