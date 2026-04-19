using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WatershedSim
{
    /// <summary>
    /// Builds and manages all UI for the Watershed Simulation scene at runtime.
    /// Attach to any active GameObject. Requires TextMeshPro in the project.
    ///
    /// Layout:
    ///   Top bar       — Pollution meter + percentage label
    ///   Bottom bar    — Mode buttons (Camera | Water | Dam | Pollutant | Remove)
    ///   Left panel    — Rain toggle + Rain Intensity slider
    ///   Right panel   — Flow Damping slider
    ///   Top-left      — Back button (→ MainMenu)
    ///   Bottom-right  — Reset button
    /// </summary>
    public class WatershedUI : MonoBehaviour
    {
        [Header("References")]
        public WaterSimulation    waterSim;
        public WatershedInteraction interaction;

        [Tooltip("Scene name to load when Back is pressed.")]
        public string mainMenuScene = "MainMenu";

        [Header("Pollution Colors")]
        [Tooltip("Colors offered when placing pollutants via UI. First color is the default.")]
        public Color[] pollutantColors = new Color[]
        {
            new Color(0.6f, 0.2f, 0.0f),  // brown  (fertiliser runoff)
            new Color(0.8f, 0.1f, 0.1f),  // red    (industrial)
            new Color(0.1f, 0.5f, 0.1f),  // green  (algae bloom)
            new Color(0.5f, 0.0f, 0.5f),  // purple (chemical)
        };

        // ── UI State ──────────────────────────────────────────────────────────

        Image         _pollutionFill;
        TMP_Text      _pollutionLabel;
        List<Button>  _modeButtons = new();
        Slider        _rainSlider;
        Slider        _dampingSlider;
        Button[]      _colorButtons;
        GameObject    _colorRow;
        int           _selectedColorIndex = 0;

        static readonly Color ActiveButtonColor   = new Color(0.20f, 0.60f, 1.00f);
        static readonly Color InactiveButtonColor = new Color(0.15f, 0.15f, 0.20f);

        // ── Lifecycle ──────────────────────────────────────────────────────────

        void Awake()
        {
            BuildUI();
        }

        void Update()
        {
            if (waterSim == null) return;

            float level = waterSim.GetOverallPollutionLevel();
            if (_pollutionFill  != null) _pollutionFill.fillAmount = level;
            if (_pollutionLabel != null) _pollutionLabel.text = $"Watershed Pollution: {level * 100f:F0}%";
        }

        // ── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            // --- Root Canvas ---
            var canvasGO = new GameObject("WatershedCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // --- Panels ---
            BuildTopBar(canvasGO);
            BuildBottomBar(canvasGO);
            BuildLeftPanel(canvasGO);
            BuildRightPanel(canvasGO);
            BuildBackButton(canvasGO);
            BuildResetButton(canvasGO);
        }

        // Top bar: pollution meter ─────────────────────────────────────────────

        void BuildTopBar(GameObject canvas)
        {
            var bar = MakePanel(canvas, "TopBar",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -80), new Vector2(0, 0),
                new Color(0.05f, 0.05f, 0.10f, 0.80f));
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80f);

            // Background track
            var track = MakePanel(bar.gameObject, "PollTrack",
                new Vector2(0.05f, 0.2f), new Vector2(0.75f, 0.8f),
                Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.2f, 0.2f, 1f));

            // Fill (green → red via Image.fillAmount)
            var fillGO  = new GameObject("PollFill");
            fillGO.transform.SetParent(track.transform, false);
            _pollutionFill = fillGO.AddComponent<Image>();
            _pollutionFill.color      = new Color(0.8f, 0.2f, 0.1f);
            _pollutionFill.type       = Image.Type.Filled;
            _pollutionFill.fillMethod = Image.FillMethod.Horizontal;
            _pollutionFill.fillAmount = 0f;
            var frt = fillGO.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            // Label
            _pollutionLabel = MakeLabel(bar.gameObject, "PollLabel",
                new Vector2(0.76f, 0.1f), new Vector2(0.99f, 0.9f),
                "Pollution: 0%", 22f);
        }

        // Bottom bar: mode buttons ─────────────────────────────────────────────

        void BuildBottomBar(GameObject canvas)
        {
            var bar = MakePanel(canvas, "BottomBar",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, 100),
                new Color(0.05f, 0.05f, 0.10f, 0.85f));
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);

            string[] labels = { "Camera", "Water", "Dam", "Pollutant", "Remove" };
            var modes = new WatershedInteraction.InteractionMode[]
            {
                WatershedInteraction.InteractionMode.Camera,
                WatershedInteraction.InteractionMode.Water,
                WatershedInteraction.InteractionMode.Dam,
                WatershedInteraction.InteractionMode.Pollutant,
                WatershedInteraction.InteractionMode.Remove,
            };

            float step = 1f / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                float xMin = step * i;
                float xMax = step * (i + 1);
                var btn = MakeButton(bar.gameObject, labels[i],
                    new Vector2(xMin + 0.01f, 0.05f), new Vector2(xMax - 0.01f, 0.95f),
                    labels[i], 24f, InactiveButtonColor);
                btn.onClick.AddListener(() => OnModeButtonClicked(idx, modes[idx]));
                _modeButtons.Add(btn);
            }

            // Pollutant color pickers (shown only when Pollutant mode is active)
            _colorRow = new GameObject("ColorRow");
            _colorRow.transform.SetParent(bar.transform, false);
            var crt = _colorRow.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.sizeDelta = new Vector2(0f, 50f);
            crt.anchoredPosition = new Vector2(0f, 25f);
            _colorRow.SetActive(false);

            _colorButtons = new Button[pollutantColors.Length];
            float cs = 1f / pollutantColors.Length;
            for (int i = 0; i < pollutantColors.Length; i++)
            {
                int ci = i;
                Color c = pollutantColors[i];
                var cb = MakeButton(_colorRow, $"Color{i}",
                    new Vector2(cs * i + 0.01f, 0.05f), new Vector2(cs * (i+1) - 0.01f, 0.95f),
                    "", 1f, c);
                cb.onClick.AddListener(() => SelectPollutantColor(ci));
                _colorButtons[i] = cb;
            }

            // Highlight first mode button (Camera)
            RefreshModeButtons(0);
        }

        // Left panel: rain controls ────────────────────────────────────────────

        void BuildLeftPanel(GameObject canvas)
        {
            var panel = MakePanel(canvas, "LeftPanel",
                new Vector2(0, 0.15f), new Vector2(0, 0.65f),
                new Vector2(0, 0), new Vector2(160, 0),
                new Color(0.05f, 0.05f, 0.10f, 0.75f));
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);

            MakeLabel(panel.gameObject, "RainTitle",
                new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f),
                "Rain", 26f);

            // Toggle button
            bool rainOn = false;
            Button rainToggle = MakeButton(panel.gameObject, "RainToggle",
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f),
                "OFF", 22f, new Color(0.2f, 0.2f, 0.3f));
            rainToggle.onClick.AddListener(() =>
            {
                rainOn = !rainOn;
                waterSim?.SetRain(rainOn);
                var lbl = rainToggle.GetComponentInChildren<TMP_Text>();
                if (lbl != null) lbl.text = rainOn ? "ON" : "OFF";
                var img = rainToggle.GetComponent<Image>();
                if (img != null) img.color = rainOn ? new Color(0.1f, 0.5f, 0.9f) : new Color(0.2f, 0.2f, 0.3f);
            });

            MakeLabel(panel.gameObject, "IntensityLabel",
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.52f),
                "Intensity", 20f);

            _rainSlider = MakeSlider(panel.gameObject, "RainSlider",
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.35f),
                0f, 0.5f, 0f);
            _rainSlider.onValueChanged.AddListener(v =>
            {
                if (waterSim != null) waterSim.SetRainRate(v);
            });
        }

        // Right panel: damping ────────────────────────────────────────────────

        void BuildRightPanel(GameObject canvas)
        {
            var panel = MakePanel(canvas, "RightPanel",
                new Vector2(1, 0.15f), new Vector2(1, 0.65f),
                new Vector2(-160, 0), new Vector2(0, 0),
                new Color(0.05f, 0.05f, 0.10f, 0.75f));
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);

            MakeLabel(panel.gameObject, "DampTitle",
                new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f),
                "Flow Speed", 22f);

            _dampingSlider = MakeSlider(panel.gameObject, "DampSlider",
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.75f),
                0.90f, 1.00f, 0.99f);
            // Slider goes right = faster flow (less damping)
            _dampingSlider.onValueChanged.AddListener(v =>
            {
                if (waterSim != null) waterSim.SetDamping(v);
            });

            MakeLabel(panel.gameObject, "SlowFast",
                new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.42f),
                "Slow  ←→  Fast", 16f);
        }

        // Back and Reset ───────────────────────────────────────────────────────

        void BuildBackButton(GameObject canvas)
        {
            var btn = MakeButton(canvas, "BackButton",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                "◀ Back", 22f, new Color(0.15f, 0.15f, 0.20f));
            var rt = btn.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(65f, -30f);
            rt.sizeDelta = new Vector2(120f, 50f);
            btn.onClick.AddListener(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene));
        }

        void BuildResetButton(GameObject canvas)
        {
            var btn = MakeButton(canvas, "ResetButton",
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                "Reset", 22f, new Color(0.6f, 0.15f, 0.10f));
            var rt = btn.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-65f, 110f);
            rt.sizeDelta = new Vector2(110f, 50f);
            btn.onClick.AddListener(() => waterSim?.ClearAll(regenerateTerrain: true));
        }

        // ── Mode Button Logic ─────────────────────────────────────────────────

        void OnModeButtonClicked(int idx, WatershedInteraction.InteractionMode mode)
        {
            if (interaction != null)
                interaction.SetMode(mode);
            RefreshModeButtons(idx);

            // Show color picker row only in Pollutant mode
            bool isPollutant = mode == WatershedInteraction.InteractionMode.Pollutant;
            if (_colorRow != null) _colorRow.SetActive(isPollutant);
        }

        void RefreshModeButtons(int activeIdx)
        {
            for (int i = 0; i < _modeButtons.Count; i++)
            {
                var img = _modeButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == activeIdx) ? ActiveButtonColor : InactiveButtonColor;
            }
        }

        void SelectPollutantColor(int idx)
        {
            _selectedColorIndex = idx;
            if (interaction != null && idx < pollutantColors.Length)
                interaction.pollutionColor = pollutantColors[idx];

            // Highlight selected color button
            for (int i = 0; i < _colorButtons.Length; i++)
            {
                var rt2 = _colorButtons[i].GetComponent<RectTransform>();
                if (rt2 != null)
                    rt2.localScale = (i == idx) ? Vector3.one * 1.15f : Vector3.one;
            }
        }

        // ── UI Primitives ─────────────────────────────────────────────────────

        static RectTransform MakePanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color color)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        static TMP_Text MakeLabel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            string text, float fontSize)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var lbl = go.AddComponent<TextMeshProUGUI>();
            lbl.text      = text;
            lbl.fontSize  = fontSize;
            lbl.color     = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            return lbl;
        }

        static Button MakeButton(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            string label, float fontSize, Color bgColor)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();

            // Label child
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = label;
            lbl.fontSize  = fontSize;
            lbl.color     = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;

            var cs = btn.colors;
            cs.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f);
            cs.pressedColor     = new Color(bgColor.r - 0.1f,  bgColor.g - 0.1f,  bgColor.b - 0.1f);
            btn.colors = cs;

            return btn;
        }

        static Slider MakeSlider(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            float minVal, float maxVal, float initialVal)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var slider = go.AddComponent<Slider>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value    = initialVal;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.3f);
            bgRt.anchorMax = new Vector2(1f, 0.7f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            // Fill area
            var fillAreaGO = new GameObject("FillArea");
            fillAreaGO.transform.SetParent(go.transform, false);
            var faRt = fillAreaGO.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.3f);
            faRt.anchorMax = new Vector2(1f, 0.7f);
            faRt.offsetMin = new Vector2(5f, 0f);
            faRt.offsetMax = new Vector2(-15f, 0f);

            var fillGO  = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 1.0f);
            var fillRt = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            slider.fillRect = fillRt;

            // Handle
            var handleAreaGO = new GameObject("HandleArea");
            handleAreaGO.transform.SetParent(go.transform, false);
            var haRt = handleAreaGO.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = haRt.offsetMax = Vector2.zero;

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handleGO.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20f, 20f);
            slider.handleRect = handleRt;

            slider.targetGraphic = handleImg;
            return slider;
        }
    }
}
