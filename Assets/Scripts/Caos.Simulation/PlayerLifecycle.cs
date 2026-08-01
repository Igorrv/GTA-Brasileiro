using Caos.Core;
using Caos.Gameplay;
using Caos.World;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Fluxo de fim-de-rodada (docs/10.8 — Busted/Wasted):
    ///  • <b>WASTED</b> — quando <see cref="PlayerAttributes"/> publica <see cref="PlayerMorreu"/> (Saúde ≤ 0 por
    ///    fome/ferimentos; bater até inutilizar o motor também fere o jogador via <see cref="VehicleHealth"/>).
    ///  • <b>BUSTED</b> — se estiver procurado e a polícia mantiver contato próximo por alguns segundos.
    ///
    /// Em ambos: tela cheia com o veredito (o cartão preto do gênero), teleporte para o ponto de
    /// partida, atributos restaurados, multa em R$ e procurado zerado.
    /// </summary>
    public class PlayerLifecycle : MonoBehaviour
    {
        private const float kBustRadius = 5f;
        private const float kBustTime   = 2.5f;
        private const float kFine       = 150f;
        private const float kCartao     = 2.6f;   // duração do cartão na tela

        private Transform        _player;
        private Transform        _vehicle;
        private PoliceSystem     _police;
        private Vector3          _spawn;

        private PlayerAttributes _attrs;
        private EconomyService   _econ;
        private WorldStateService _world;

        private float _bustAccum;
        private bool  _busy;

        // cartão de fim de rodada
        private CanvasGroup _grupo;
        private Image       _fundo;
        private Text        _titulo, _subtitulo;
        private float       _mostrarAte;

        public void Init(Transform player, Transform vehicle, PoliceSystem police)
        {
            _player  = player;
            _vehicle = vehicle;
            _police  = police;
            _spawn   = player != null ? player.position : Vector3.zero;
            ServiceLocator.TryGet(out _attrs);
            ServiceLocator.TryGet(out _econ);
            ServiceLocator.TryGet(out _world);
        }

        private void Awake() => MontarCartao();

        private void OnEnable()  => EventBus<PlayerMorreu>.Subscribe(OnMorreu);
        private void OnDisable() => EventBus<PlayerMorreu>.Unsubscribe(OnMorreu);

        private void OnMorreu(PlayerMorreu e)
            => Respawn("WASTED", "Você não aguentou. Acordou no posto de saúde.", new Color(0.55f, 0.05f, 0.05f), fine: 0f, clearStars: false);

        private void Update()
        {
            if (_grupo != null)
            {
                float restante = _mostrarAte - Time.unscaledTime;
                _grupo.alpha = restante <= 0f ? 0f : Mathf.Clamp01(Mathf.Min(restante, 0.6f) / 0.6f);
            }

            if (_busy || _world == null || _police == null) return;

            if (_world.Stars > 0)
            {
                float d = _police.NearestDistanceTo(_player);
                if (d <= kBustRadius)
                {
                    _bustAccum += Time.deltaTime;
                    if (_bustAccum >= kBustTime)
                        Respawn("BUSTED", $"A PM te levou. Multa de R$ {kFine:F0} e o carro no pátio.", new Color(0.05f, 0.12f, 0.35f), kFine, true);
                }
                else _bustAccum = 0f;
            }
            else _bustAccum = 0f;
        }

        private void Respawn(string titulo, string subtitulo, Color cor, float fine, bool clearStars)
        {
            _busy = true;
            _bustAccum = 0f;

            if (_econ != null && fine > 0f) _econ.TrySpend(fine);
            if (clearStars && _world != null) _world.SetStars(0);
            if (_attrs != null) _attrs.Hydrate(70f, 70f, 70f, 60f, 100f);   // fome, sede, energia, sanidade, saúde

            if (_vehicle != null)
            {
                _vehicle.position = _spawn + new Vector3(6f, 0.7f, 0f);
                _vehicle.rotation = Quaternion.identity;
                var rb = _vehicle.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }
            if (_player != null) _player.position = _spawn + Vector3.up * 1f;

            MostrarCartao(titulo, subtitulo, cor);
            Debug.Log($"[Vida] {titulo} — {subtitulo}");
            _busy = false;
        }

        // ------------------------------------------------------------------ cartão
        private void MontarCartao()
        {
            var canvasGo = new GameObject("CartaoFimDeRodada", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _grupo = canvasGo.AddComponent<CanvasGroup>();
            _grupo.alpha = 0f;
            _grupo.interactable = false;
            _grupo.blocksRaycasts = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var bg = new GameObject("Fundo", typeof(RectTransform));
            var bgRt = (RectTransform)bg.transform;
            bgRt.SetParent(canvasGo.transform, false);
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            _fundo = bg.AddComponent<Image>();
            _fundo.color = new Color(0f, 0f, 0f, 0.72f);
            _fundo.raycastTarget = false;

            _titulo = Linha(canvasGo.transform, font, 130, FontStyle.Bold, 30f);
            _subtitulo = Linha(canvasGo.transform, font, 32, FontStyle.Normal, -80f);
        }

        private static Text Linha(Transform parent, Font font, int size, FontStyle style, float y)
        {
            var go = new GameObject("Linha", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, size + 20f);
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private void MostrarCartao(string titulo, string subtitulo, Color cor)
        {
            if (_grupo == null) return;
            _titulo.text = titulo;
            _titulo.color = Color.Lerp(cor, Color.white, 0.45f);
            _subtitulo.text = subtitulo;
            _fundo.color = new Color(cor.r * 0.35f, cor.g * 0.35f, cor.b * 0.35f, 0.80f);
            _mostrarAte = Time.unscaledTime + kCartao;
            _grupo.alpha = 1f;
        }
    }
}
