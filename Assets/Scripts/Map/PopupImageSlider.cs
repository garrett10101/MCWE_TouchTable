using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Self-contained marker + image-crossfade popup script. Drop it on any map marker GameObject.
///
/// HOW TO CREATE AN IMAGE SLIDER MAP MARKER:
/// 1. Right-click in Hierarchy → Create Empty → rename (e.g. "SpringLake-Photos")
/// 2. Add Component → Sprite Renderer
/// 3. Add Component → Box Collider 2D
/// 4. Add Component → PopupImageSlider
/// 5. In the Inspector:
///    - Marker Sprite:    drag a circle/pin sprite (optional — falls back to Unity Knob)
///    - Marker Color:     pick your color
///    - Popup Width / Height: size of the dark popup panel (default 620 × 680)
///    - Position Mode:    NearMarker opens near tap; Centered always centers on screen
///    - Tap Offset:       nudge popup away from finger (NearMarker only)
///    - Slides:           expand the array, set Size, and for each element drag a Sprite
///                        into Picture and optionally type a Caption
///    - Show Captions:    global toggle; when off the caption strip is always hidden
///    - Caption Font Size / Color / Height: appearance of the caption text
///    - Slider Height / Colors: visual style of the slider bar and handle
/// 6. Position the marker on the map in the Scene view
/// 7. Press Play — clicking the marker shows the popup; drag the slider to crossfade images
/// </summary>

[System.Serializable]
public class ImageSlide
{
    public Sprite picture;
    [TextArea(2, 4)]
    public string caption = "";
}

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PopupImageSlider : MonoBehaviour, IPointerClickHandler
{
    [Header("Marker Appearance")]
    public Sprite markerSprite;
    public Color  markerColor = new Color(0.2f, 0.7f, 1f, 1f);

    [Header("Popup Layout")]
    public float             popupWidth   = 620f;
    public float             popupHeight  = 680f;
    public PopupPositionMode positionMode = PopupPositionMode.NearMarker;
    public Vector2           tapOffset    = new Vector2(20f, 20f);

    [Header("Image Slides")]
    public ImageSlide[] slides;
    public bool         showCaptions = true;

    [Header("Caption Style")]
    public float captionFontSize = 22f;
    public Color captionColor    = Color.white;
    public float captionHeight   = 100f;

    [Header("Slider Style")]
    public float sliderHeight    = 40f;
    public Color sliderBgColor   = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color sliderFillColor = new Color(0.2f, 0.7f, 1f, 1f);
    public Color handleColor     = Color.white;

    private ImageSliderPopupPanel _popupPanel;

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = markerSprite != null
            ? markerSprite
            : Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = markerColor;

        var col = GetComponent<BoxCollider2D>();
        if (sr.sprite != null)
            col.size = sr.sprite.bounds.size;

        if (Camera.main != null && Camera.main.GetComponent<Physics2DRaycaster>() == null)
            Camera.main.gameObject.AddComponent<Physics2DRaycaster>();
    }

    private void Start()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning($"[PopupImageSlider] '{name}': Slides array is empty — popup will not open.");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        var go = new GameObject($"ImageSliderPopup_{name}", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        _popupPanel = go.AddComponent<ImageSliderPopupPanel>();
        _popupPanel.Build(canvas, this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_popupPanel != null)
            _popupPanel.Show(eventData.position);
    }

    private void OnDestroy()
    {
        if (_popupPanel != null)
            Destroy(_popupPanel.gameObject);
    }
}

/// <summary>
/// Per-marker runtime-built popup panel containing two stacked Images crossfaded by a Slider.
/// One panel per PopupImageSlider instance; created once in Start(), not per-click.
/// </summary>
public class ImageSliderPopupPanel : MonoBehaviour
{
    private Canvas             _canvas;
    private PopupImageSlider   _config;
    private CanvasGroup        _group;
    private RectTransform      _content;
    private Image              _imageA;
    private Image              _imageB;
    private TextMeshProUGUI    _captionText;
    private GameObject         _captionBox;
    private Slider             _slider;

    public void Build(Canvas canvas, PopupImageSlider config)
    {
        _canvas = canvas;
        _config = config;

        // ── Root: full-screen stretch ──────────────────────────────────────────
        var root = GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        _group = gameObject.AddComponent<CanvasGroup>();

        // ── Backdrop: transparent full-screen click-to-dismiss ─────────────────
        var bd = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        bd.transform.SetParent(transform, false);
        var bdRect = bd.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero; bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = Vector2.zero; bdRect.offsetMax = Vector2.zero;
        bd.GetComponent<Image>().color = Color.clear;
        bd.GetComponent<Button>().onClick.AddListener(Close);

        // ── PopupContent: dark panel, centered anchor, top-left pivot ──────────
        var contentGo = new GameObject("PopupContent", typeof(RectTransform), typeof(Image));
        contentGo.transform.SetParent(transform, false);
        _content = contentGo.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0.5f, 0.5f);
        _content.anchorMax = new Vector2(0.5f, 0.5f);
        _content.pivot     = new Vector2(0f, 1f);
        _content.sizeDelta = new Vector2(config.popupWidth, config.popupHeight);
        contentGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        // ── Layout constants ───────────────────────────────────────────────────
        const float closeSize  = 60f;
        const float closeInset = 4f;
        const float sidePad    = 12f;
        const float sliderPad  = 10f;

