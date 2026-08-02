using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Controles virtuais mobile (docs/08 T14). Monta um Canvas sobreposto com:
    ///  • <b>Joystick flutuante</b> (metade esquerda) → <see cref="GameInput.VirtualMove"/> (andar/esterçar/acelerar/ré).
    ///  • <b>Freio</b> e <b>Correr</b> (hold) → <see cref="GameInput.VirtualBrake"/> / <see cref="GameInput.VirtualRun"/>.
    ///  • <b>E / F / R</b> (tap) → <see cref="GameInput.QueueInteract"/> / <see cref="GameInput.QueueUse"/> / <see cref="GameInput.QueueRefuel"/>.
    ///
    /// Garante um <see cref="EventSystem"/> (necessário para os eventos de pointer). Mostra sempre — útil
    /// também no Device Simulator; no PC o teclado continua funcionando em paralelo.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        private void Awake() => Build();

        private void Build()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("TouchControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            // ---- zona do joystick (metade esquerda) ----
            var joyZone = Child("JoystickZone", root);
            Stretch(joyZone, new Vector2(0, 0), new Vector2(0.55f, 0.65f));
            var zoneImg = joyZone.gameObject.AddComponent<Image>();
            zoneImg.color = new Color(0, 0, 0, 0.001f); // transparente mas captura pointer
            var joystick = joyZone.gameObject.AddComponent<VirtualJoystick>();
            joystick.radius = 110f;

            var thumb = Child("Thumb", joyZone);
            thumb.anchorMin = new Vector2(0.5f, 0.5f); thumb.anchorMax = new Vector2(0.5f, 0.5f);
            thumb.pivot = new Vector2(0.5f, 0.5f);
            thumb.sizeDelta = new Vector2(110f, 110f);
            var thumbImg = thumb.gameObject.AddComponent<Image>();
            thumbImg.color = new Color(1f, 1f, 1f, 0.35f);
            thumbImg.raycastTarget = false;
            joystick.thumb = thumb;

            // ---------------------------------------------------------------
            // Layout MOBILE (alvo do projeto): polegar esquerdo no joystick, polegar direito num
            // arco de botões no canto inferior direito. Nada de botão no meio da tela, e nada que
            // encoste no painel do carro (centro-baixo) nem no radar (canto inferior esquerdo).
            // ---------------------------------------------------------------

            // ação principal e freio: o par que o polegar direito mais usa, os dois maiores
            Hold(root, "FREIO", new Vector2(0.935f, 0.14f), new Vector2(0.15f, 0.14f), new Color(0.85f, 0.25f, 0.25f, 0.55f),
                down => GameInput.VirtualBrake = down);
            Tap (root, "E",     new Vector2(0.795f, 0.16f), new Vector2(0.11f, 0.11f), new Color(0.20f, 0.70f, 0.40f, 0.55f), GameInput.QueueInteract);
            Tap (root, "F",     new Vector2(0.935f, 0.34f), new Vector2(0.11f, 0.11f), new Color(0.95f, 0.75f, 0.20f, 0.55f), GameInput.QueueUse);

            // segunda fileira: correr, agachar, sentar
            Hold(root, "CORRER", new Vector2(0.795f, 0.34f), new Vector2(0.10f, 0.09f), new Color(0.25f, 0.55f, 0.95f, 0.5f),
                down => GameInput.VirtualRun = down);
            Hold(root, "AGACHAR", new Vector2(0.675f, 0.16f), new Vector2(0.10f, 0.09f), new Color(0.45f, 0.45f, 0.55f, 0.5f),
                down => { GameInput.VirtualCrouch = down; GameInput.VirtualHandbrake = down; });
            Tap (root, "SENTAR",  new Vector2(0.675f, 0.34f), new Vector2(0.10f, 0.09f), new Color(0.55f, 0.40f, 0.30f, 0.5f), GameInput.QueueSit);

            // utilidades: coluna discreta na borda direita, longe do polegar de ação
            Tap(root, "BUZINA", new Vector2(0.675f, 0.52f), new Vector2(0.10f, 0.09f), new Color(0.95f, 0.55f, 0.15f, 0.5f), GameInput.QueueHorn);
            Tap(root, "R",     new Vector2(0.955f, 0.56f), new Vector2(0.07f, 0.07f), new Color(0.70f, 0.40f, 0.90f, 0.5f), GameInput.QueueRefuel);
            Tap(root, "RÁDIO", new Vector2(0.955f, 0.67f), new Vector2(0.07f, 0.07f), new Color(0.90f, 0.35f, 0.55f, 0.5f), GameInput.QueueRadioNext);
            Tap(root, "FONE",  new Vector2(0.955f, 0.78f), new Vector2(0.07f, 0.07f), new Color(0.30f, 0.65f, 0.95f, 0.5f), GameInput.QueuePhone);
            Tap(root, "II",    new Vector2(0.955f, 0.89f), new Vector2(0.07f, 0.07f), new Color(0.25f, 0.25f, 0.30f, 0.5f), GameInput.QueuePause);

            Debug.Log("[Touch] Layout mobile ativo: joystick + freio/E/F + correr/agachar/sentar + R/rádio/fone/pausa.");
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

    /// <summary>Joystick flutuante: o "polegar" segue o dedo dentro de um raio; soltar zera o eixo.</summary>
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public RectTransform thumb;
        public float radius = 110f;
        private RectTransform _base;

        private void Awake() => _base = (RectTransform)transform;

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_base, e.position, e.pressEventCamera, out Vector2 local)) return;
            Vector2 c = Vector2.ClampMagnitude(local, radius);
            if (thumb != null) thumb.localPosition = c;
            GameInput.VirtualMove = c / radius;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (thumb != null) thumb.localPosition = Vector3.zero;
            GameInput.VirtualMove = Vector2.zero;
        }
    }

    /// <summary>Botão de segurar (pressionar/soltar).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action<bool> pressed;
        public void OnPointerDown(PointerEventData e) { pressed?.Invoke(true); }
        public void OnPointerUp(PointerEventData e)   { pressed?.Invoke(false); }
    }

    /// <summary>Botão de toque único (dispara na pressão).</summary>
    public class TapButton : MonoBehaviour, IPointerDownHandler
    {
        public System.Action tapped;
        public void OnPointerDown(PointerEventData e) { tapped?.Invoke(); }
    }
}
