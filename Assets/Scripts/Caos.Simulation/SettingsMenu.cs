using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Ajustes do jogo (docs/08). Abre pelo menu de pausa e grava em <see cref="PlayerPrefs"/> —
    /// preferência é da <b>máquina</b>, não do save: trocar de slot ou de mundo não deve baixar o
    /// volume nem inverter a câmera de novo.
    ///
    /// O que dá pra mexer é o que realmente incomoda: volume separado de música e efeitos,
    /// sensibilidade e inversão da câmera, e um nível de qualidade que muda sombra e pós-processamento
    /// de uma vez — quem está num aparelho fraco precisa de um botão só, não de sete.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        private const string kVolGeral = "caos_vol_geral";
        private const string kVolMusica= "caos_vol_musica";
        private const string kSensib   = "caos_sensibilidade";
        private const string kInverter = "caos_inverter_y";
        private const string kQualidade= "caos_qualidade";

        public static float VolumeGeral  { get; private set; } = 0.8f;
        public static float VolumeMusica { get; private set; } = 0.7f;
        public static float Sensibilidade{ get; private set; } = 1f;
        public static bool  InverterY    { get; private set; }
        /// <summary>0 = leve · 1 = equilibrado · 2 = bonito.</summary>
        public static int   Qualidade    { get; private set; } = 1;

        private GameObject _painel;
        private Font _font;
        private Text _resumo;

        /// <summary>Carrega as preferências. Chamado no boot, antes de qualquer UI existir.</summary>
        public static void Carregar()
        {
            VolumeGeral   = PlayerPrefs.GetFloat(kVolGeral,  0.8f);
            VolumeMusica  = PlayerPrefs.GetFloat(kVolMusica, 0.7f);
            Sensibilidade = PlayerPrefs.GetFloat(kSensib,    1f);
            InverterY     = PlayerPrefs.GetInt(kInverter, 0) == 1;
            Qualidade     = PlayerPrefs.GetInt(kQualidade, MobilePerf.Mobile ? 0 : 2);
            Aplicar();
        }

        /// <summary>Empurra as preferências para os sistemas que as consomem.</summary>
        public static void Aplicar()
        {
            AudioListener.volume = VolumeGeral;

            // qualidade em um botão só: sombra e pós-processamento andam juntos
            switch (Qualidade)
            {
                case 0:  QualitySettings.shadowDistance = 45f;  QualitySettings.lodBias = 0.7f; break;
                case 1:  QualitySettings.shadowDistance = 90f;  QualitySettings.lodBias = 1.0f; break;
                default: QualitySettings.shadowDistance = 150f; QualitySettings.lodBias = 1.4f; break;
            }

            var pos = FindObjectOfType<PostProcessing>();
            if (pos != null) pos.enabled = Qualidade > 0;

            PlayerPrefs.SetFloat(kVolGeral, VolumeGeral);
            PlayerPrefs.SetFloat(kVolMusica, VolumeMusica);
            PlayerPrefs.SetFloat(kSensib, Sensibilidade);
            PlayerPrefs.SetInt(kInverter, InverterY ? 1 : 0);
            PlayerPrefs.SetInt(kQualidade, Qualidade);
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Montar();
        }

        public void Alternar()
        {
            bool abrir = !_painel.activeSelf;
            _painel.SetActive(abrir);
            if (abrir) AtualizarResumo();
        }

        public bool Aberto => _painel != null && _painel.activeSelf;

        private void AtualizarResumo()
        {
            if (_resumo == null) return;
            string q = Qualidade == 0 ? "Leve" : Qualidade == 1 ? "Equilibrado" : "Bonito";
            _resumo.text =
                $"Volume geral: {VolumeGeral * 100f:F0}%          Música: {VolumeMusica * 100f:F0}%\n" +
                $"Sensibilidade da câmera: {Sensibilidade:F2}×     Inverter Y: {(InverterY ? "sim" : "não")}\n" +
                $"Qualidade: {q}  ·  sombra {QualitySettings.shadowDistance:F0} m  ·  pós-processamento {(Qualidade > 0 ? "ligado" : "desligado")}";
        }

        // ------------------------------------------------------------------ UI
        private void Montar()
        {
            var canvasGo = new GameObject("AjustesUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;                    // acima do menu de pausa
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

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

            Texto(seguroRt, "AJUSTES", 64, new Color(0.98f, 0.82f, 0.20f), FontStyle.Bold, 340f);
            _resumo = Texto(seguroRt, "", 24, Color.white, FontStyle.Normal, 230f);
            _resumo.lineSpacing = 1.5f;

            // volume
            Linha(seguroRt, "Volume geral", 120f,
                  () => { VolumeGeral = Mathf.Clamp01(VolumeGeral - 0.1f); Aplicar(); AtualizarResumo(); },
                  () => { VolumeGeral = Mathf.Clamp01(VolumeGeral + 0.1f); Aplicar(); AtualizarResumo(); });

            Linha(seguroRt, "Volume da música", 50f,
                  () => { VolumeMusica = Mathf.Clamp01(VolumeMusica - 0.1f); Aplicar(); AtualizarResumo(); },
                  () => { VolumeMusica = Mathf.Clamp01(VolumeMusica + 0.1f); Aplicar(); AtualizarResumo(); });

            Linha(seguroRt, "Sensibilidade da câmera", -20f,
                  () => { Sensibilidade = Mathf.Clamp(Sensibilidade - 0.1f, 0.2f, 3f); Aplicar(); AtualizarResumo(); },
                  () => { Sensibilidade = Mathf.Clamp(Sensibilidade + 0.1f, 0.2f, 3f); Aplicar(); AtualizarResumo(); });

            Linha(seguroRt, "Inverter eixo Y", -90f,
                  () => { InverterY = !InverterY; Aplicar(); AtualizarResumo(); },
                  () => { InverterY = !InverterY; Aplicar(); AtualizarResumo(); });

            Linha(seguroRt, "Qualidade", -160f,
                  () => { Qualidade = Mathf.Max(0, Qualidade - 1); Aplicar(); AtualizarResumo(); },
                  () => { Qualidade = Mathf.Min(2, Qualidade + 1); Aplicar(); AtualizarResumo(); });

            Botao(seguroRt, "VOLTAR", new Vector2(0f, -280f), new Vector2(420f, 60f),
                  new Color(0.35f, 0.78f, 0.45f), Alternar);

            _painel.SetActive(false);
        }

        /// <summary>Linha de ajuste: rótulo à esquerda, botões − e + à direita.</summary>
        private void Linha(RectTransform pai, string rotulo, float y,
                           UnityEngine.Events.UnityAction menos, UnityEngine.Events.UnityAction mais)
        {
            var t = Texto(pai, rotulo, 26, new Color(0.86f, 0.86f, 0.89f), FontStyle.Normal, y);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0.5f, 0.5f); trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(-120f, y);
            trt.sizeDelta = new Vector2(520f, 44f);
            t.alignment = TextAnchor.MiddleRight;

            Botao(pai, "−", new Vector2(190f, y), new Vector2(64f, 52f), new Color(0.45f, 0.45f, 0.52f), menos);
            Botao(pai, "+", new Vector2(270f, y), new Vector2(64f, 52f), new Color(0.45f, 0.45f, 0.52f), mais);
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
            t.font = _font; t.fontSize = Mathf.RoundToInt(tamanho.y * 0.45f);
            t.color = Color.white; t.text = rotulo;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        }
    }
}
