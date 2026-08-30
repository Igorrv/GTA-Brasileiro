using System;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Preferências de acessibilidade do <b>aparelho</b> (docs/08). Ao contrário do save, isso é
    /// gosto da pessoa que segura o celular: trocar de slot não pode diminuir o texto de alguém com
    /// baixa visão, nem religar o pisca-pisca de quem pediu para reduzir movimento. Por isso vivem
    /// no <see cref="PlayerPrefs"/> — o mesmo lugar onde já vivem volume e sensibilidade da câmera
    /// (<see cref="SettingsMenu"/>), e não no <see cref="Caos.Save.SaveSystem"/>.
    ///
    /// Quatro ajustes, todos com efeito imediato via <see cref="Aplicar"/>:
    ///  • <see cref="TextScale"/>        — escala o tamanho da fonte de toda a UI (0,8× a 1,6×).
    ///  • <see cref="ColorblindMode"/>   — paleta segura para daltonismo nas estrelas de procurado.
    ///  • <see cref="ReduceMotion"/>     — corta animações decorativas (piscar crítico, transições).
    ///  • <see cref="HoldToInteract"/>   — alterna entre "tocar" e "segurar" para confirmar ações.
    ///
    /// Sistemas que respeitam essas preferências se inscrevem em <see cref="Mudou"/> e re-aplicam.
    /// </summary>
    public static class AccessibilitySettings
    {
        // ---- chaves do PlayerPrefs (mesmo prefixo 'caos_' do SettingsMenu) ----
        private const string kTextScale     = "caos_a11y_text_scale";
        private const string kColorblind    = "caos_a11y_colorblind";
        private const string kReduceMotion  = "caos_a11y_reduce_motion";
        private const string kHoldInteract  = "caos_a11y_hold_interact";

        /// <summary>Escala de texto: 0,8 = compacto · 1,0 = padrão · 1,6 = grande.</summary>
        public static float TextScale { get; private set; } = 1f;

        /// <summary>Tipo de daltonismo que guia a paleta do HUD.</summary>
        public static ColorblindMode ColorblindMode { get; private set; } = ColorblindMode.Nenhum;

        /// <summary>Verdadeiro para reduzir movimento decorativo (pisca, tremor, transições longas).</summary>
        public static bool ReduceMotion { get; private set; }

        /// <summary>
        /// Verdadeiro para confirmar interações <b>segurando</b> o botão em vez de um toque único.
        /// É a alternativa de mobilidade ao padrão tap-and-go do <see cref="TouchControls"/>.
        /// </summary>
        public static bool HoldToInteract { get; private set; }

        /// <summary>Disparado quando qualquer preferência muda. Sistemas se inscrevem para re-aplicar.</summary>
        public static event Action Mudou;

        /// <summary>Carrega do PlayerPrefs. Chamado no boot, antes de qualquer UI existir.</summary>
        public static void Carregar()
        {
            TextScale      = ClampScale(PlayerPrefs.GetFloat(kTextScale, 1f));
            ColorblindMode = (ColorblindMode)PlayerPrefs.GetInt(kColorblind, (int)ColorblindMode.Nenhum);
            ReduceMotion   = PlayerPrefs.GetInt(kReduceMotion, 0) == 1;
            HoldToInteract = PlayerPrefs.GetInt(kHoldInteract, 0) == 1;
        }

        /// <summary>Empurra as preferências para os sistemas que as consomem e grava no disco.</summary>
        public static void Aplicar()
        {
            PlayerPrefs.SetFloat(kTextScale, TextScale);
            PlayerPrefs.SetInt(kColorblind, (int)ColorblindMode);
            PlayerPrefs.SetInt(kReduceMotion, ReduceMotion ? 1 : 0);
            PlayerPrefs.SetInt(kHoldInteract, HoldToInteract ? 1 : 0);
            PlayerPrefs.Save();
            Mudou?.Invoke();
        }

        // ---- setters chamados pela UI de acessibilidade ----
        public static void SetTextScale(float v)
        {
            TextScale = ClampScale(v);
            Aplicar();
        }

        public static void SetColorblind(ColorblindMode m)
        {
            ColorblindMode = m;
            Aplicar();
        }

        public static void SetReduceMotion(bool v)
        {
            ReduceMotion = v;
            Aplicar();
        }

        public static void SetHoldToInteract(bool v)
        {
            HoldToInteract = v;
            Aplicar();
        }

        private static float ClampScale(float v) => Mathf.Clamp(v, 0.8f, 1.6f);

        // ------------------------------------------------------------------
        //  Paleta segura para daltonismo
        // ------------------------------------------------------------------
        // As estrelas de procurado, no padrão, são OURO (ativo) x CINZA (apagado). Ouro x cinza já
        // é distinguível por luminância — mas o "quente" do ouro some para quem tem protanopia, e o
        // nível vira um amarelo esverdeado difícil de ler contra o painel escuro. A paleta abaixo
        // troca por AZUL VIVO x CINZA ESCURO: azul mantém contraste nos três tipos de daltonismo e o
        // cinza escuro dá o "apagado" sem depender de matiz. O nome do nível ("ROTA na área" etc.)
        // continua aparecendo em texto — então a informação nunca é só cor.
        //
        // Por que não mexer nas barras de necessidade (Vida/Fome/Sede/Energia/Sanidade): elas são
        // re-coloridas a cada quadro pelo HudController (pisca crítico), e o HudController é dono
        // exclusivo dessas cores. Em vez de brigar com ele, a acessibilidade já ganha pelo lado do
        // texto: cada barra tem rótulo ("Vida", "Fome"...) e valor numérico legíveis, então a cor é
        // redundância, não o único canal. A paleta das estrelas é o ganho real e limpo.
        // ------------------------------------------------------------------

        /// <summary>Cor da estrela acesa (procurado ativo), já respeitando o modo de daltonismo.</summary>
        public static Color EstrelaAcesa
        {
            get
            {
                switch (ColorblindMode)
                {
                    case ColorblindMode.Protanopia:
                    case ColorblindMode.Deutanopia:
                    case ColorblindMode.Tritanopia:
                        return new Color(0.20f, 0.55f, 1.00f);   // azul vivo — seguro nos 3 tipos
                    default:
                        return new Color(1.00f, 0.84f, 0.28f);   // ouro (padrão do jogo)
                }
            }
        }

        /// <summary>Cor da estrela apagada (procurado inativo).</summary>
        public static Color EstrelaApagada
        {
            get
            {
                switch (ColorblindMode)
                {
                    case ColorblindMode.Protanopia:
                    case ColorblindMode.Deutanopia:
                    case ColorblindMode.Tritanopia:
                        return new Color(0.22f, 0.22f, 0.26f, 0.92f); // cinza escuro
                    default:
                        return new Color(0.28f, 0.28f, 0.30f, 0.85f); // cinza padrão
                }
            }
        }

        /// <summary>Cor do texto do nível de procurado, respeitando o modo de daltonismo.</summary>
        public static Color TextoProcurado
        {
            get
            {
                switch (ColorblindMode)
                {
                    case ColorblindMode.Protanopia:
                    case ColorblindMode.Deutanopia:
                    case ColorblindMode.Tritanopia:
                        return new Color(0.55f, 0.78f, 1.00f);   // azul-claro legível
                    default:
                        return new Color(1f, 0.45f, 0.4f);       // vermelho-rosa (padrão)
                }
            }
        }

        // ------------------------------------------------------------------
        //  Hold vs Tap — a alternativa de mobilidade, documentada
        // ------------------------------------------------------------------
        // O <see cref="TouchControls"/> já mistura os dois estilos:
        //   • SEGURAR (Hold): FREIO, CORRER, AGACHAR — ações contínuas que fazem sentido enquanto o
        //     dedo está apertado. Solta = para.
        //   • TOCAR  (Tap) : E (interagir), F (usar), R (abastecer), SENTAR, BUZINA, RÁDIO, FONE, II
        //     — confirmações de um toque só.
        //
        // Para quem tem tremor ou dificuldade de motor fina, "tocar e soltar rápido" pode escapar o
        // dedo. A preferência <see cref="HoldToInteract"/> oferece a alternativa: segurar o botão de
        // ação por um tempo curto (~0,35 s) confirma, em vez de um toque. Um medidor visual de
        // progresso aparece no botão para dar feedback.
        //
        // Por que isto é só uma preferência registrada, e não já ligado no InteractionScanner: o
        // scanner é o dono da leitura de "Use" (tecla F / botão F), e está fora do alcance deste
        // slice (PR paralelo o edita). A preferência fica pronta aqui em PlayerPrefs; quando o
        // scanner passar a consultá-la, o comportamento liga sem mudar nada nesta classe. É o
        // gancho documentado — não uma promessa vazia, é o ponto exato de integração.
        // ------------------------------------------------------------------
    }

    /// <summary>Tipos de daltonismo cobertos pela paleta segura do HUD.</summary>
    public enum ColorblindMode
    {
        Nenhum = 0,
        Protanopia = 1,
        Deutanopia = 2,
        Tritanopia = 3,
    }
}
