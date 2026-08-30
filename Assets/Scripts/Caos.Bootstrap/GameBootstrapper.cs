using Caos.Core;
using UnityEngine;

namespace Caos.Bootstrap
{
    /// <summary>
    /// Garante que um <see cref="GameManager"/> exista em qualquer cena ao entrar em Play Mode,
    /// mesmo numa cena vazia (padrão auto-bootstrap). Assim o backend de sistemas roda sem setup manual.
    ///
    /// É também a <b>ponte entre o núcleo sem engine e o Unity</b>: as camadas de regra logam via
    /// <see cref="CaosLog"/>, e é aqui que esse log ganha um destino (o Console) e que o estado
    /// estático compartilhado é zerado antes de a primeira cena existir.
    /// </summary>
    public static class GameBootstrapper
    {
        private const string kManagerName = "[GameManager]";

        /// <summary>
        /// Primeira fase de inicialização, antes de qualquer cena. Roda antes de
        /// <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>, então tudo o que vier depois
        /// (MobilePerf, GameManager, WorldBuilder, MainMenu) já encontra o runtime limpo — não importa
        /// em que ordem esses pontos de entrada sejam chamados.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void PrepararRuntime()
        {
            CaosLog.Destino = EscreverNoConsole;

            // Na build de loja o Info some: a string do log é montada mesmo quando ninguém lê, e isso
            // custa alocação no celular. Editor e development build continuam falantes.
            CaosLog.Nivel = Debug.isDebugBuild ? NivelDeLog.Info : NivelDeLog.Aviso;

            // Estático sobrevive ao Play Mode sem recarga de domínio e ao retorno ao menu numa build.
            CaosRuntime.Reiniciar();
        }

        /// <summary>Único ponto do jogo que fala direto com o Console — todo o resto passa pelo CaosLog.</summary>
        private static void EscreverNoConsole(NivelDeLog nivel, string mensagem)
        {
            switch (nivel)
            {
                case NivelDeLog.Erro:  Debug.LogError(mensagem);   break;
                case NivelDeLog.Aviso: Debug.LogWarning(mensagem); break;
                default:               Debug.Log(mensagem);        break;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureGameManager()
        {
            if (GameObject.Find(kManagerName) != null) return;
            var go = new GameObject(kManagerName);
            go.AddComponent<GameManager>();
            CaosLog.Info("[Bootstrap] GameManager injetado na cena.");
        }
    }
}
