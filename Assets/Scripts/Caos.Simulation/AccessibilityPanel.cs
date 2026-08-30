using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Painel de Acessibilidade (docs/08). É um sistema novo e independente — não reescreve o
    /// <see cref="SettingsMenu"/> nem o <see cref="PauseMenu"/>. Abre por um botão pequeno no topo
    /// (acessível por toque, longe do joystick e dos botões de ação) e também pela tecla F6 no PC.
    ///
    /// Quatro ajustes, todos com efeito imediato (gravam em PlayerPrefs via
    /// <see cref="AccessibilitySettings"/>):
    ///  • Tamanho do texto (− / +)
    ///  • Cores para daltonismo (cicla Nenhum → Protan → Deutan → Tritan)
    ///  • Reduzir movimento (liga/desliga)
    ///  • Segurar p/ interagir (liga/desliga — alternativa hold-vs-tap)
    /// </summary>
    public class AccessibilityPanel : MonoBehaviour
    {
        private GameObject _painel;
        private Text _resumo;
        private Font _font;
        private GameObject _gatilho;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<AccessibilityPanel>() != null) return;
            var go = new GameObject("[A11Y]");
            go.AddComponent<AccessibilityPanel>();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Montar();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6)) Alternar();
        }

        public bool Aberto => _painel != null && _painel.activeSelf;

        public void Alternar()
        {
            bool abrir = !_painel.activeSelf;
            _painel.SetActive(abrir);
            if (_gatilho != null) _gatilho.SetActive(!abrir);
            if (abrir) AtualizarResumo();
        }

        private void AtualizarResumo()
        {
            if (_resumo == null) return;
            string cb = AccessibilitySettings.ColorblindMode switch
            {
                ColorblindMode.Protanopia => "Protanopia (vermelho)",
                ColorblindMode.Deutanopia => "Deutanopia (verde)",
                ColorblindMode.Tritanopia => "Tritanopia (azul)",
                _                          => "Nenhum (padrão)",
            };
            _resumo.text =
                $"Texto: {AccessibilitySettings.TextScale:F2}×\n" +
                $"Cores: {cb}\n" +
                $"Reduzir movimento: {(AccessibilitySettings.ReduceMotion ? "sim" : "não")}\n" +
                $"Segurar p/ interagir: {(AccessibilitySettings.HoldToInteract ? "sim" : "não")}";
        }

        // ------------------------------------------------------------------ UI
        private void Montar()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("A11YUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;   // acima do menu de ajustes (40), abaixo do menu inicial (200)
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // ---- gatilho: botão pequeno no topo-centro (área segura) ----
            var seguroGatilho = new GameObject("GatilhoArea", typeof(RectTransform));
            var sgrt = (RectTransform)seguroGatilho.transform;
            sgrt.SetParent(canvasGo.transform, false);
            SafeArea.Aplicar(sgrt, 6f);

            _gatilho = new GameObject("BtnA11Y", typeof(RectTransform));
            var grt = (RectTransform)_gatilho.transform;
            grt.SetParent(sgrt, false);
            grt.anchorMin = new Vector2(0.5f, 1f); grt.anchorMax = new Vector2(0.5f, 1f);
            grt.pivot = new Vector2(0.5f, 1f);
            grt.anchoredPosition = new Vector2(0, -6f);
            grt.sizeDelta = new Vector2(96f, 44f);
            var gimg = _gatilho.AddComponent<Image>();
            gimg.sprite = UiTextures.Arredondado(0.30f);
            gimg.type = Image.Type.Sliced;
            gimg.color = new Color(0.18f, 0.32f, 0.55f, 0.85f);
            var gbtn = _gatilho.AddComponent<Button>();
            gbtn.targetGraphic = gimg;
            var gc = gbtn.colors; gc.highlightedColor = new Color(0.30f, 0.50f, 0.80f); gbtn.colors = gc;
            gbtn.onClick.AddListener(Alternar);
            var glbl = new GameObject("Lbl", typeof(RectTransform));
            var glrt = (RectTransform)glbl.transform;
            glrt.SetParent(grt, false);
            glrt.anchorMin = Vector2.zero; glrt.anchorMax = Vector2.one;
            glrt.offsetMin = Vector2.zero; glrt.offsetMax = Vector2.zero;
            var gt = glbl.AddComponent<Text>();
            gt.font = _font; gt.fontSize = 22; gt.color = Color.white; gt.text = "A11Y";
            gt.alignment = TextAnchor.MiddleCenter; gt.raycastTarget = false;

            // ---- painel (fecha o gatilho enquanto aberto) ----
            _painel = new GameObject("Painel", typeof(RectTransform));
            var rt = (RectTransform)_painel.transform;
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _painel.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.96f);

            var seguro = new GameObject("AreaSegura", typeof(RectTransform));
            var seguroRt = (RectTransform)seguro.transform;
            seguroRt.SetParent(rt, false);
            SafeArea.Aplicar(seguroRt, 16f);

            Texto(seguroRt, "ACESSIBILIDADE", 56, new Color(0.55f, 0.78f, 1.00f), FontStyle.Bold, 350f);
            _resumo = Texto(seguroRt, "", 24, Color.white, FontStyle.Normal, 250f);
            _resumo.lineSpacing = 1.5f;

            Linha(seguroRt, "Tamanho do texto", 140f,
                  () => AccessibilitySettings.SetTextScale(AccessibilitySettings.TextScale - 0.1f),
                  () => AccessibilitySettings.SetTextScale(AccessibilitySettings.TextScale + 0.1f));

            Linha(seguroRt, "Cores (daltonismo)", 70f,
                  () => CycColorblind(-1),
                  () => CycColorblind(+1));

            Toggle(seguroRt, "Reduzir movimento", 0f,
                   () => AccessibilitySettings.SetReduceMotion(!AccessibilitySettings.ReduceMotion));

            Toggle(seguroRt, "Segurar p/ interagir", -70f,
                   () => AccessibilitySettings.SetHoldToInteract(!AccessibilitySettings.HoldToInteract));

            Botao(seguroRt, "FECHAR", new Vector2(0f, -250f), new Vector2(420f, 60f),
                  new Color(0.35f, 0.78f, 0.45f), Alternar);

            _painel.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private void CycColorblind(int dir)
        {
            int n = System.Enum.GetValues(typeof(ColorblindMode)).Length;
            int cur = (int)AccessibilitySettings.ColorblindMode;
            int nv = ((cur + dir) % n + n) % n;
            AccessibilitySettings.SetColorblind((ColorblindMode)nv);
            AtualizarResumo();
        }

        private void Linha(RectTransform pai, string rotulo, float y,
                           UnityEngine.Events.UnityAction menos, UnityEngine.Events.UnityAction mais)
        {
            var t = Texto(pai, rotulo, 26, new Color(0.86f, 0.86f, 0.89f), FontStyle.Normal, y);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0.5f, 0.5f); trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(-120f, y);
            trt.sizeDelta = new Vector2(520f, 44f);
            t.alignment = TextAnchor.MiddleRight;

            Botao(pai, "−", new Vector2(190f, y), new Vector2(64f, 52f), new Color(0.45f, 0.45f, 0.52f), () => { menos(); AtualizarResumo(); });
            Botao(pai, "+", new Vector2(270f, y), new Vector2(64f, 52f), new Color(0.45f, 0.45f, 0.52f), () => { mais();  AtualizarResumo(); });
        }

        private void Toggle(RectTransform pai, string rotulo, float y, UnityEngine.Events.UnityAction acao)
        {
            var t = Texto(pai, rotulo, 26, new Color(0.86f, 0.86f, 0.89f), FontStyle.Normal, y);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0.5f, 0.5f); trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(-120f, y);
            trt.sizeDelta = new Vector2(520f, 44f);
            t.alignment = TextAnchor.MiddleRight;

            Botao(pai, "alternar", new Vector2(230f, y), new Vector2(160f, 52f),
                  new Color(0.30f, 0.50f, 0.80f), () => { acao(); AtualizarResumo(); });
        }

        private Text Texto(RectTransform pai, string txt, int tamanho, Color cor, FontStyle estilo, float y)
        {
            var go = new GameObject("Txt", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pai, false);
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(-80f, tamanho * 4f);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.fontStyle = estilo; t.text = txt;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private void Botao(RectTransform pai, string rotulo, Vector2 pos, Vector2 tamanho, Color cor,
                           UnityEngine.Events.UnityAction acao)
        {
            var go = new GameObject("Btn_" + rotulo, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pai, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamanho;

            var img = go.AddComponent<Image>();
            img.sprite = UiTextures.Arredondado(0.28f);
            img.type = Image.Type.Sliced;
            img.color = new Color(cor.r * 0.5f, cor.g * 0.5f, cor.b * 0.5f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.highlightedColor = cor;
            btn.colors = cores;
            btn.onClick.AddListener(acao);

            var lbl = new GameObject("Rotulo", typeof(RectTransform));
            var lrt = (RectTransform)lbl.transform;
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var t = lbl.AddComponent<Text>();
            t.font = _font; t.fontSize = Mathf.RoundToInt(tamanho.y * 0.42f);
            t.color = Color.white; t.text = rotulo;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        }
    }
}
