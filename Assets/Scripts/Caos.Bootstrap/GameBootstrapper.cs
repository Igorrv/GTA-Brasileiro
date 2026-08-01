using UnityEngine;

namespace Caos.Bootstrap
{
    /// <summary>
    /// Garante que um <see cref="GameManager"/> exista em qualquer cena ao entrar em Play Mode,
    /// mesmo numa cena vazia (padrão auto-bootstrap). Assim o backend de sistemas roda sem setup manual.
    /// </summary>
    public static class GameBootstrapper
    {
        private const string kManagerName = "[GameManager]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureGameManager()
        {
            if (GameObject.Find(kManagerName) != null) return;
            var go = new GameObject(kManagerName);
            go.AddComponent<GameManager>();
            Debug.Log("[Bootstrap] GameManager injetado na cena.");
        }
    }
}
