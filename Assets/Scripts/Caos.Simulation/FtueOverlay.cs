using System.Collections;
using Caos.Core;
using Caos.World;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// FTUE — First-Time User Experience (docs/08). Tutorial de boas-vindas que aparece só na
    /// <b>primeira sessão</b> de cada aparelho (marcador em PlayerPrefs, não no save — trocar de slot
    /// não reprime o jogador que já aprendeu). É um sistema novo: não reescreve PhoneUI nem
    /// PauseMenu, e se auto-cria como o MainMenu/WorldBuilder.
    ///
    /// Três passos curtos, no ritmo do jogador (não cronômetro):
    ///   1. <b>Andar</b>      — empurre o joystick / WASD. Avança ao detectar movimento.
    ///   2. <b>Entrar no carro</b> — chegue perto do carro e toque E. Avança ao sair a pé.
    ///   3. <b>Procurado</b>  — faça um crime (atropelar/roubar). Avança ao subir as estrelas.
    ///
    /// Tempo de teto: 90 s. Quem terminar os 3 passos antes, dispensa. Quem não fizer nada, dispensa
    /// sozinho com um "valeu, é só isso". Respeita <see cref="AccessibilitySettings.ReduceMotion"/>
    /// (sem fade animado) e <see cref="AccessibilitySettings.TextScale"/> (já aplicado pelo applier).
    /// </summary>
    public class FtueOverlay : MonoBehaviour
    {
        private const string kVisto = "caos_ftue_visto";
        private const float kTeto = 90f;
        private const float kPassoMin = 6f;   // cada passo dura no mínimo isso (não pisca)

        private Canvas _canvas;
        private GameObject _root;
        private CanvasGroup _cg;
        private Text _titulo, _corpo, _dica, _passos;
        private Font _font;

        private int _passo = 0;          // 0=andar, 1=carro, 2=procurado, 3=pronto
        private float _tempo;
        private float _passoAte;
        private bool _andou, _entrou, _procurado;
        private PlayerVehicleLink _link;
        private bool _foiApe;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Application.isBatchMode) return;        // CI headless não precisa de tutorial
            if (PlayerPrefs.GetInt(kVisto, 0) == 1) return;
            if (FindObjectOfType<FtueOverlay>() != null) return;
            var go = new GameObject("[FTUE]");
            go.AddComponent<FtueOverlay>();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Montar();
        }

        private void OnEnable()
        {
            EventBus<EstrelasMudou>.Subscribe(OnStars);
        }

        private void OnDisable()
        {
            EventBus<EstrelasMudou>.Unsubscribe(OnStars);
        }

        private void Start()
        {
            _root.SetActive(false);   // só aparece quando o mundo está no ar
        }

        private void Update()
        {
            if (_passo >= 3) return;

            // só roda depois do mundo montado e do jogador no controle
            if (!CityRuntime.Pronta) return;
            if (_link == null)
            {
                _link = FindObjectOfType<PlayerVehicleLink>();
                if (_link == null) return;
                _foiApe = _link.OnFoot;
                Mostrar();
            }

            _tempo += Time.deltaTime;

            // detecta andar (magnitude do movimento, teclado ou joystick)
            if (!_andou && GameInput.Move.sqrMagnitude > 0.25f)
                _andou = true;

            // detecta entrar no carro (saiu de a pé → dentro)
            if (!_entrou && _foiApe && !_link.OnFoot)
                _entrou = true;
            _foiApe = _link.OnFoot;

            AvancarSePronto();
            if (_passo >= 3) return;   // encerrou este quadro (sucesso ou teto) — nada mais a fazer
            AtualizarTela();

            if (_tempo >= kTeto)
                Encerrar(mostrarValeu: true);
        }

        private void OnStars(EstrelasMudou e)
        {
            if (e.valor > 0) _procurado = true;
        }

        private void AvancarSePronto()
        {
            if (Time.time < _passoAte) return;

            switch (_passo)
            {
                case 0: if (_andou)    { _passo = 1; _passoAte = Time.time + kPassoMin; } break;
                case 1: if (_entrou)   { _passo = 2; _passoAte = Time.time + kPassoMin; } break;
                case 2: if (_procurado) { _passo = 3; Encerrar(mostrarValeu: false); }    break;
            }
        }

        private void AtualizarTela()
        {
            if (!_root.activeSelf) return;
            switch (_passo)
            {
                case 0:
                    _titulo.text = "Bem-vindo a São Genésio";
                    _corpo.text  = "Empurre o joystick à esquerda pra andar.\nNo PC, use W A S D.";
                    _dica.text   = _andou ? "Isso! Agora o carro..." : "";
                    break;
                case 1:
                    _titulo.text = "Pega um carro";
                    _corpo.text  = "Chegue perto do carro e toque em E pra entrar.\nDepois, toque de novo pra sair.";
                    _dica.text   = _entrou ? "Boa. Agora a parte que rende notícia..." : "";
                    break;
                case 2:
                    _titulo.text = "Cuidado com as estrelas";
                    _corpo.text  = "Atropelar ou roubar chama a polícia.\nCada estrela = mais pressão. Tente uma vez (só pra ver).";
                    _dica.text   = _procurado ? "Viu como subiu? Esconde que ela esfria." : "";
                    break;
            }
            _passos.text = PassoMarcador();
        }

        private string PassoMarcador()
        {
            string Marcador(int i) => i < _passo ? "●" : i == _passo ? "○" : "·";
            return $"{Marcador(0)} andar   {Marcador(1)} carro   {Marcador(2)} procurado";
        }

        private void Mostrar()
        {
            _root.SetActive(true);
            _passoAte = Time.time + kPassoMin;
            AtualizarTela();
            // Reduzir movimento: entrada instantânea em vez de fade. (O pisca crítico do HUD e o
            // giroflex da polícia são dos donos respectivos — futuros ganchos documentados.)
            if (AccessibilitySettings.ReduceMotion || _cg == null) { if (_cg != null) _cg.alpha = 1f; return; }
            StartCoroutine(Fade(_cg, 0f, 1f, 0.4f));
        }

        private static IEnumerator Fade(CanvasGroup cg, float de, float para, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(de, para, t / dur);
                yield return null;
            }
            cg.alpha = para;
        }

        private void Encerrar(bool mostrarValeu)
        {
            _passo = 3;
            PlayerPrefs.SetInt(kVisto, 1);
            PlayerPrefs.Save();
            if (mostrarValeu && _root != null)
                StartCoroutine(ValeuEDispensa());
            else
                Destruir();
        }

        private IEnumerator ValeuEDispensa()
        {
            _titulo.text = "Valeu, é só isso";
            _corpo.text  = "Bom jogo. Toque no A11Y no topo pra ajustar texto e cores.";
            _dica.text   = "";
            _passos.text = "";
            yield return new WaitForSeconds(3f);
            Destruir();
        }

        private void Destruir()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ UI
        private void Montar()
        {
            var canvasGo = new GameObject("FTUE", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 30;   // acima do HUD(4) e do touch(10), abaixo dos menus (40+)
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _root = new GameObject("Raiz", typeof(RectTransform));
            var rt = (RectTransform)_root.transform;
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 300f);   // acima do prompt/feed, abaixo do topo
            rt.sizeDelta = new Vector2(900f, 220f);
            var bg = _root.AddComponent<Image>();
            bg.sprite = UiTextures.Arredondado(0.10f);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.86f);
            bg.raycastTarget = false;   // não bloqueia o toque nos botões do jogo
            _cg = _root.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;

            _titulo = Texto(rt, "", 40, new Color(0.98f, 0.82f, 0.20f), FontStyle.Bold, new Vector2(0, 78f));
            _corpo  = Texto(rt, "", 26, Color.white, FontStyle.Normal, new Vector2(0, 18f));
            _corpo.lineSpacing = 1.3f;
            _dica   = Texto(rt, "", 24, new Color(0.6f, 1f, 0.65f), FontStyle.Normal, new Vector2(0, -34f));
            _passos = Texto(rt, "", 22, new Color(0.7f, 0.8f, 1f), FontStyle.Normal, new Vector2(0, -74f));

            _root.SetActive(false);
        }

        private Text Texto(Transform pai, string txt, int tamanho, Color cor, FontStyle estilo, Vector2 pos)
        {
            var go = new GameObject("Txt", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pai, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-40f, tamanho * 4f);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.fontStyle = estilo; t.text = txt;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
