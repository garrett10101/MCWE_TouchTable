using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Self-contained marker + popup script. Drop it on any map marker GameObject.
///
/// HOW TO CREATE A MAP MARKER:
/// 1. Right-click in Hierarchy → Create Empty → rename (e.g. "SpringLake-Video")
/// 2. Add Component → Sprite Renderer
/// 3. Add Component → Box Collider 2D
/// 4. Add Component → PopupText_With_Picture
/// 5. In the Inspector:
///    - Marker Sprite: drag a circle/pin sprite (optional — falls back to Unity Knob)
///    - Marker Color:  pick your color
///    - Popup Picture: drag a sprite (optional)
///    - Text File:     drag a .txt TextAsset from Project (takes priority over Popup Text)
///    - Popup Text:    type content directly if no Text File is assigned
/// 6. Position the marker on the map in the Scene view
/// 7. Press Play — clicking the marker shows the popup
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PopupText_With_Picture : MonoBehaviour, IPointerClickHandler
{
    [Header("Marker Appearance")]
    public Sprite markerSprite;
    public Color  markerColor = new Color(1f, 0.03f, 0f, 1f);

    [Header("Popup Layout")]
    public float popupWidth   = 500f;
    public float popupHeight  = 650f;
    public float imageHeight  = 250f;
    public float textFontSize = 22f;
    [Range(0f, 360f)]
    public float popupRotation = 0f;

    [Header("Popup Content")]
    public Sprite    popupPicture;
    public TextAsset textFile;
    [TextArea(4, 12)]
    public string popupText = "";

    // One panel shared by all markers in the scene; rebuilt lazily after scene reload.
    private static SharedPopupPanel _panel;

    private void Awake()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = markerSprite != null
            ? markerSprite
            : Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        spriteRenderer.color = markerColor;

        var col = GetComponent<BoxCollider2D>();
        if (spriteRenderer.sprite != null)
            col.size = spriteRenderer.sprite.bounds.size;

        // Physics2DRaycaster is required for IPointerClickHandler to receive events from 2D colliders.
        if (Camera.main != null && Camera.main.GetComponent<Physics2DRaycaster>() == null)
            Camera.main.gameObject.AddComponent<Physics2DRaycaster>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        string content = textFile != null ? textFile.text : popupText;
        EnsurePanel().Show(popupPicture, content, eventData.position,
            popupWidth, popupHeight, imageHeight, textFontSize, popupRotation);
    }

    private static SharedPopupPanel EnsurePanel()
    {
        if (_panel != null) return _panel;

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

        var go = new GameObject("MarkerPopup", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        _panel = go.AddComponent<SharedPopupPanel>();
        _panel.Build(canvas);
        return _panel;
    }
}

/// <summary>
/// Runtime-built popup panel shared by all PopupText_With_Picture markers in the scene.
/// </summary>
public class SharedPopupPanel : MonoBehaviour
{
    private Canvas          _canvas;
    private CanvasGroup     _group;
    private RectTransform   _content;
    private Image           _image;
    private RectTransform   _imgRect;
    private RectTransform   _scrollRt;
    private TextMeshProUGUI _text;
    private ScrollRect      _scroll;

    // Current popup dimensions — updated each Show() call
    private float _popupWidth;
    private float _popupHeight;

    private const float CloseSize = 60f;
    private const float SidePad   = 12f;
    private const float TopPad    = CloseSize + 8f;

    public void Build(Canvas canvas)
    {
        _canvas = canvas;

        // Root — stretches full canvas so backdrop can intercept all clicks
        var root = GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        _group = gameObject.AddComponent<CanvasGroup>();

        // Backdrop — plain Image (no Button) so clicks pass through to the map
        var bd = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        bd.transform.SetParent(transform, false);
        var bdRect = bd.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero; bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = Vector2.zero; bdRect.offsetMax = Vector2.zero;
        bd.GetComponent<Image>().color = Color.clear;

        // PopupContent — centered anchor, top-left pivot so anchoredPosition maps to tap point
        var contentGo = new GameObject("PopupContent", typeof(RectTransform), typeof(Image));
        contentGo.transform.SetParent(transform, false);
        _content = contentGo.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0.5f, 0.5f);
        _content.anchorMax = new Vector2(0.5f, 0.5f);
        _content.pivot     = new Vector2(0f, 1f);
        _content.sizeDelta = new Vector2(500f, 650f);
        contentGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        // Close button — top-right corner, red X
        var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(contentGo.transform, false);
        var closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin        = new Vector2(1f, 1f);
        closeRect.anchorMax        = new Vector2(1f, 1f);
        closeRect.pivot            = new Vector2(1f, 1f);
        closeRect.sizeDelta        = new Vector2(CloseSize, CloseSize);
        closeRect.anchoredPosition = new Vector2(-4f, -4f);
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

