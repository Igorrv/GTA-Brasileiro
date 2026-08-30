using System.Collections.Generic;
using Caos.Core;
using Caos.World;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Aplica as preferências de <see cref="AccessibilitySettings"/> na UI que <b>já existe</b>,
    /// sem reescrever os donos dela (HudController, TouchControls, SettingsMenu). É um visitante:
    /// encontra os canvases pelo nome, escala o tamanho da fonte de cada <see cref="Text"/> e
    /// re-colore as estrelas de procurado com a paleta segura para daltonismo.
    ///
    /// Texto: escala o <c>fontSize</c> de todo <see cref="Text"/> vivo. Cada UI do jogo monta sua
    /// fonte uma única vez no boot (BuildUi/Build/Montar), então escalar depois é estável — nenhum
    /// sistema reescreve fontSize por frame. Guardamos o tamanho-base de cada Text na primeira
    /// passada e recalculamos quando a escala muda.
    ///
    /// Estrelas: o HudController só re-colore as estrelas quando o nível de procurado muda
    /// (<see cref="EstrelasMudou"/>), não a cada quadro. Então nos inscrevemos no mesmo evento e
    /// re-aplicamos a paleta segura logo depois — fica sempre correto, sem brigar com o dono.
    /// </summary>
    public class AccessibilityApplier : MonoBehaviour
    {
        private readonly Dictionary<Text, int> _baseSizes = new Dictionary<Text, int>();
        private float _lastScale = -1f;
        private ColorblindMode _lastMode = (ColorblindMode)(-1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<AccessibilityApplier>() != null) return;
            var go = new GameObject("[A11yApplier]");
            go.AddComponent<AccessibilityApplier>();
        }

        private void Awake()
        {
            AccessibilitySettings.Carregar();
            AccessibilitySettings.Mudou += OnMudou;
        }

        private void OnDestroy() => AccessibilitySettings.Mudou -= OnMudou;

        private void Start() => OnMudou();

        private void OnEnable()
        {
            EventBus<EstrelasMudou>.Subscribe(OnStars);
        }

        private void OnDisable()
        {
            EventBus<EstrelasMudou>.Unsubscribe(OnStars);
        }

        // As estrelas são re-coloridas pelo HudController quando o nível muda; re-aplicamos na sequência.
        private void OnStars(EstrelasMudou e) => ApplyStars();

        private void OnMudou()
        {
            ApplyTextScale();
            ApplyStars();
            _lastScale = AccessibilitySettings.TextScale;
            _lastMode  = AccessibilitySettings.ColorblindMode;
        }

        private void Update()
        {
            // barato: só reage quando algo muda de fato. O poll existe porque canvases podem surgir
            // tarde (HUD só nasce depois do slot ser escolhido) e novos Text aparecem com o menu.
            if (!Mathf.Approximately(_lastScale, AccessibilitySettings.TextScale) ||
                _lastMode != AccessibilitySettings.ColorblindMode)
            {
                OnMudou();
                return;
            }

            // descobre Texts novos (HUD/touch nascem após o boot) — uma passada leve por frame.
            if (Time.frameCount % 30 == 0)
                ApplyTextScale();
        }

        // ---------------- texto ----------------
        private void ApplyTextScale()
        {
            float scale = AccessibilitySettings.TextScale;
            var texts = FindObjectsByType<Text>(FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t == null) continue;
                if (!_baseSizes.TryGetValue(t, out int baseSize))
                {
                    baseSize = t.fontSize;
                    _baseSizes[t] = baseSize;
                }
                t.fontSize = Mathf.Max(8, Mathf.RoundToInt(baseSize * scale));
            }
        }

        // ---------------- estrelas de procurado ----------------
        private void ApplyStars()
        {
            // o canvas do HUD se chama "HUD" (HudController.BuildUi); as estrelas são Estrela0..4.
            var hud = FindCanvas("HUD");
            if (hud == null) return;

            Color acesa  = AccessibilitySettings.EstrelaAcesa;
            Color apag  = AccessibilitySettings.EstrelaApagada;

            // lê o nível atual do mundo, se disponível, para acender o número certo de estrelas
            int stars = 0;
            if (ServiceLocator.TryGet<WorldStateService>(out var world))
                stars = world.Stars;

            for (int i = 0; i < 5; i++)
            {
                var star = FindChildByName(hud.transform, "Estrela" + i);
                if (star == null) continue;
                var img = star.GetComponent<Image>();
                if (img != null) img.color = i < stars ? acesa : apag;
            }

            var nivel = FindChildByName(hud.transform, "NivelProcurado");
            if (nivel != null)
            {
                var txt = nivel.GetComponent<Text>();
                if (txt != null)
                {
                    txt.color = AccessibilitySettings.TextoProcurado;
                    if (stars > 0 && string.IsNullOrEmpty(txt.text))
                        txt.text = PoliceSystem.NomeDoNivel(stars);
                }
            }
        }

        // ---------------- helpers ----------------
        private static Canvas FindCanvas(string name)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i] != null && canvases[i].name == name)
                    return canvases[i];
            return null;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindChildByName(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
