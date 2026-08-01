#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caos.EditorTools
{
    /// <summary>
    /// Smoke test headless: cria uma cena vazia, entra em Play Mode por alguns segundos (deixando o
    /// <see cref="Caos.Simulation.WorldBuilder"/> montar o mundo) e sai. Roda via
    /// <c>-executeMethod Caos.EditorTools.CaosPlaySmoke.Run</c> em batch mode. Permite "rodar o jogo"
    /// sem GUI e provar via log que o boot/mundo funcionam.
    /// </summary>
    public static class CaosPlaySmoke
    {
        private const string kFile = "caos_smoke.txt";
        private static double _playQuitAt;
        private static double _hardQuitAt;

        public static void Run()
        {
            Mark("SMOKE START");
            _hardQuitAt = EditorApplication.timeSinceStartup + 25.0; // guardião: sai em no máximo 25s
            EditorApplication.update += HardGuard;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.EnterPlaymode();
        }

        private static void OnState(PlayModeStateChange s)
        {
            Mark("STATE " + s);
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                _playQuitAt = EditorApplication.timeSinceStartup + 6.0; // deixa o mundo montar ~6s
                EditorApplication.update += Tick;
            }
        }

        private static void Tick()
        {
            if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup < _playQuitAt) return;
            EditorApplication.update -= Tick;
            Mark("SMOKE STOP PLAY");
            EditorApplication.Exit(0);
        }

        private static void HardGuard()
        {
            if (EditorApplication.timeSinceStartup < _hardQuitAt) return;
            EditorApplication.update -= HardGuard;
            Mark("SMOKE HARD QUIT (deadline)");
            EditorApplication.Exit(0);
        }

        private static void Mark(string m)
        {
            Debug.Log("[SMOKE] " + m);
            try
            {
                File.AppendAllText(Path.Combine(Application.persistentDataPath, kFile),
                    System.DateTime.Now.ToString("HH:mm:ss") + "  " + m + "\n");
            }
            catch { /* ignora */ }
        }
    }
}
#endif