        // PopupImage — below close button, hidden when no sprite is assigned
        var imgGo = new GameObject("PopupImage", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(contentGo.transform, false);
        var imgRect = imgGo.GetComponent<RectTransform>();
        imgRect.anchorMin        = new Vector2(0f, 1f);
        imgRect.anchorMax        = new Vector2(1f, 1f);
        imgRect.pivot            = new Vector2(0.5f, 1f);
        imgRect.offsetMin        = new Vector2(SidePad, 0f);
        imgRect.offsetMax        = new Vector2(-SidePad, 0f);
        imgRect.sizeDelta        = new Vector2(0f, 250f);
        imgRect.anchoredPosition = new Vector2(0f, -TopPad);
        _imgRect = imgRect;
        _image = imgGo.GetComponent<Image>();
        _image.color          = new Color(0.2f, 0.2f, 0.2f, 1f);
        _image.preserveAspect = true;

        // ScrollRect — fills remaining space below the image
        var scrollGo = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(contentGo.transform, false);
        var scrollComp = scrollGo.GetComponent<ScrollRect>();
        var scrollRt   = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(SidePad, 10f);
        scrollRt.offsetMax = new Vector2(-SidePad, -(TopPad + 250f + 8f));
        _scrollRt = scrollRt;
        scrollGo.GetComponent<Image>().color = Color.clear;

        // Viewport
        var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpRect = vpGo.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = new Vector2(-20f, 0f);
        vpGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // near-zero alpha required by Mask
        vpGo.GetComponent<Mask>().showMaskGraphic = false;

        // Content (auto-sizes vertically)
        var contentNode = new GameObject("Content", typeof(RectTransform));
        contentNode.transform.SetParent(vpGo.transform, false);
        var cnRect = contentNode.GetComponent<RectTransform>();
        cnRect.anchorMin = new Vector2(0f, 1f);
        cnRect.anchorMax = new Vector2(1f, 1f);
        cnRect.pivot     = new Vector2(0.5f, 1f);
        cnRect.offsetMin = Vector2.zero;
        cnRect.offsetMax = Vector2.zero;
        var csf = contentNode.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Text
        var textGo = new GameObject("ContentText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(contentNode.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot     = new Vector2(0.5f, 1f);
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);
        _text = textGo.GetComponent<TextMeshProUGUI>();
        _text.fontSize           = 22f;
        _text.color              = Color.white;
        _text.enableWordWrapping = true;
        var textCsf = textGo.AddComponent<ContentSizeFitter>();
        textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Scrollbar
        var sbGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
        sbGo.transform.SetParent(scrollGo.transform, false);
        var sbRect = sbGo.GetComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1f, 0f); sbRect.anchorMax = new Vector2(1f, 1f);
        sbRect.pivot     = new Vector2(1f, 0.5f);
        sbRect.offsetMin = Vector2.zero;        sbRect.offsetMax = Vector2.zero;
        sbRect.sizeDelta = new Vector2(18f, 0f);
        sbGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var saGo = new GameObject("SlidingArea", typeof(RectTransform));
        saGo.transform.SetParent(sbGo.transform, false);
        var saRect = saGo.GetComponent<RectTransform>();
        saRect.anchorMin = Vector2.zero;          saRect.anchorMax = Vector2.one;
        saRect.offsetMin = new Vector2(0f, 10f);  saRect.offsetMax = new Vector2(0f, -10f);

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(saGo.transform, false);
        var handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero; handleRect.offsetMax = Vector2.zero;
        handleGo.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1f);

        var sb = sbGo.GetComponent<Scrollbar>();
        sb.direction  = Scrollbar.Direction.BottomToTop;
        sb.handleRect = handleRect;

        scrollComp.content    = cnRect;
        scrollComp.viewport   = vpRect;
        scrollComp.verticalScrollbar = sb;
        scrollComp.horizontal = false;
        scrollComp.vertical   = true;
        scrollComp.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollComp.scrollSensitivity = 30f;
        _scroll = scrollComp;

        SetVisible(false);
    }

    public void Show(Sprite picture, string text, Vector2 screenPos,
                     float popupWidth, float popupHeight, float imageHeight, float textFontSize,
                     float rotation = 0f)
    {
        // Apply per-marker layout
        _popupWidth  = popupWidth;
        _popupHeight = popupHeight;
        _content.sizeDelta = new Vector2(popupWidth, popupHeight);
        _content.localEulerAngles = new Vector3(0f, 0f, rotation);

        bool hasImage = picture != null;
        _imgRect.sizeDelta        = new Vector2(0f, imageHeight);
        _imgRect.anchoredPosition = new Vector2(0f, -TopPad);
        _imgRect.gameObject.SetActive(hasImage);

        float scrollTop = TopPad + (hasImage ? imageHeight + 8f : 0f);
        _scrollRt.offsetMax = new Vector2(-SidePad, -scrollTop);

        _text.fontSize = textFontSize;

        if (hasImage)
        {
            _image.sprite = picture;
            _image.color  = Color.white;
        }

        _text.text = string.IsNullOrEmpty(text) ? "Content coming soon." : text;

        PositionNear(screenPos);

        _scroll.verticalNormalizedPosition = 1f;
        SetVisible(true);
    }

    public void Close()
    {
        SetVisible(false);
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
        Camera uiCam   = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCam, out Vector2 localPos);

        Vector2 target     = localPos + new Vector2(20f, 20f);
        Vector2 canvasHalf = canvasRect.rect.size * 0.5f;

        target.x = Mathf.Clamp(target.x, -canvasHalf.x, canvasHalf.x - _popupWidth);
        target.y = Mathf.Clamp(target.y, -canvasHalf.y + _popupHeight, canvasHalf.y);

        _content.anchoredPosition = target;
    }
}