        float sliderBottom  = sliderPad;
        float sliderTop     = sliderBottom + config.sliderHeight;
        float captionBottom = sliderTop + sliderPad;
        float captionTop    = captionBottom + config.captionHeight;
        float imageTop      = closeSize + closeInset + sidePad;
        float imageBottom   = (config.showCaptions ? captionTop : sliderTop) + sidePad;
        float imageHeight   = config.popupHeight - imageTop - imageBottom;

        if (imageHeight < 50f)
            Debug.LogWarning($"[PopupImageSlider] '{config.name}': imageHeight={imageHeight:F0} is very small — check popupHeight/captionHeight/sliderHeight settings.");

        // ── Close button: top-right red X ─────────────────────────────────────
        var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(contentGo.transform, false);
        var closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin        = new Vector2(1f, 1f);
        closeRect.anchorMax        = new Vector2(1f, 1f);
        closeRect.pivot            = new Vector2(1f, 1f);
        closeRect.sizeDelta        = new Vector2(closeSize, closeSize);
        closeRect.anchoredPosition = new Vector2(-closeInset, -closeInset);
        closeGo.GetComponent<Image>().color = new Color(0.7f, 0.05f, 0.05f, 1f);
        closeGo.GetComponent<Button>().onClick.AddListener(Close);

        var closeLbl = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeLbl.transform.SetParent(closeGo.transform, false);
        var clr = closeLbl.GetComponent<RectTransform>();
        clr.anchorMin = Vector2.zero; clr.anchorMax = Vector2.one;
        clr.offsetMin = Vector2.zero; clr.offsetMax = Vector2.zero;
        var clTmp = closeLbl.GetComponent<TextMeshProUGUI>();
        clTmp.text      = "X";
        clTmp.alignment = TextAlignmentOptions.Center;
        clTmp.fontSize  = 28;
        clTmp.fontStyle = FontStyles.Bold;
        clTmp.color     = Color.white;

        // ── ImageStack: two stacked Images for crossfade ───────────────────────
        var stackGo = new GameObject("ImageStack", typeof(RectTransform));
        stackGo.transform.SetParent(contentGo.transform, false);
        var stackRect = stackGo.GetComponent<RectTransform>();
        stackRect.anchorMin        = new Vector2(0f, 1f);
        stackRect.anchorMax        = new Vector2(1f, 1f);
        stackRect.pivot            = new Vector2(0.5f, 1f);
        stackRect.offsetMin        = new Vector2(sidePad, 0f);
        stackRect.offsetMax        = new Vector2(-sidePad, 0f);
        stackRect.sizeDelta        = new Vector2(0f, imageHeight);
        stackRect.anchoredPosition = new Vector2(0f, -imageTop);

        // ImageA — bottom layer, always alpha=1
        var imgAGo = new GameObject("ImageA", typeof(RectTransform), typeof(Image));
        imgAGo.transform.SetParent(stackGo.transform, false);
        var imgARect = imgAGo.GetComponent<RectTransform>();
        imgARect.anchorMin = Vector2.zero; imgARect.anchorMax = Vector2.one;
        imgARect.offsetMin = Vector2.zero; imgARect.offsetMax = Vector2.zero;
        _imageA = imgAGo.GetComponent<Image>();
        _imageA.preserveAspect = true;
        _imageA.color = Color.white;

        // ImageB — top layer, starts transparent
        var imgBGo = new GameObject("ImageB", typeof(RectTransform), typeof(Image));
        imgBGo.transform.SetParent(stackGo.transform, false);
        var imgBRect = imgBGo.GetComponent<RectTransform>();
        imgBRect.anchorMin = Vector2.zero; imgBRect.anchorMax = Vector2.one;
        imgBRect.offsetMin = Vector2.zero; imgBRect.offsetMax = Vector2.zero;
        _imageB = imgBGo.GetComponent<Image>();
        _imageB.preserveAspect = true;
        _imageB.color = Color.clear;

        // ── CaptionBox: TMP text above slider ─────────────────────────────────
        _captionBox = new GameObject("CaptionBox", typeof(RectTransform), typeof(TextMeshProUGUI));
        _captionBox.transform.SetParent(contentGo.transform, false);
        var captionRect = _captionBox.GetComponent<RectTransform>();
        captionRect.anchorMin        = new Vector2(0f, 0f);
        captionRect.anchorMax        = new Vector2(1f, 0f);
        captionRect.pivot            = new Vector2(0.5f, 0f);
        captionRect.offsetMin        = new Vector2(sidePad, 0f);
        captionRect.offsetMax        = new Vector2(-sidePad, 0f);
        captionRect.sizeDelta        = new Vector2(0f, config.captionHeight);
        captionRect.anchoredPosition = new Vector2(0f, captionBottom);
        _captionText = _captionBox.GetComponent<TextMeshProUGUI>();
        _captionText.fontSize         = config.captionFontSize;
        _captionText.color            = config.captionColor;
        _captionText.alignment        = TextAlignmentOptions.TopLeft;
        _captionText.enableWordWrapping = true;
        _captionBox.SetActive(false);

