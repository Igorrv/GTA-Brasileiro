using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Painel do carro (docs/08 T05): dois mostradores redondos com <b>ponteiro</b> — conta-giros à
    /// esquerda e velocímetro à direita —, marcha no meio, ponteiro de combustível e as luzes de aviso
    /// que todo carro brasileiro tem: <b>reserva</b>, <b>motor</b> e <b>freio de mão</b>.
    ///
    /// Os mostradores são redondos de verdade porque as texturas de disco/anel são geradas em runtime
    /// (<see cref="UiTextures"/>) — o projeto não tem nenhum arquivo de imagem. O painel só aparece
    /// dirigindo e some a pé, e fica ancorado embaixo ao centro para caber na tela do celular sem
    /// brigar com o radar (esquerda) nem com o dinheiro (direita).
    /// </summary>
    public class DashboardUI : MonoBehaviour
    {
        private const float kAnguloMin = 210f;   // ponteiro no zero
        private const float kAnguloMax = -30f;   // ponteiro no fundo de escala

        private PlayerVehicleLink _link;
        private VehicleController _vehicle;
        private VehicleHealth     _health;

        private GameObject   _painel;
        private RectTransform _ponteiroGiro, _ponteiroVel, _ponteiroComb;
        private Text          _marcha, _velNumero, _modelo, _litros;
        private Text[]        _reguaMarchas = new Text[6];   // R, 1, 2, 3, 4, 5
        private Image         _avisoReserva, _avisoMotor, _avisoFreio;
        private Font          _font;

        private static readonly Color kMostrador = new Color(0.07f, 0.075f, 0.09f, 0.94f);
        private static readonly Color kAro       = new Color(0.55f, 0.57f, 0.62f, 0.9f);
        private static readonly Color kPonteiro  = new Color(0.95f, 0.30f, 0.25f);
        private static readonly Color kApagado   = new Color(0.22f, 0.22f, 0.25f, 0.85f);

        public void Init(PlayerVehicleLink link, VehicleController vehicle, VehicleHealth health)
        {
            _link = link; _vehicle = vehicle; _health = health;
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Montar();
        }

        public void DefinirModelo(string nome) { if (_modelo != null) _modelo.text = nome; }

        private void Update()
        {
            bool dirigindo = _link != null && !_link.OnFoot && _vehicle != null;
            if (_painel.activeSelf != dirigindo) _painel.SetActive(dirigindo);
            if (!dirigindo) return;

            // ---- ponteiros ----
            _ponteiroGiro.localRotation = Quaternion.Euler(0f, 0f, Angulo(_vehicle.Rpm01));
            float vel01 = Mathf.Clamp01(Mathf.Abs(_vehicle.SpeedKmh) / 220f);
            _ponteiroVel.localRotation  = Quaternion.Euler(0f, 0f, Angulo(vel01));
            _ponteiroComb.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-42f, 42f, _vehicle.Fuel01));

            _velNumero.text = ((int)Mathf.Abs(_vehicle.SpeedKmh)).ToString();
            _marcha.text    = _vehicle.MarchaTxt;

            // régua de marchas: mostra R 1 2 3 4 5 e acende a atual — dá pra ver o câmbio trabalhando
            for (int i = 0; i < _reguaMarchas.Length; i++)
            {
                bool atual = (i == 0 && _vehicle.Marcha == 0) || i == _vehicle.Marcha;
                _reguaMarchas[i].color = atual ? new Color(0.98f, 0.82f, 0.20f) : new Color(0.42f, 0.42f, 0.46f);
                _reguaMarchas[i].fontStyle = atual ? FontStyle.Bold : FontStyle.Normal;
            }

            // gasolina em litros e porcentagem: "12,4 L · 26%" é informação, "meia barra" não é
            if (_litros != null)
            {
                float litros = _vehicle.Fuel;
                _litros.text  = $"{litros:F1} L  ·  {_vehicle.Fuel01 * 100f:F0}%  de {_vehicle.TankLiters:F0} L";
                _litros.color = _vehicle.Fuel01 < 0.15f ? new Color(0.98f, 0.45f, 0.25f)
                                                        : new Color(0.72f, 0.74f, 0.78f);
            }
            _marcha.color   = _vehicle.Derrapando ? new Color(1f, 0.5f, 0.35f) : new Color(0.98f, 0.82f, 0.20f);

            // ---- luzes de aviso ----
            bool reserva = _vehicle.Fuel01 < 0.15f;
            bool motor   = _health != null && _health.Hp01 < 0.35f;
            bool freio   = GameInput.Handbrake;
            float pisca  = 0.55f + 0.45f * Mathf.Sin(Time.time * 5f);

            _avisoReserva.color = reserva ? new Color(0.98f, 0.62f, 0.10f, pisca) : kApagado;
            _avisoMotor.color   = motor   ? new Color(0.95f, 0.25f, 0.20f, pisca) : kApagado;
            _avisoFreio.color   = freio   ? new Color(0.95f, 0.25f, 0.20f, 1f)    : kApagado;
        }

        private static float Angulo(float t) => Mathf.Lerp(kAnguloMin, kAnguloMax, Mathf.Clamp01(t));

        // ------------------------------------------------------------------ montagem
        private void Montar()
        {
            var canvasGo = new GameObject("PainelDoCarro", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;      // celular deitado: escala pela altura

            _painel = new GameObject("Painel", typeof(RectTransform));
            var rt = (RectTransform)_painel.transform;
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 18f);
            rt.sizeDelta = new Vector2(560f, 210f);

            var fundo = _painel.AddComponent<Image>();
            fundo.sprite = UiTextures.Arredondado(0.18f);
            fundo.type = Image.Type.Sliced;
            fundo.color = new Color(0.04f, 0.045f, 0.06f, 0.80f);
            fundo.raycastTarget = false;

            // ---- mostradores ----
            _ponteiroGiro = Mostrador(rt, new Vector2(-165f, 104f), 176f, "x1000", out _);
            _ponteiroVel  = Mostrador(rt, new Vector2( 165f, 104f), 176f, "km/h", out _velNumero);

            // ---- marcha no centro ----
            var gearRt = Filho(rt, "Marcha");
            gearRt.anchorMin = gearRt.anchorMax = new Vector2(0.5f, 0.5f);
            gearRt.anchoredPosition = new Vector2(0f, 30f);
            gearRt.sizeDelta = new Vector2(110f, 110f);
            var gearBg = gearRt.gameObject.AddComponent<Image>();
            gearBg.sprite = UiTextures.Circulo(); gearBg.color = kMostrador; gearBg.raycastTarget = false;

            _marcha = Rotulo(gearRt, "1", 60, new Color(0.98f, 0.82f, 0.20f), TextAnchor.MiddleCenter);
            _marcha.fontStyle = FontStyle.Bold;

            // régua de marchas logo acima do centro
            var regua = Filho(rt, "ReguaMarchas");
            regua.anchorMin = regua.anchorMax = new Vector2(0.5f, 1f);
            regua.pivot = new Vector2(0.5f, 1f);
            regua.anchoredPosition = new Vector2(0f, -6f);
            regua.sizeDelta = new Vector2(220f, 26f);
            string[] nomes = { "R", "1", "2", "3", "4", "5" };
            for (int i = 0; i < nomes.Length; i++)
            {
                var t = Rotulo(regua, nomes[i], 19, new Color(0.42f, 0.42f, 0.46f), TextAnchor.MiddleCenter);
                var trt = (RectTransform)t.transform;
                trt.sizeDelta = new Vector2(30f, 26f);
                trt.anchoredPosition = new Vector2((i - 2.5f) * 34f, 0f);
                _reguaMarchas[i] = t;
            }

            // ---- combustível (ponteiro curto embaixo) ----
            var combRt = Filho(rt, "Combustivel");
            combRt.anchorMin = combRt.anchorMax = new Vector2(0.5f, 0f);
            combRt.anchoredPosition = new Vector2(0f, 44f);
            combRt.sizeDelta = new Vector2(150f, 12f);
            var combBg = combRt.gameObject.AddComponent<Image>();
            combBg.sprite = UiTextures.Arredondado(0.5f); combBg.type = Image.Type.Sliced;
            combBg.color = new Color(0.15f, 0.16f, 0.19f, 0.95f); combBg.raycastTarget = false;

            _ponteiroComb = Filho(combRt, "PonteiroComb");
            _ponteiroComb.anchorMin = _ponteiroComb.anchorMax = new Vector2(0.5f, 0f);
            _ponteiroComb.pivot = new Vector2(0.5f, 0f);
            _ponteiroComb.sizeDelta = new Vector2(4f, 26f);
            var combImg = _ponteiroComb.gameObject.AddComponent<Image>();
            combImg.color = new Color(0.55f, 0.85f, 0.35f); combImg.raycastTarget = false;
            Rotulo(combRt, "E                     F", 13, new Color(0.62f, 0.62f, 0.66f), TextAnchor.MiddleCenter);

            // leitura em litros logo abaixo do ponteiro
            _litros = Rotulo(combRt, "", 15, new Color(0.72f, 0.74f, 0.78f), TextAnchor.MiddleCenter);
            ((RectTransform)_litros.transform).anchoredPosition = new Vector2(0f, -20f);

            // ---- luzes de aviso ----
            _avisoReserva = Aviso(rt, new Vector2(-120f, 18f), "RESERVA");
            _avisoMotor   = Aviso(rt, new Vector2(   0f, 18f), "MOTOR");
            _avisoFreio   = Aviso(rt, new Vector2( 120f, 18f), "FREIO");

            _modelo = Rotulo(rt, "", 15, new Color(0.60f, 0.60f, 0.64f), TextAnchor.UpperCenter);
            var mrt = (RectTransform)_modelo.transform;
            mrt.anchorMin = new Vector2(0f, 1f); mrt.anchorMax = new Vector2(1f, 1f);
            mrt.pivot = new Vector2(0.5f, 0f);
            mrt.anchoredPosition = new Vector2(0f, 22f);
            mrt.sizeDelta = new Vector2(0f, 20f);

            _painel.SetActive(false);
        }

        /// <summary>Mostrador redondo: disco + aro + marcas + ponteiro com pivô no centro.</summary>
        private RectTransform Mostrador(RectTransform pai, Vector2 pos, float tam, string unidade, out Text numero)
        {
            var rt = Filho(pai, "Mostrador_" + unidade);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(tam, tam);

            var disco = rt.gameObject.AddComponent<Image>();
            disco.sprite = UiTextures.Circulo(); disco.color = kMostrador; disco.raycastTarget = false;

            var aroRt = Filho(rt, "Aro");
            aroRt.anchorMin = Vector2.zero; aroRt.anchorMax = Vector2.one;
            aroRt.offsetMin = Vector2.zero; aroRt.offsetMax = Vector2.zero;
            var aro = aroRt.gameObject.AddComponent<Image>();
            aro.sprite = UiTextures.Anel(0.06f); aro.color = kAro; aro.raycastTarget = false;

            // marcas da escala (a última é a faixa vermelha)
            for (int i = 0; i <= 10; i++)
            {
                float ang = Mathf.Lerp(kAnguloMin, kAnguloMax, i / 10f);
                var marca = Filho(rt, "Marca");
                marca.anchorMin = marca.anchorMax = new Vector2(0.5f, 0.5f);
                marca.pivot = new Vector2(0.5f, 0f);
                marca.anchoredPosition = Vector2.zero;
                marca.localRotation = Quaternion.Euler(0f, 0f, ang);
                marca.sizeDelta = new Vector2(i % 5 == 0 ? 4f : 2f, tam * 0.46f);
                var mi = marca.gameObject.AddComponent<Image>();
                mi.color = i >= 9 ? new Color(0.95f, 0.25f, 0.20f, 0.9f) : new Color(0.75f, 0.75f, 0.80f, 0.55f);
                mi.raycastTarget = false;
                // encurta a marca para virar "tique" na borda
                var filho = Filho(marca, "Vao");
                filho.anchorMin = new Vector2(0f, 0f); filho.anchorMax = new Vector2(1f, 0.78f);
                filho.offsetMin = Vector2.zero; filho.offsetMax = Vector2.zero;
                var fi = filho.gameObject.AddComponent<Image>();
                fi.color = kMostrador; fi.raycastTarget = false;
            }

            var ponteiro = Filho(rt, "Ponteiro");
            ponteiro.anchorMin = ponteiro.anchorMax = new Vector2(0.5f, 0.5f);
            ponteiro.pivot = new Vector2(0.5f, 0.08f);
            ponteiro.anchoredPosition = Vector2.zero;
            ponteiro.sizeDelta = new Vector2(5f, tam * 0.46f);
            var pImg = ponteiro.gameObject.AddComponent<Image>();
            pImg.color = kPonteiro; pImg.raycastTarget = false;

            var eixo = Filho(rt, "Eixo");
            eixo.anchorMin = eixo.anchorMax = new Vector2(0.5f, 0.5f);
            eixo.sizeDelta = new Vector2(18f, 18f);
            var eImg = eixo.gameObject.AddComponent<Image>();
            eImg.sprite = UiTextures.Circulo(); eImg.color = kAro; eImg.raycastTarget = false;

            numero = Rotulo(rt, "", 34, Color.white, TextAnchor.MiddleCenter);
            var nrt = (RectTransform)numero.transform;
            nrt.anchoredPosition = new Vector2(0f, -tam * 0.22f);

            var uni = Rotulo(rt, unidade, 14, new Color(0.62f, 0.62f, 0.66f), TextAnchor.MiddleCenter);
            ((RectTransform)uni.transform).anchoredPosition = new Vector2(0f, -tam * 0.33f);

            return ponteiro;
        }

        private Image Aviso(RectTransform pai, Vector2 pos, string texto)
        {
            var rt = Filho(pai, "Aviso_" + texto);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(112f, 24f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiTextures.Arredondado(0.45f); img.type = Image.Type.Sliced;
            img.color = kApagado; img.raycastTarget = false;

            var t = Rotulo(rt, texto, 14, new Color(0.05f, 0.05f, 0.06f), TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            return img;
        }

        // ---- helpers ----
        private static RectTransform Filho(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        private Text Rotulo(Transform pai, string txt, int tamanho, Color cor, TextAnchor alinhamento)
        {
            var rt = Filho(pai, "Txt");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(240f, tamanho + 12f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.text = txt;
            t.alignment = alinhamento; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
