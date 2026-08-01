using System;

namespace Caos.Core
{
    /// <summary>Estado da máquina global (Boot → MainMenu → Loading → Playing → Paused).</summary>
    public interface IGameState
    {
        string Name { get; }
        void Enter() { }
        void Exit() { }
        void Tick(float dt) { }
    }

    /// <summary>
    /// Máquina de estados simples pushdown. Mantém a fase atual do jogo.
    /// Sistpios consultam <see cref="Current"/> para saber se devem simular (Playing) ou não.
    /// </summary>
    public sealed class GameStateMachine
    {
        public IGameState Current { get; private set; }

        public void ChangeState(IGameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            Current?.Exit();
            Current = state;
            UnityEngine.Debug.Log($"[FSM] -> {state.Name}");
            Current.Enter();
        }

        public void Tick(float dt) => Current?.Tick(dt);
    }

    // ---- Estados concretos (minimal) ----
    public sealed class BootState : IGameState { public string Name => "Boot"; }
    public sealed class MainMenuState : IGameState { public string Name => "MainMenu"; }
    public sealed class LoadingState : IGameState { public string Name => "Loading"; }
    public sealed class PlayingState : IGameState { public string Name => "Playing"; }
    public sealed class PausedState : IGameState { public string Name => "Paused"; }
    public sealed class CutsceneState : IGameState { public string Name => "Cutscene"; }
}
