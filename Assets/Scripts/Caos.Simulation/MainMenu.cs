using Caos.Core;
using Caos.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Menu inicial (docs/08 T00). É a primeira coisa que sobe no Play e segura a partida: enquanto o
    /// jogador não escolhe um slot, <see cref="GameSession.Iniciado"/> continua falso e nem o save é
    /// carregado nem a cidade é construída.
    ///
    /// Mostra os <b>3 slots</b> com o resumo lido direto do arquivo (dia, hora, R$, bairro, missões),
    /// e cada cartão oferece Continuar (se houver save), Novo jogo e Apagar.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private Canvas   _canvas;
        private Font     _font;
        private GameObject _raiz;
        private readonly Text[] _resumo = new Text[SaveSystem.Slots];
        private readonly Text[] _titulo = new Text[SaveSystem.Slots];
        private Text _rodape;

        private static readonly Color kOuro   = new Color(0.98f, 0.82f, 0.20f);
        private static readonly Color kVerde  = new Color(0.35f, 0.78f, 0.45f);
        private static readonly Color kCinza  = new Color(0.62f, 0.62f, 0.66f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            // estáticos sobrevivem entre Plays no Editor: sempre recomeça pelo menu
            GameSession.Reset();
            if (FindObjectOfType<MainMenu>() != null) return;
            var go = new GameObject("[MenuInicial]");
            go.AddComponent<MainMenu>();
        }

        private WorldHub _hub;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Montar();

            // fluxo: hub de mundos → escolha do mundo → slots daquele mundo
            _hub = gameObject.AddComponent<WorldHub>();
            _hub.Init(_font);
            if (_raiz != null) _raiz.SetActive(false);   // slots só depois de escolher o mundo

            // sem ninguém para clicar (smoke headless / CI), pega o primeiro mundo e começa
            if (Application.isBatchMode)
            {
                _hub.Fechar();
                Jogar(1, novo: true);
            }
        }

        /// <summary>Chamado pelo <see cref="WorldHub"/> quando o jogador entra num mundo.</summary>
        private void HubEscolheuMundo(Caos.Data.WorldDto mundo)
        {
            if (_raiz != null) _raiz.SetActive(true);
            AtualizarCartoes();
            if (_rodape != null)
                _rodape.text = $"Mundo: {mundo.nome}  ·  escolha em qual vida continuar";
        }

        private void Update()
        {
            // atalho: Enter continua o último slot com save (ou começa no 1)
            if (!GameSession.Iniciado && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                for (int s = 1; s <= SaveSystem.Slots; s++)
                    if (SaveSystem.Existe(s)) { Jogar(s, false); return; }
                Jogar(1, true);
            }
        }

        // ------------------------------------------------------------------ ações
        private void Jogar(int slot, bool novo)
        {
            if (novo) SaveSystem.Delete(slot);
            SaveSystem.SlotAtual = slot;
            GameSession.Iniciar(slot, novo);

            Debug.Log($"[Menu] Iniciando no slot {slot} ({(novo ? "novo jogo" : "continuar")}).");
            if (_raiz != null) _raiz.SetActive(false);
            enabled = false;
        }

        private void Apagar(int slot)
        {
            SaveSystem.Delete(slot);
            AtualizarCartoes();
            if (_rodape != null) _rodape.text = $"Slot {slot} apagado.";
        }

        private void AtualizarCartoes()
        {
            var infos = SaveSystem.PeekTodos();
            for (int i = 0; i < infos.Length; i++)
            {
                _titulo[i].text = $"SLOT {infos[i].slot}";
                _titulo[i].color = infos[i].existe ? kOuro : kCinza;
                _resumo[i].text = infos[i].existe
                    ? infos[i].Resumo + (string.IsNullOrEmpty(infos[i].salvoEm) ? "" : "\nsalvo em " + infos[i].salvoEm)
                    : "Slot vazio\nComece uma vida nova em São Genésio";
                _resumo[i].color = infos[i].existe ? Color.white : kCinza;
            }
        }

        // ------------------------------------------------------------------ UI
        private void Montar()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("MenuInicialUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;                       // acima de tudo, inclusive da tela de carga
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _raiz = new GameObject("Raiz", typeof(RectTransform));
            var raizRt = (RectTransform)_raiz.transform;
            raizRt.SetParent(canvasGo.transform, false);
            Esticar(raizRt);
            var fundo = _raiz.AddComponent<Image>();
            fundo.color = new Color(0.045f, 0.05f, 0.07f, 1f);

            // faixa de cor no topo (verde-amarelo, sem ser bandeira literal)
            var faixa = Filho(raizRt, "Faixa");
            faixa.anchorMin = new Vector2(0f, 1f); faixa.anchorMax = new Vector2(1f, 1f);
            faixa.pivot = new Vector2(0.5f, 1f);
            faixa.anchoredPosition = Vector2.zero;
            faixa.sizeDelta = new Vector2(0f, 8f);
            faixa.gameObject.AddComponent<Image>().color = kVerde;

            Texto(raizRt, "CIDADE DO CAOS", 96, kOuro, FontStyle.Bold, 300f, TextAnchor.MiddleCenter);
            Texto(raizRt, "Mundo aberto brasileiro  ·  São Genésio do Caos", 26, kCinza, FontStyle.Normal, 232f, TextAnchor.MiddleCenter);

            // ---- três cartões de slot ----
            for (int i = 0; i < SaveSystem.Slots; i++)
            {
                int slot = i + 1;
                var cartao = Filho(raizRt, "Slot" + slot);
                cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
                cartao.pivot = new Vector2(0.5f, 0.5f);
                cartao.anchoredPosition = new Vector2((i - 1) * 520f, 20f);
                cartao.sizeDelta = new Vector2(480f, 300f);
                cartao.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.12f, 0.15f, 0.96f);

                _titulo[i] = Texto(cartao, "SLOT " + slot, 34, kOuro, FontStyle.Bold, 118f, TextAnchor.MiddleCenter);
                _resumo[i] = Texto(cartao, "", 20, Color.white, FontStyle.Normal, 40f, TextAnchor.UpperCenter);
                _resumo[i].lineSpacing = 1.3f;

                Botao(cartao, "CONTINUAR", new Vector2(0f, -62f), new Vector2(400f, 52f), kVerde, () => Jogar(slot, false));
                Botao(cartao, "NOVO JOGO", new Vector2(0f, -118f), new Vector2(400f, 46f), new Color(0.25f, 0.45f, 0.85f), () => Jogar(slot, true));
                Botao(cartao, "apagar",    new Vector2(0f, -166f), new Vector2(180f, 34f), new Color(0.45f, 0.20f, 0.20f), () => Apagar(slot));
            }

            _rodape = Texto(raizRt, "Enter continua o primeiro slot com jogo salvo  ·  Tab pausa durante a partida",
                            20, kCinza, FontStyle.Normal, -240f, TextAnchor.MiddleCenter);

            AtualizarCartoes();
        }

        // ---- helpers ----
        private static void Esticar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static RectTransform Filho(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        private Text Texto(Transform pai, string txt, int tamanho, Color cor, FontStyle estilo, float y, TextAnchor alinhamento)
        {
            var rt = Filho(pai, "Txt");
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(-40f, tamanho * 4f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.fontStyle = estilo;
            t.text = txt; t.alignment = alinhamento; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private void Botao(Transform pai, string rotulo, Vector2 pos, Vector2 tamanho, Color cor, UnityEngine.Events.UnityAction acao)
        {
            var rt = Filho(pai, "Botao_" + rotulo);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamanho;

            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(cor.r * 0.55f, cor.g * 0.55f, cor.b * 0.55f, 1f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.highlightedColor = cor;
            cores.pressedColor     = Color.Lerp(cor, Color.white, 0.4f);
            btn.colors = cores;
            btn.onClick.AddListener(acao);

            var lbl = Filho(rt, "Rotulo");
            lbl.anchorMin = Vector2.zero; lbl.anchorMax = Vector2.one;
            lbl.offsetMin = Vector2.zero; lbl.offsetMax = Vector2.zero;
            var t = lbl.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = Mathf.RoundToInt(tamanho.y * 0.42f);
            t.color = Color.white; t.text = rotulo;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        }
    }
}
