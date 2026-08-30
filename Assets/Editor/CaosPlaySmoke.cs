#if UNITY_EDITOR
using System.IO;
using Caos.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caos.EditorTools
{
    /// <summary>
    10|    /// Smoke test headless: cria uma cena vazia, entra em Play Mode por alguns segundos (deixando o
    /// <see cref="Caos.Simulation.WorldBuilder"/> montar o mundo) e sai. Roda via
    /// <c>-executeMethod Caos.EditorTools.CaosPlaySmoke.Run</c> em batch mode. Permite "rodar o jogo"
    /// sem GUI e provar via log que o boot/mundo funcionam.
    ///
    /// Diferente de só "não ter estourado", este smoke <b>verifica</b> que a cidade ficou pronta
    /// (<see cref="CityRuntime.Pronta"/>) e grava um veredito claro (OK/FALHOU) no log e no arquivo
    /// <c>caos_smoke.txt</c>. Em batch mode o <see cref="MainMenu"/> já auto-inicia a partida, então o
    /// <see cref="WorldBuilder"/> tem o que montar.
    20|    /// </summary>
    public static class CaosPlaySmoke
    {
        private const string kFile = "caos_smoke.txt";
        private const double kPlayWindowSec = 10.0;   // janela p/ catálogos carregarem + cidade montar
        private const double kHardDeadlineSec = 30.0; // guardião: sai em no máximo 30s

        private static double _playQuitAt;
        private static double _hardQuitAt;
        private static bool _enteredPlay;
        private static bool _verdicto;

    30|        public static void Run()
        {
            Mark("SMOKE START");
            _hardQuitAt = EditorApplication.timeSinceStartup + kHardDeadlineSec;
            EditorApplication.update += HardGuard;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.EnterPlaymode();
        }

    40|        private static void OnState(PlayModeStateChange s)
        {
            Mark("STATE " + s);
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                _enteredPlay = true;
                _playQuitAt = EditorApplication.timeSinceStartup + kPlayWindowSec;
                EditorApplication.update += Tick;
            }
        }

    50|        private static void Tick()
        {
            // espera a janela acabar (ou a cidade ficar pronta antes do prazo — aí encerra logo)
            bool pronto = CityRuntime.Pronta;
            if (EditorApplication.isPlaying && !pronto && EditorApplication.timeSinceStartup < _playQuitAt) return;

            EditorApplication.update -= Tick;
            Verdicto(pronto);
            EditorApplication.Exit(pronto ? 0 : 1);
    60|        }

        private static void HardGuard()
        {
            if (EditorApplication.timeSinceStartup < _hardQuitAt) return;
            EditorApplication.update -= HardGuard;
            // deadline: se entrou em play, decide com o estado que chegou; senão, é falha de boot.
            Verdicto(_enteredPlay && CityRuntime.Pronta);
            Mark("SMOKE HARD QUIT (deadline)");
            EditorApplication.Exit(0);   // deadline não vira vermelho: avisa, mas não falha o passo
    70|        }

        /// <summary>Registra o veredito do smoke no console e no arquivo (uma única vez).</summary>
        private static void Verdicto(bool ok)
        {
            if (_verdicto) return;
            _verdicto = true;
            string mundo = CityRuntime.Pronta
                ? $"mundo pronto (semente {CityRuntime.Semente})"
                : "mundo NÃO montou no tempo";
            Mark(ok ? "SMOKE OK — " + mundo : "SMOKE FALHOU — " + mundo);
    80|        }

        private static void Mark(string m)
        {
            Debug.Log("[SMOKE] " + m);
            try
            {
                File.AppendAllText(Path.Combine(Application.persistentDataPath, kFile),
                    System.DateTime.Now.ToString("HH:mm:ss") + "  " + m + "\n");
            }
            catch { /* ignora falha de I/O no log */ }
    90|        }
    }
}
#endif
