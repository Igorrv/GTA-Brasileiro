#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caos.EditorTools
{
    /// <summary>
    /// Abre o jogo direto em Play Mode, sem nenhum setup manual: cria uma cena vazia e aperta o Play.
    /// O <see cref="Caos.Simulation.WorldBuilder"/> (RuntimeInitializeOnLoadMethod) monta a cidade sozinho.
    ///
    /// Dois usos:
    ///  • menu <b>Caos ▸ Jogar (cena vazia + Play)</b> dentro do Editor;
    ///  • linha de comando, para o Editor já abrir jogando:
    ///    <c>Unity.exe -projectPath . -executeMethod Caos.EditorTools.CaosPlay.Abrir</c>
    ///
    /// Diferente do <see cref="CaosPlaySmoke"/>, aqui <b>não</b> existe deadline nem Exit: a sessão fica
    /// aberta para jogar.
    /// </summary>
    public static class CaosPlay
    {
        [MenuItem("Caos/Jogar (cena vazia + Play) %#j")]
        public static void Abrir()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.Log("[Caos] Já está em Play Mode.");
                return;
            }

            // A cena aberta é sempre a untitled vazia (o projeto não versiona cenas — o mundo é
            // gerado em runtime). Ainda assim, respeita alteração não salva em vez de descartar.
            var atual = EditorSceneManager.GetActiveScene();
            if (atual.isDirty && !EditorUtility.DisplayDialog(
                    "Cidade do Caos",
                    "A cena aberta tem alterações não salvas. Criar uma cena vazia e jogar mesmo assim?",
                    "Jogar", "Cancelar"))
                return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("[Caos] Entrando em Play — o WorldBuilder monta São Genésio do Caos em ~1,5 s.");
            EditorApplication.EnterPlaymode();
        }
    }
}
#endif
