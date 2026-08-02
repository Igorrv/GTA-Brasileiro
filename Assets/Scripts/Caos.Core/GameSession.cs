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

        /// <summary>
        /// Semente do mundo. <b>É o contrato do multiplayer</b>: a cidade é gerada em runtime, então
        /// dois jogadores só estão no mesmo mundo se partirem da mesma semente. Em partida em rede o
        /// anfitrião sorteia, envia no handshake, e cada cliente chama <see cref="DefinirSemente"/>
        /// antes do mundo subir.
        ///
        /// Em jogo solo a semente é derivada do slot, para que o save 1 sempre reabra a mesma cidade.
        /// </summary>
        public static int Semente { get; private set; }

        /// <summary>Semente veio de fora (anfitrião/servidor) em vez de ser derivada do slot.</summary>
        public static bool SementeExterna { get; private set; }

        public static void Iniciar(int slot, bool novoJogo)
        {
            Slot     = slot < 1 ? 1 : slot;
            NovoJogo = novoJogo;
            Iniciado = true;
            if (!SementeExterna) Semente = SementeDoSlot(Slot);
        }

        /// <summary>Define a semente recebida da rede. Chamar <b>antes</b> de <see cref="Iniciar"/>.</summary>
        public static void DefinirSemente(int semente)
        {
            Semente = semente;
            SementeExterna = true;
        }

        /// <summary>
        /// Semente estável por slot: o mesmo save reabre exatamente a mesma cidade, em qualquer
        /// máquina. Números primos grandes só para espalhar bem slots vizinhos.
        /// </summary>
        private static int SementeDoSlot(int slot) => unchecked(slot * 73856093 ^ 0x5F3759DF);

        /// <summary>Volta ao estado de "menu" (chamado no boot, já que estáticos sobrevivem ao Play Mode).</summary>
        public static void Reset()
        {
            Iniciado = false;
            NovoJogo = false;
            Slot     = 1;
            Semente  = 0;
            SementeExterna = false;
        }
    }
}
