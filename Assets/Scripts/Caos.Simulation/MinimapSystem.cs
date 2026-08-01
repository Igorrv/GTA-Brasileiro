using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Radar estilo GTA: uma câmera ortográfica olhando a cidade de cima renderiza numa RenderTexture
    /// exibida no canto, com <b>blips</b> desenhados por cima em espaço de tela (comércio, missão,
    /// polícia e o jogador). Tecla <b>M</b> abre o mapa grande.
    ///
    /// Custo controlado: a câmera do radar fica desligada e é renderizada à mão a 12 Hz
    /// (<see cref="Camera.Render"/>) — não paga um render extra por frame (docs/12 §12.7).
    /// </summary>
    public class MinimapSystem : MonoBehaviour
    {
        private const int   kResolucao   = 320;
        private const float kAlturaCam   = 120f;
        private const float kZoomRadar   = 78f;
        private const float kZoomMapa    = 300f;
        private const float kPainelRadar = 300f;
        private const float kHz          = 12f;

        private Transform      _player;
        private Camera         _cam;
        private RenderTexture  _rt;
        private RectTransform  _painel, _recorte;
        private RawImage       _imagem;
        private Image          _borda;
        private Mask           _mascara;
        private Text           _norte, _legenda;
        private Font           _font;
        private bool           _mapaGrande;
        private float          _accum;

        private readonly List<Image> _blips = new List<Image>();
        private readonly List<Text>  _rotulosBairro = new List<Text>();
        private readonly List<Image> _trechos = new List<Image>();
        private readonly List<Vector3> _rota = new List<Vector3>();
        private float _proximoRecalculo;
        private int   _trechosUsados;

        /// <summary>Distância pelo trajeto até o destino (m) — o painel de missão mostra.</summary>
        public float DistanciaDaRota { get; private set; }

        public void Init(Transform player)
        {
            _player = player;
            MontarCamera();
            MontarUi();
        }

        private void MontarCamera()
        {
            _rt = new RenderTexture(kResolucao, kResolucao, 16) { name = "RadarRT" };

            var go = new GameObject("CameraRadar");
            _cam = go.AddComponent<Camera>();
            _cam.orthographic     = true;
            _cam.orthographicSize = kZoomRadar;
            _cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _cam.clearFlags       = CameraClearFlags.SolidColor;
            _cam.backgroundColor  = new Color(0.10f, 0.12f, 0.14f);
            _cam.nearClipPlane    = 1f;
            _cam.farClipPlane     = kAlturaCam + 60f;
            _cam.targetTexture    = _rt;
            _cam.allowHDR = false; _cam.allowMSAA = false;
            _cam.enabled = false;                       // renderizado à mão, a 12 Hz
        }

        private void MontarUi()
        {
            var canvasGo = new GameObject("RadarUI", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var moldura = new GameObject("Moldura", typeof(RectTransform)).GetComponent<RectTransform>();
            moldura.SetParent(canvasGo.transform, false);
            moldura.anchorMin = Vector2.zero; moldura.anchorMax = Vector2.zero;
            moldura.pivot = Vector2.zero;
            moldura.anchoredPosition = new Vector2(26f, 26f);
            moldura.sizeDelta = new Vector2(kPainelRadar, kPainelRadar);
            _borda = moldura.gameObject.AddComponent<Image>();
            _borda.sprite = UiTextures.Circulo();          // aro do radar: redondo
            _borda.color = new Color(0f, 0f, 0f, 0.80f);
            _borda.raycastTarget = false;

            // recorte circular: a Image do círculo não é desenhada (showMaskGraphic = false),
            // ela só define o formato — é o que transforma o retângulo do RenderTexture em radar.
            _recorte = new GameObject("Recorte", typeof(RectTransform)).GetComponent<RectTransform>();
            _recorte.SetParent(moldura, false);
            _recorte.anchorMin = Vector2.zero; _recorte.anchorMax = Vector2.one;
            _recorte.offsetMin = new Vector2(5f, 5f); _recorte.offsetMax = new Vector2(-5f, -5f);
            var mascara = _recorte.gameObject.AddComponent<Image>();
            mascara.sprite = UiTextures.Circulo();
            mascara.raycastTarget = false;
            _mascara = _recorte.gameObject.AddComponent<Mask>();
            _mascara.showMaskGraphic = false;

            _painel = new GameObject("Radar", typeof(RectTransform)).GetComponent<RectTransform>();
            _painel.SetParent(_recorte, false);
            _painel.anchorMin = Vector2.zero; _painel.anchorMax = Vector2.one;
            _painel.offsetMin = Vector2.zero; _painel.offsetMax = Vector2.zero;
            _imagem = _painel.gameObject.AddComponent<RawImage>();
            _imagem.texture = _rt;
            _imagem.raycastTarget = false;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var nGo = new GameObject("Norte", typeof(RectTransform));
            var nRt = (RectTransform)nGo.transform;
            nRt.SetParent(moldura, false);
            nRt.anchorMin = new Vector2(0.5f, 1f); nRt.anchorMax = new Vector2(0.5f, 1f);
            nRt.anchoredPosition = new Vector2(0f, -12f);
            nRt.sizeDelta = new Vector2(40f, 26f);
            _norte = nGo.AddComponent<Text>();
            _norte.text = "N"; _norte.font = _font; _norte.fontSize = 20; _norte.fontStyle = FontStyle.Bold;
            _norte.color = new Color(1f, 1f, 1f, 0.85f); _norte.alignment = TextAnchor.MiddleCenter;
            _norte.raycastTarget = false;

            // legenda: só aparece no mapa grande
            var lGo = new GameObject("Legenda", typeof(RectTransform));
            var lRt = (RectTransform)lGo.transform;
            lRt.SetParent(moldura, false);
            lRt.anchorMin = new Vector2(0f, 0f); lRt.anchorMax = new Vector2(1f, 0f);
            lRt.pivot = new Vector2(0.5f, 1f);
            lRt.anchoredPosition = new Vector2(0f, -8f);
            lRt.sizeDelta = new Vector2(0f, 28f);
            _legenda = lGo.AddComponent<Text>();
            _legenda.font = _font; _legenda.fontSize = 18;
            _legenda.text = "branco: você   ·   dourado: destino   ·   azul: polícia   ·   colorido: comércio   ·   M fecha";
            _legenda.color = new Color(0.85f, 0.85f, 0.88f, 0.9f);
            _legenda.alignment = TextAnchor.MiddleCenter; _legenda.raycastTarget = false;
            _legenda.gameObject.SetActive(false);
        }

        /// <summary>Rótulo de bairro desenhado sobre o mapa grande.</summary>
        private Text RotuloBairro(int indice)
        {
            while (_rotulosBairro.Count <= indice)
            {
                var go = new GameObject("Bairro", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_painel, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(260f, 24f);
                var t = go.AddComponent<Text>();
                t.font = _font; t.fontSize = 17; t.fontStyle = FontStyle.Bold;
                t.color = new Color(1f, 1f, 1f, 0.92f);
                t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
                _rotulosBairro.Add(t);
            }
            return _rotulosBairro[indice];
        }

        private void Update()
        {
            if (_player == null || _cam == null) return;

            if (GameInput.MapToggle) AlternarMapa();

            Vector3 p = _player.position;
            _cam.transform.position = new Vector3(p.x, kAlturaCam, p.z);

            _accum += Time.deltaTime;
            if (_accum >= 1f / kHz)
            {
                _accum = 0f;
                _cam.Render();
                AtualizarBlips();
            }
        }

        private void AlternarMapa()
        {
            _mapaGrande = !_mapaGrande;
            _cam.orthographicSize = _mapaGrande ? kZoomMapa : kZoomRadar;

            var moldura = (RectTransform)_recorte.parent;
            if (_mapaGrande)
            {
                moldura.anchorMin = new Vector2(0.5f, 0.5f);
                moldura.anchorMax = new Vector2(0.5f, 0.5f);
                moldura.pivot     = new Vector2(0.5f, 0.5f);
                moldura.anchoredPosition = Vector2.zero;
                moldura.sizeDelta = new Vector2(900f, 900f);

                // mapa grande é uma carta retangular de cantos arredondados, não um radar
                _borda.sprite = UiTextures.Arredondado(0.03f);
                _borda.type   = Image.Type.Sliced;
                _borda.color  = new Color(0f, 0f, 0f, 0.92f);
                TrocarMascara(UiTextures.Arredondado(0.03f), Image.Type.Sliced);
            }
            else
            {
                moldura.anchorMin = Vector2.zero;
                moldura.anchorMax = Vector2.zero;
                moldura.pivot     = Vector2.zero;
                moldura.anchoredPosition = new Vector2(26f, 26f);
                moldura.sizeDelta = new Vector2(kPainelRadar, kPainelRadar);

                _borda.sprite = UiTextures.Circulo();
                _borda.type   = Image.Type.Simple;
                _borda.color  = new Color(0f, 0f, 0f, 0.80f);
                TrocarMascara(UiTextures.Circulo(), Image.Type.Simple);
            }

            _legenda.gameObject.SetActive(_mapaGrande);
            foreach (var t in _rotulosBairro) t.gameObject.SetActive(_mapaGrande);
        }

        private void TrocarMascara(Sprite sprite, Image.Type tipo)
        {
            var img = _recorte.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.type   = tipo;
        }

        // ------------------------------------------------------------------ blips
        private int _usados;

        private void AtualizarBlips()
        {
            _usados = 0;
            var gen = CityRuntime.Generator;
            float zoom = _cam.orthographicSize;
            Vector3 centro = _cam.transform.position;

            DesenharRota(centro, zoom);

            // comércio (só o que está no alcance)
            if (gen != null)
            {
                for (int i = 0; i < gen.Shops.Count; i++)
                {
                    var s = gen.Shops[i];
                    if (s == null) continue;
                    Vector3 d = s.transform.position - centro;
                    if (Mathf.Abs(d.x) > zoom || Mathf.Abs(d.z) > zoom) continue;
                    Blip(d, zoom, s.cor, 9f);
                }
            }

            // polícia
            foreach (var pc in FindObjectsOfType<PoliceCar>())
            {
                Vector3 d = pc.transform.position - centro;
                if (Mathf.Abs(d.x) > zoom || Mathf.Abs(d.z) > zoom) continue;
                Blip(d, zoom, new Color(0.25f, 0.5f, 1f), 12f);
            }

            // destino da missão
            var tracker = FindObjectOfType<MissionTracker>();
            if (tracker != null && tracker.TemDestino)
            {
                Vector3 d = tracker.Destino - centro;
                float mx = Mathf.Clamp(d.x, -zoom, zoom);
                float mz = Mathf.Clamp(d.z, -zoom, zoom);
                Blip(new Vector3(mx, 0f, mz), zoom, new Color(1f, 0.85f, 0.2f), 16f);
            }

            // jogador por último (fica por cima), como losango apontando pra frente
            var eu = Blip(Vector3.zero, zoom, Color.white, 16f, redondo: false);
            if (eu != null && _player != null)
                eu.transform.localRotation = Quaternion.Euler(0f, 0f, -_player.eulerAngles.y + 45f);

            for (int i = _usados; i < _blips.Count; i++)
                if (_blips[i] != null) _blips[i].enabled = false;

            // nomes de bairro (só no mapa grande — no radar não caberia)
            if (_mapaGrande && CityRuntime.Layout != null)
            {
                var bairros = CityRuntime.Layout.Districts;
                float meio = Mathf.Min(_painel.rect.width, _painel.rect.height) * 0.5f;
                for (int i = 0; i < bairros.Count; i++)
                {
                    var t = RotuloBairro(i);
                    var b = bairros[i];
                    Vector3 d = new Vector3(b.centroX, 0f, b.centroZ) - centro;
                    bool dentro = Mathf.Abs(d.x) <= zoom && Mathf.Abs(d.z) <= zoom;
                    t.gameObject.SetActive(dentro);
                    if (!dentro) continue;
                    t.text = b.nome;
                    ((RectTransform)t.transform).anchoredPosition =
                        new Vector2(d.x / zoom * meio, d.z / zoom * meio);
                }
                for (int i = bairros.Count; i < _rotulosBairro.Count; i++)
                    _rotulosBairro[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Desenha a <b>linha do trajeto</b> — o caminho que o GPS manda seguir até a missão.
        /// Cada trecho vira um retângulo fino girado entre dois pontos da rota; o recálculo acontece a
        /// cada meio segundo (ou quando o destino muda), não a cada quadro.
        /// </summary>
        private void DesenharRota(Vector3 centro, float zoom)
        {
            _trechosUsados = 0;

            var tracker = FindObjectOfType<MissionTracker>();
            var layout  = CityRuntime.Layout;
            bool temRota = tracker != null && tracker.TemDestino && layout != null && _player != null;

            if (temRota && Time.time >= _proximoRecalculo)
            {
                _proximoRecalculo = Time.time + 0.5f;
                _rota.Clear();
                _rota.AddRange(layout.CalcularRota(_player.position, tracker.Destino));
                DistanciaDaRota = CityLayout.ComprimentoDaRota(_rota);
            }
            if (!temRota) { _rota.Clear(); DistanciaDaRota = 0f; }

            float meio = Mathf.Min(_painel.rect.width, _painel.rect.height) * 0.5f;
            for (int i = 1; i < _rota.Count; i++)
            {
                Vector2 a = Projetar(_rota[i - 1], centro, zoom, meio);
                Vector2 b = Projetar(_rota[i],     centro, zoom, meio);
                Vector2 d = b - a;
                if (d.sqrMagnitude < 1f) continue;

                var img = Trecho(_trechosUsados++);
                var rt  = (RectTransform)img.transform;
                rt.anchoredPosition = (a + b) * 0.5f;
                rt.sizeDelta = new Vector2(d.magnitude, _mapaGrande ? 7f : 5f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                img.enabled = true;
            }

            for (int i = _trechosUsados; i < _trechos.Count; i++)
                if (_trechos[i] != null) _trechos[i].enabled = false;
        }

        private static Vector2 Projetar(Vector3 mundo, Vector3 centro, float zoom, float meio)
        {
            Vector3 d = mundo - centro;
            return new Vector2(Mathf.Clamp(d.x / zoom, -1f, 1f) * meio,
                               Mathf.Clamp(d.z / zoom, -1f, 1f) * meio);
        }

        private Image Trecho(int indice)
        {
            while (_trechos.Count <= indice)
            {
                var go = new GameObject("Trecho", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_painel, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = UiTextures.Arredondado(0.5f);
                img.type = Image.Type.Sliced;
                img.color = new Color(0.20f, 0.80f, 1f, 0.92f);   // azul de GPS
                img.raycastTarget = false;
                // fica atrás dos blips
                go.transform.SetAsFirstSibling();
                _trechos.Add(img);
            }
            return _trechos[indice];
        }

        /// <summary>Desenha um blip. Redondo por padrão; o do jogador vira losango ao ser girado.</summary>
        private Image Blip(Vector3 offsetMundo, float zoom, Color cor, float tamanho, bool redondo = true)
        {
            Image img;
            if (_usados < _blips.Count) img = _blips[_usados];
            else
            {
                var go = new GameObject("Blip", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_painel, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                img = go.AddComponent<Image>();
                img.raycastTarget = false;
                _blips.Add(img);
            }
            _usados++;

            var rect = _painel.rect;
            float meio = Mathf.Min(rect.width, rect.height) * 0.5f;
            var rtb = (RectTransform)img.transform;
            rtb.anchoredPosition = new Vector2(offsetMundo.x / zoom * meio, offsetMundo.z / zoom * meio);
            rtb.sizeDelta = new Vector2(tamanho, tamanho);
            rtb.localRotation = Quaternion.identity;
            img.sprite = redondo ? UiTextures.Circulo() : null;
            img.color = cor;
            img.enabled = true;
            return img;
        }

        private void OnDestroy()
        {
            if (_cam != null) _cam.targetTexture = null;
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
        }
    }
}