        // ── Slider: bottom strip ───────────────────────────────────────────────
        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(contentGo.transform, false);
        var sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin        = new Vector2(0f, 0f);
        sliderRect.anchorMax        = new Vector2(1f, 0f);
        sliderRect.pivot            = new Vector2(0.5f, 0f);
        sliderRect.offsetMin        = new Vector2(sidePad, 0f);
        sliderRect.offsetMax        = new Vector2(-sidePad, 0f);
        sliderRect.sizeDelta        = new Vector2(0f, config.sliderHeight);
        sliderRect.anchoredPosition = new Vector2(0f, sliderBottom);

        // Background
        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(sliderGo.transform, false);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(0f, 5f); bgRect.offsetMax = new Vector2(0f, -5f);
        bgGo.GetComponent<Image>().color = config.sliderBgColor;

        // Fill Area
        var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5f, 0f);
        fillAreaRect.offsetMax = new Vector2(-15f, 0f);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.sizeDelta = new Vector2(10f, 0f);
        fillGo.GetComponent<Image>().color = config.sliderFillColor;

        // Handle Slide Area
        var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGo.transform.SetParent(sliderGo.transform, false);
        var handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        var handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.sizeDelta = new Vector2(20f, 0f);
        handleGo.GetComponent<Image>().color = config.handleColor;

        // Wire up Slider component
        _slider = sliderGo.GetComponent<Slider>();
        _slider.direction  = Slider.Direction.LeftToRight;
        _slider.minValue   = 0f;
        _slider.maxValue   = 1f;
        _slider.value      = 0f;
        _slider.fillRect   = fillRect;
        _slider.handleRect = handleRect;

        // ── Single-slide guard ─────────────────────────────────────────────────
        if (config.slides.Length == 1)
        {
            sliderGo.SetActive(false);
            _slider = null;

            _imageA.sprite = config.slides[0].picture;
            _imageA.color  = config.slides[0].picture != null ? Color.white : new Color(0.15f, 0.15f, 0.15f, 1f);

            if (config.showCaptions && !string.IsNullOrEmpty(config.slides[0].caption))
            {
                _captionText.text = config.slides[0].caption;
                _captionBox.SetActive(true);
            }
        }
        else
        {
            _slider.onValueChanged.AddListener(OnSliderChanged);
            UpdateImages(0f);
        }

        if (!config.showCaptions)
            _captionBox.SetActive(false);

        SetVisible(false);
    }

    public void Show(Vector2 screenPos)
    {
        if (_config.positionMode == PopupPositionMode.NearMarker)
            PositionNear(screenPos);
        else
            PositionCentered();

        // Reset slider to first image each open
        if (_slider != null)
        {
            _slider.value = 0f;
            UpdateImages(0f);
        }

        SetVisible(true);
    }

    public void Close()
    {
        SetVisible(false);
    }

    private void OnSliderChanged(float value)
    {
        UpdateImages(value);
    }

    private void UpdateImages(float t)
    {
        if (_config.slides.Length == 1) return;

        float pos   = t * (_config.slides.Length - 1);
        int   from  = Mathf.FloorToInt(pos);
        int   to    = Mathf.Min(from + 1, _config.slides.Length - 1);
        float blend = pos - from;

        _imageA.sprite = _config.slides[from].picture;
        _imageA.color  = _config.slides[from].picture != null
            ? Color.white
            : new Color(0.15f, 0.15f, 0.15f, 1f);

        _imageB.sprite = _config.slides[to].picture;
        _imageB.color  = _config.slides[to].picture != null
            ? new Color(1f, 1f, 1f, blend)
            : Color.clear;

        // Caption: show the dominant (nearest) slide's caption
        if (_config.showCaptions)
        {
            int    dominant = Mathf.RoundToInt(pos);
            string cap      = _config.slides[dominant].caption;
            bool   show     = !string.IsNullOrEmpty(cap);
            _captionBox.SetActive(show);
            if (show) _captionText.text = cap;
        }
    }

    private void SetVisible(bool visible)
    {
        _group.alpha          = visible ? 1f : 0f;
        _group.blocksRaycasts = visible;
        _group.interactable   = visible;
    }

    private void PositionNear(Vector2 screenPos)
    {
        var canvasRect = _canvas.transform as RectTransform;
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCam, out Vector2 localPos);

        Vector2 target     = localPos + _config.tapOffset;
        Vector2 canvasHalf = canvasRect.rect.size * 0.5f;

        target.x = Mathf.Clamp(target.x, -canvasHalf.x, canvasHalf.x - _config.popupWidth);
        target.y = Mathf.Clamp(target.y, -canvasHalf.y + _config.popupHeight, canvasHalf.y);

        _content.anchoredPosition = target;
    }

    private void PositionCentered()
    {
        _content.anchoredPosition = new Vector2(-_config.popupWidth * 0.5f, _config.popupHeight * 0.5f);
    }
}
