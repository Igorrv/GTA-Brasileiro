using Caos.Core;

namespace Caos.Gameplay
{
    /// <summary>
    /// Experiência e nível (docs/05). As missões já pagavam XP desde o começo, mas ninguém guardava —
    /// o número simplesmente evaporava. Aqui ele vira progressão: XP acumula, o nível sobe e cada
    /// nível <b>afrouxa a vida na cidade</b> em vez de dar um número abstrato.
    ///
    /// A curva é quadrática suave (<c>base × nível^1,45</c>): subir de 1 para 2 é rápido o bastante
    /// para ensinar que o sistema existe, e os níveis altos custam sem virar moagem.
    /// </summary>
    public sealed class ExperienceService
    {
        public const int NivelMaximo = 30;

        public float Xp    { get; private set; }
        public int   Nivel { get; private set; } = 1;

        /// <summary>XP acumulado necessário para chegar em <paramref name="nivel"/>.</summary>
        public static float XpParaNivel(int nivel)
        {
            if (nivel <= 1) return 0f;
            return 120f * CaosMath.Potencia(nivel - 1, 1.45f);
        }

        public float XpDoNivelAtual  => XpParaNivel(Nivel);
        public float XpDoProximo     => Nivel >= NivelMaximo ? XpDoNivelAtual : XpParaNivel(Nivel + 1);

        /// <summary>Progresso 0..1 dentro do nível atual — é o que a barra do HUD desenha.</summary>
        public float Progresso01
        {
            get
            {
                if (Nivel >= NivelMaximo) return 1f;
                float piso = XpDoNivelAtual, teto = XpDoProximo;
                return teto <= piso ? 0f : CaosMath.Limitar01((Xp - piso) / (teto - piso));
            }
        }

        /// <summary>
        /// Título do nível — o jogador entende "Motoboy" na hora, e não "nível 7".
        /// </summary>
        public string Titulo
        {
            get
            {
                if (Nivel >= 26) return "Dono da Cidade";
                if (Nivel >= 21) return "Figura Carimbada";
                if (Nivel >= 16) return "Conhecido na Área";
                if (Nivel >= 11) return "Rodando Firme";
                if (Nivel >= 7)  return "Motoboy Veterano";
                if (Nivel >= 4)  return "Entregador";
                if (Nivel >= 2)  return "Chegante";
                return "Recém-chegado";
            }
        }

        /// <summary>
        /// Vantagens por nível, aplicadas por quem consulta. São práticas de propósito: mais dinheiro
        /// por turno, menos cansaço e polícia que desiste mais rápido — coisas que o jogador sente
        /// dirigindo, não numa planilha.
        /// </summary>
        public float BonusPagamento   => 1f + (Nivel - 1) * 0.04f;   // +4% por nível no turno
        public float DescontoCansaco  => 1f - (Nivel - 1) * 0.015f;  // até −43% de desgaste no topo
        public float BonusDespiste    => 1f + (Nivel - 1) * 0.03f;   // estrelas caem mais rápido

        public void Adicionar(float xp, string motivo = "")
        {
            if (xp <= 0f) return;
            Xp += xp;

            int antes = Nivel;
            while (Nivel < NivelMaximo && Xp >= XpParaNivel(Nivel + 1)) Nivel++;

            EventBus<XpMudou>.Publish(new XpMudou { xp = Xp, nivel = Nivel, progresso = Progresso01 });

            if (Nivel > antes)
            {
                EventBus<SubiuDeNivel>.Publish(new SubiuDeNivel { nivel = Nivel, titulo = Titulo });
                CaosLog.Info($"[XP] Subiu para o nível {Nivel} — {Titulo}.");
            }
            else if (!string.IsNullOrEmpty(motivo))
            {
                CaosLog.Info($"[XP] +{xp:F0} ({motivo}) — total {Xp:F0}.");
            }
        }

        /// <summary>Restaura estado a partir do save.</summary>
        public void Hydrate(float xp, int nivel)
        {
            Xp = xp;
            Nivel = CaosMath.Limitar(nivel < 1 ? 1 : nivel, 1, NivelMaximo);
            EventBus<XpMudou>.Publish(new XpMudou { xp = Xp, nivel = Nivel, progresso = Progresso01 });
        }
    }

    public struct XpMudou      : IGameEvent { public float xp; public int nivel; public float progresso; }
    public struct SubiuDeNivel : IGameEvent { public int nivel; public string titulo; }
}
