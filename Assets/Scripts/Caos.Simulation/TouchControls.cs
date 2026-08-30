using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Controles virtuais mobile (docs/08 T14). Monta um Canvas sobreposto com:
    ///  • <b>Joystick flutuante</b> (metade esquerda) → <see cref="GameInput.VirtualMove"/> (andar/esterçar/acelerar/ré).
    ///  • <b>Swipe livre</b> para orbitar e botão de olhar para trás.
    ///  • Arcos de ações contextuais: correr/pular/agachar a pé; freios/buzina no carro.
    ///  • <b>Ação / Usar / R</b> → <see cref="GameInput.QueueInteract"/> / <see cref="GameInput.QueueUse"/> / <see cref="GameInput.QueueRefuel"/>.
    ///
    /// Garante um <see cref="EventSystem"/> (necessário para os eventos de pointer). Mostra sempre — útil
    /// também no Device Simulator; no PC o teclado continua funcionando em paralelo.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        private GameObject _canvasGo;
        private VirtualJoystick _joystick;
        private TouchLookSurface _lookSurface;
        private PlayerVehicleLink _link;
        private GameObject _controlesAPe;
        private GameObject _controlesCarro;
        private HoldButton[] _holdButtons;
        private bool _contextoInicializado;
        private bool _ultimoContextoAPe;

        private void Awake() => Build();

        private void Update()
        {
            if (_link == null) _link = Object.FindObjectOfType<PlayerVehicleLink>();
            AtualizarContexto();
        }

        private void OnDisable() => CancelarToques();
        private void OnApplicationFocus(bool focado)
        {
            if (!focado) CancelarToques();
        }
        private void OnApplicationPause(bool pausado)
        {
            if (pausado) CancelarToques();
        }

        private void Build()
        {
            if (_canvasGo != null) return;
            EnsureEventSystem();

            var canvasGo = new GameObject("TouchControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasGo = canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // botões também respeitam a área segura: na barra de gestos eles ficavam inalcançáveis
            var seguro = new GameObject("AreaSegura", typeof(RectTransform));
            var seguroRt = (RectTransform)seguro.transform;
            seguroRt.SetParent(canvas.transform, false);
            SafeArea.Aplicar(seguroRt, 6f);
            Transform root = seguroRt;

            // A superfície de câmera fica atrás dos demais controles no raycast: qualquer área livre
            // aceita swipe, enquanto joystick e botões continuam ganhando o toque quando sobrepostos.
            var lookZone = Child("CameraSwipeZone", root);
            Stretch(lookZone, Vector2.zero, Vector2.one);
            var lookImg = lookZone.gameObject.AddComponent<Image>();
            lookImg.color = new Color(0f, 0f, 0f, 0.001f);
            var look = lookZone.gameObject.AddComponent<TouchLookSurface>();
            _lookSurface = look;
            look.referenceHeight = 1080f;
            look.sensitivity = 0.075f;

            // ---- zona do joystick (metade esquerda) ----
            var joyZone = Child("JoystickZone", root);
            Stretch(joyZone, new Vector2(0, 0), new Vector2(0.58f, 0.68f));
            var zoneImg = joyZone.gameObject.AddComponent<Image>();
            zoneImg.color = new Color(0, 0, 0, 0.001f); // transparente mas captura pointer
            var joystick = joyZone.gameObject.AddComponent<VirtualJoystick>();
            _joystick = joystick;

            var stickBase = Child("Base", joyZone);
            stickBase.anchorMin = new Vector2(0.5f, 0.5f); stickBase.anchorMax = new Vector2(0.5f, 0.5f);
            stickBase.pivot = new Vector2(0.5f, 0.5f);
            stickBase.sizeDelta = new Vector2(250f, 250f);
            var baseImg = stickBase.gameObject.AddComponent<Image>();
            baseImg.color = new Color(0.06f, 0.08f, 0.12f, 0.28f);
            baseImg.raycastTarget = false;
            AplicarSpriteUi(baseImg, "UI/Skin/UISprite.psd");

            var thumb = Child("Thumb", stickBase);
            thumb.anchorMin = new Vector2(0.5f, 0.5f); thumb.anchorMax = new Vector2(0.5f, 0.5f);
            thumb.pivot = new Vector2(0.5f, 0.5f);
            thumb.sizeDelta = new Vector2(96f, 96f);
            var thumbImg = thumb.gameObject.AddComponent<Image>();
            thumbImg.color = new Color(1f, 1f, 1f, 0.48f);
            thumbImg.raycastTarget = false;
            AplicarSpriteUi(thumbImg, "UI/Skin/Knob.psd");

            joystick.visualRoot = stickBase;
            joystick.thumb = thumb;
            joystick.radius = 105f;
            joystick.restPosition = new Vector2(0.25f, 0.27f);

            // ---------------------------------------------------------------
            // Layout MOBILE (alvo do projeto): polegar esquerdo no joystick, polegar direito num
            // arco de botões no canto inferior direito. Nada de botão no meio da tela, e nada que
            // encoste no painel do carro (centro-baixo) nem no radar (canto inferior esquerdo).
            // ---------------------------------------------------------------

            var aPe = Child("ControlesAPe", root);
            Stretch(aPe, Vector2.zero, Vector2.one);
            _controlesAPe = aPe.gameObject;
            var carro = Child("ControlesCarro", root);
            Stretch(carro, Vector2.zero, Vector2.one);
            _controlesCarro = carro.gameObject;

            // Ação e uso servem nos dois contextos; os demais trocam junto com a câmera ao entrar.
            Tap (root, "AÇÃO",  new Vector2(0.795f, 0.16f), new Vector2(0.11f, 0.11f), new Color(0.20f, 0.70f, 0.40f, 0.55f), GameInput.QueueInteract);
            Tap (root, "USAR",  new Vector2(0.935f, 0.34f), new Vector2(0.11f, 0.11f), new Color(0.95f, 0.75f, 0.20f, 0.55f), GameInput.QueueUse);

            // A pé: os quatro comandos de locomoção ficam no mesmo arco do polegar.
            Hold(aPe, "CORRER", new Vector2(0.795f, 0.34f), new Vector2(0.10f, 0.09f), new Color(0.25f, 0.55f, 0.95f, 0.5f),
                down => GameInput.VirtualRun = down);
            Hold(aPe, "AGACHAR", new Vector2(0.675f, 0.16f), new Vector2(0.10f, 0.09f), new Color(0.45f, 0.45f, 0.55f, 0.5f),
                down => GameInput.VirtualCrouch = down);
            Tap(aPe, "PULAR",  new Vector2(0.675f, 0.34f), new Vector2(0.10f, 0.09f), new Color(0.30f, 0.65f, 0.90f, 0.5f), GameInput.QueueJump);
            Tap(aPe, "SENTAR", new Vector2(0.675f, 0.52f), new Vector2(0.10f, 0.09f), new Color(0.55f, 0.40f, 0.30f, 0.5f), GameInput.QueueSit);

            // No carro, aceleração/esterço ficam no stick; estes três completam a condução.
            Hold(carro, "FREIO", new Vector2(0.935f, 0.14f), new Vector2(0.15f, 0.14f), new Color(0.85f, 0.25f, 0.25f, 0.55f),
                down => GameInput.VirtualBrake = down);
            Hold(carro, "FREIO MÃO", new Vector2(0.795f, 0.34f), new Vector2(0.115f, 0.09f), new Color(0.45f, 0.45f, 0.55f, 0.5f),
                down => GameInput.VirtualHandbrake = down);
            Tap(carro, "BUZINA", new Vector2(0.675f, 0.52f), new Vector2(0.10f, 0.09f), new Color(0.95f, 0.55f, 0.15f, 0.5f), GameInput.QueueHorn);

            // Olhar para trás e utilidades são comuns aos dois contextos.
            Hold(root, "TRÁS",  new Vector2(0.795f, 0.52f), new Vector2(0.10f, 0.09f), new Color(0.25f, 0.50f, 0.75f, 0.5f),
                down => GameInput.VirtualLookBehind = down);
            Tap(root, "R",     new Vector2(0.955f, 0.56f), new Vector2(0.07f, 0.07f), new Color(0.70f, 0.40f, 0.90f, 0.5f), GameInput.QueueRefuel);
            Tap(root, "RÁDIO", new Vector2(0.955f, 0.67f), new Vector2(0.07f, 0.07f), new Color(0.90f, 0.35f, 0.55f, 0.5f), GameInput.QueueRadioNext);
            Tap(root, "FONE",  new Vector2(0.955f, 0.78f), new Vector2(0.07f, 0.07f), new Color(0.30f, 0.65f, 0.95f, 0.5f), GameInput.QueuePhone);
            Tap(root, "II",    new Vector2(0.955f, 0.89f), new Vector2(0.07f, 0.07f), new Color(0.25f, 0.25f, 0.30f, 0.5f), GameInput.QueuePause);

            _holdButtons = canvasGo.GetComponentsInChildren<HoldButton>(true);
            _link = Object.FindObjectOfType<PlayerVehicleLink>();
            AtualizarContexto(true);
            Debug.Log("[Touch] Layout mobile ativo: joystick flutuante + swipe de câmera + olhar para trás + ações.");
        }

        private void AtualizarContexto(bool forcar = false)
        {
            bool aPe = _link == null || _link.OnFoot;
            if (!forcar && _contextoInicializado && aPe == _ultimoContextoAPe) return;

            // Solta holds do contexto anterior antes de esconder seus botões.
            GameInput.VirtualRun = false;
            GameInput.VirtualBrake = false;
            GameInput.VirtualCrouch = false;
            GameInput.VirtualHandbrake = false;
            if (!aPe) GameInput.CancelarPuloVirtual();
            if (_joystick != null) _joystick.Cancelar();

            if (_controlesAPe != null) _controlesAPe.SetActive(aPe);
            if (_controlesCarro != null) _controlesCarro.SetActive(!aPe);
            _ultimoContextoAPe = aPe;
            _contextoInicializado = true;
        }

        private void CancelarToques()
        {
            if (_joystick != null) _joystick.Cancelar();
            if (_lookSurface != null) _lookSurface.Cancelar();
            if (_holdButtons != null)
            {
                for (int i = 0; i < _holdButtons.Length; i++)
                    if (_holdButtons[i] != null) _holdButtons[i].Cancelar();
            }
            GameInput.ResetVirtualControls();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ---- helpers ----
        private static RectTransform Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        private static RectTransform Box(Transform parent, string name, Vector2 center, Vector2 size, Color c, out Image img)
        {
            var rt = Child(name, parent);
            rt.anchorMin = center; rt.anchorMax = center;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size * 1080f; // size em fração da altura de referência
            var i = rt.gameObject.AddComponent<Image>();
            i.color = c;
            img = i;
            return rt;
        }
        private static void AplicarSpriteUi(Image image, string caminho)
        {
            var sprite = Resources.GetBuiltinResource<Sprite>(caminho);
            if (sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        private static void Label(Transform parent, string text, int size, Color c)
        {
            var rt = Child("Lbl", parent);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(400, size + 8);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size; t.color = c; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false; t.text = text;
        }
        private static void Hold(Transform parent, string name, Vector2 center, Vector2 size, Color c, System.Action<bool> onPress)
        {
            var rt = Box(parent, name, center, size, c, out _);
            var btn = rt.gameObject.AddComponent<HoldButton>();
            btn.pressed = onPress;
            Label(rt, name, 26, Color.white);
        }
        private static void Tap(Transform parent, string name, Vector2 center, Vector2 size, Color c, System.Action onTap)
        {
            var rt = Box(parent, name, center, size, c, out _);
            var btn = rt.gameObject.AddComponent<TapButton>();
            btn.tapped = onTap;
            Label(rt, name, 30, Color.white);
        }
    }

    /// <summary>
    /// Joystick flutuante: nasce sob o polegar, aplica zona morta radial e volta ao ponto de repouso
    /// ao soltar. Rastrear o pointer evita que um segundo dedo roube a direção no meio de uma curva.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IInitializePotentialDragHandler, IPointerDownHandler,
                                   IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        public RectTransform visualRoot;
        public RectTransform thumb;
        public float radius = 110f;
        public Vector2 restPosition = new Vector2(0.25f, 0.27f);

        private RectTransform _zone;
        private Vector2 _touchOrigin;
        private int _pointerId = int.MinValue;

        private void Awake() => _zone = (RectTransform)transform;

        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            VoltarAoRepouso();
        }

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != int.MinValue ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, e.position, e.pressEventCamera, out Vector2 local))
                return;

            _pointerId = e.pointerId;
            _touchOrigin = local;
            PosicionarBase(LimitarCentro(local));
            Aplicar(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == _pointerId) Aplicar(e);
        }

        public void OnEndDrag(PointerEventData e) => Soltar(e.pointerId);
        public void OnPointerUp(PointerEventData e) => Soltar(e.pointerId);

        private void Aplicar(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, e.position, e.pressEventCamera, out Vector2 local))
                return;

            Vector2 deslocamento = Vector2.ClampMagnitude(local - _touchOrigin, radius);
            if (thumb != null) thumb.anchoredPosition = deslocamento;
            GameInput.VirtualMove = radius > Mathf.Epsilon ? deslocamento / radius : Vector2.zero;
        }

        private void Soltar(int pointerId)
        {
            if (pointerId != _pointerId) return;
            Cancelar();
        }

        public void Cancelar()
        {
            _pointerId = int.MinValue;
            GameInput.VirtualMove = Vector2.zero;
            if (thumb != null) thumb.anchoredPosition = Vector2.zero;
            if (_zone != null) VoltarAoRepouso();
        }

        private void OnDisable() => Cancelar();

        private void OnRectTransformDimensionsChange()
        {
            // Rotação/split-screen altera a área segura e, por consequência, esta zona.
            if (_zone != null && _pointerId == int.MinValue) VoltarAoRepouso();
        }

        private Vector2 LimitarCentro(Vector2 ponto)
        {
            Rect r = _zone.rect;
            float margemX = visualRoot != null ? Mathf.Max(radius, visualRoot.rect.width * 0.5f) : radius;
            float margemY = visualRoot != null ? Mathf.Max(radius, visualRoot.rect.height * 0.5f) : radius;
            float x = r.width  > margemX * 2f ? Mathf.Clamp(ponto.x, r.xMin + margemX, r.xMax - margemX) : r.center.x;
            float y = r.height > margemY * 2f ? Mathf.Clamp(ponto.y, r.yMin + margemY, r.yMax - margemY) : r.center.y;
            return new Vector2(x, y);
        }

        private void VoltarAoRepouso()
        {
            Rect r = _zone.rect;
            Vector2 repouso = LimitarCentro(new Vector2(
                Mathf.Lerp(r.xMin, r.xMax, Mathf.Clamp01(restPosition.x)),
                Mathf.Lerp(r.yMin, r.yMax, Mathf.Clamp01(restPosition.y))));
            PosicionarBase(repouso);
        }

        private void PosicionarBase(Vector2 ponto)
        {
            if (visualRoot != null) visualRoot.anchoredPosition = ponto;
        }
    }

    /// <summary>Swipe livre e independente do joystick, normalizado para a resolução de referência.</summary>
    public class TouchLookSurface : MonoBehaviour, IInitializePotentialDragHandler, IPointerDownHandler,
                                    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        public float referenceHeight = 1080f;
        public float sensitivity = 0.075f;
        public float maxDelta = 120f;

        private int _pointerId = int.MinValue;
        private bool _dragging;

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = true;

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId == int.MinValue) _pointerId = e.pointerId;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            _dragging = true;
            GameInput.LimparOrbitaVirtual();
            GameInput.VirtualOrbitActive = true;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging || e.pointerId != _pointerId) return;

            float escala = Screen.height > 0 ? referenceHeight / Screen.height : 1f;
            Vector2 delta = Vector2.ClampMagnitude(e.delta * escala, maxDelta);
            GameInput.AcumularOrbitaVirtual(delta * sensitivity);
        }

        public void OnEndDrag(PointerEventData e) => Soltar(e.pointerId);
        public void OnPointerUp(PointerEventData e) => Soltar(e.pointerId);

        private void Soltar(int pointerId)
        {
            if (pointerId != _pointerId) return;
            Cancelar();
        }

        public void Cancelar()
        {
            _pointerId = int.MinValue;
            _dragging = false;
            GameInput.LimparOrbitaVirtual();
            GameInput.VirtualOrbitActive = false;
        }

        private void OnDisable() => Cancelar();
    }

    /// <summary>Botão de segurar (pressionar/soltar).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action<bool> pressed;
        private readonly HashSet<int> _pointerIds = new HashSet<int>();

        public void OnPointerDown(PointerEventData e)
        {
            if (!_pointerIds.Add(e.pointerId)) return;
            if (_pointerIds.Count == 1) pressed?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_pointerIds.Remove(e.pointerId)) return;
            if (_pointerIds.Count == 0) pressed?.Invoke(false);
        }

        private void OnDisable() => Cancelar();

        public void Cancelar()
        {
            if (_pointerIds.Count == 0) return;
            _pointerIds.Clear();
            pressed?.Invoke(false);
        }
    }

    /// <summary>Botão de toque único (dispara na pressão).</summary>
    public class TapButton : MonoBehaviour, IPointerDownHandler
    {
        public System.Action tapped;
        public void OnPointerDown(PointerEventData e) { tapped?.Invoke(); }
    }
}
