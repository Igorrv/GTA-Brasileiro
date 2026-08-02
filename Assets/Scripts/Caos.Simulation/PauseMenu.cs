using Caos.Core;
using Caos.Gameplay;
using Caos.Save;
using Caos.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Menu de pausa (docs/08). <b>Tab</b> ou <b>Esc</b> congela o jogo (<c>Time.timeScale = 0</c>, o que
    /// também para o tick do <c>GameManager</c>), mostra a ficha do jogador e permite salvar na hora,
    /// consultar os controles e sair.
    ///
    /// Salvar chama o mesmo <see cref="SaveSystem.Capture"/> do autosave, puxando os serviços do
    /// <see cref="ServiceLocator"/> — nada de estado duplicado.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private const string kControles =
            "W A S D / setas — andar · dirigir\n" +
            "Shift — correr        Espaço — freio\n" +
            "E — entrar / sair do veículo\n" +
            "F — usar posto, oficina, trabalho ou comprar\n" +
            "R — abastecer      Q — trocar de estação      Z — liga/desliga o rádio\n" +
            "M — mapa grande    Botão direito do mouse — girar a câmera\n" +
            "Tab / Esc — pausa";

        private GameObject _painel;
        private Text       _ficha;
        private bool       _pausado;
        private float      _escalaAnterior = 1f;

        private PlayerAttributes  _attrs;
        private EconomyService    _econ;
        private ReputationService _rep;
        private WorldStateService _world;
        private TimeOfDayService  _time;
        private MissionService    _missoes;
        private ExperienceService _xp;

        private void Awake() => Montar();

        private void Start()
        {
            ServiceLocator.TryGet(out _attrs);
            ServiceLocator.TryGet(out _econ);
            ServiceLocator.TryGet(out _rep);
            ServiceLocator.TryGet(out _world);
            ServiceLocator.TryGet(out _time);
            ServiceLocator.TryGet(out _missoes);
            ServiceLocator.TryGet(out _xp);
        }

        private void Update()
        {
            if (GameInput.Pause) Alternar();
        }

        public void Alternar()
        {
            _pausado = !_pausado;
            _painel.SetActive(_pausado);

            if (_pausado)
            {
                _escalaAnterior = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                AtualizarFicha();
            }
            else
            {
                Time.timeScale = _escalaAnterior;
            }
        }

        private void AtualizarFicha()
        {
            if (_ficha == null) return;

            string bairro = "—";
            var layout = CityRuntime.Layout;
            if (layout != null && _world != null)
            {
                var d = layout.DistrictById(_world.CurrentDistrict.ToString());
                bairro = d != null ? d.nome : _world.CurrentDistrict.ToString();
            }

            int h = _time != null ? (int)_time.Hour : 0;
            int m = _time != null ? (int)((_time.Hour - h) * 60f) : 0;
            int feitas = _missoes != null ? _missoes.CompletedSnapshot().Count : 0;

            _ficha.text =
                $"Dia {(_time != null ? _time.Day : 1)}  ·  {h:00}:{m:00}  ·  {bairro}\n" +
                $"R$ {(_econ != null ? _econ.Rs : 0f):N2}   ·   CaosCash {(_econ != null ? _econ.CaosCash : 0f):F0}   ·   IPC-Caos {(_econ != null ? _econ.IpcCaos : 0f):P1}\n" +
                $"Missões concluídas: {feitas}   ·   Procurado: {(_world != null ? _world.Stars : 0)}/5   ·   Caos {(_world != null ? (int)_world.Caos : 0)}";
        }

        // ------------------------------------------------------------------ salvar / sair
        private void Salvar()
        {
            if (_attrs == null || _econ == null || _rep == null || _world == null || _time == null || _missoes == null)
            {
                Toast("Serviços ainda carregando — tente em instantes.");
                return;
            }
            SaveSystem.Capture(_attrs, _econ, _rep, _world, _time, _missoes, _xp);
            Toast("Jogo salvo.");
        }

        private SettingsMenu _ajustes;

        /// <summary>Abre a tela de ajustes por cima da pausa (o jogo continua congelado).</summary>
        private void AbrirAjustes()
        {
            if (_ajustes == null) _ajustes = gameObject.AddComponent<SettingsMenu>();
            _ajustes.Alternar();
        }

        private void Sair()
        {
            Salvar();
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private Text _toast;
        private void Toast(string msg)
        {
            if (_toast != null) _toast.text = msg;
            Debug.Log("[Pausa] " + msg);
        }

        // ------------------------------------------------------------------ UI
        private void Montar()
        {
            // EventSystem qualificado: Caos.Gameplay também tem um tipo com esse nome (o de eventos do mundo).
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("MenuPausa", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            _painel = new GameObject("Painel", typeof(RectTransform));
            var rt = (RectTransform)_painel.transform;
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _painel.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.92f);

            Titulo(rt, font, "CIDADE DO CAOS", 78, new Color(0.98f, 0.82f, 0.20f), 300f);
            Titulo(rt, font, "São Genésio do Caos — mundo aberto brasileiro", 24, new Color(0.75f, 0.75f, 0.78f), 240f);

            _ficha = Titulo(rt, font, "", 24, Color.white, 150f);
            _ficha.lineSpacing = 1.35f;

            Botao(rt, font, "CONTINUAR", -20f, Alternar);
            Botao(rt, font, "SALVAR AGORA", -95f, Salvar);
            Botao(rt, font, "AJUSTES", -170f, AbrirAjustes);
            Botao(rt, font, "SAIR DO JOGO", -245f, Sair);

            var ctrl = Titulo(rt, font, kControles, 21, new Color(0.72f, 0.72f, 0.75f), -350f);
            ctrl.lineSpacing = 1.25f;

            _toast = Titulo(rt, font, "", 22, new Color(0.55f, 0.95f, 0.6f), -470f);

            _painel.SetActive(false);
        }

        private static Text Titulo(RectTransform parent, Font font, string texto, int tamanho, Color cor, float y)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(-200f, tamanho * 5f);
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = tamanho; t.color = cor; t.text = texto;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void Botao(RectTransform parent, Font font, string texto, float y, UnityEngine.Events.UnityAction acao)
        {
            var go = new GameObject("Botao_" + texto, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(460f, 60f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.13f, 0.14f, 0.17f, 0.95f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.highlightedColor = new Color(0.98f, 0.82f, 0.20f, 0.35f);
            cores.pressedColor     = new Color(0.98f, 0.82f, 0.20f, 0.6f);
            btn.colors = cores;
            btn.onClick.AddListener(acao);

            var lblGo = new GameObject("Rotulo", typeof(RectTransform));
            var lrt = (RectTransform)lblGo.transform;
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var t = lblGo.AddComponent<Text>();
            t.font = font; t.fontSize = 28; t.color = Color.white; t.text = texto;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        }
    }
}
