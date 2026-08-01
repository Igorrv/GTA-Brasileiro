namespace Caos.Core
{
    /// <summary>
    /// Porta de entrada da partida. Enquanto <see cref="Iniciado"/> for falso, o
    /// <c>GameManager</c> não carrega save e o <c>WorldBuilder</c> não levanta a cidade — quem libera
    /// é o menu inicial, depois que o jogador escolhe o slot.
    ///
    /// Fica no Core (sem dependências) justamente para que Bootstrap, Save e Simulation enxerguem o
    /// mesmo estado sem ninguém depender de ninguém.
    /// </summary>
    public static class GameSession
    {
        /// <summary>Falso até o jogador apertar "jogar" no menu inicial.</summary>
        public static bool Iniciado { get; private set; }

        /// <summary>Slot de save escolhido (1..3).</summary>
        public static int Slot { get; private set; } = 1;

        /// <summary>Verdadeiro quando o jogador escolheu começar do zero naquele slot.</summary>
        public static bool NovoJogo { get; private set; }

        public static void Iniciar(int slot, bool novoJogo)
        {
            Slot     = slot < 1 ? 1 : slot;
            NovoJogo = novoJogo;
            Iniciado = true;
        }

        /// <summary>Volta ao estado de "menu" (chamado no boot, já que estáticos sobrevivem ao Play Mode).</summary>
        public static void Reset()
        {
            Iniciado = false;
            NovoJogo = false;
            Slot     = 1;
        }
    }
}
