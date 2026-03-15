using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Self-contained marker + video popup script. Drop it on any map marker GameObject.
///
/// HOW TO CREATE A VIDEO MAP MARKER:
/// 1. Right-click in Hierarchy → Create Empty → rename (e.g. "SpringLake-Video")
/// 2. Add Component → Sprite Renderer
/// 3. Add Component → Box Collider 2D
/// 4. Add Component → PopupVideo
/// 5. In the Inspector:
///    - Marker Sprite: drag a circle/pin sprite (optional — falls back to Unity Knob)
///    - Marker Color:  pick your color
///    - Video Clip:    drag a VideoClip asset from the Project window
///    - Popup Width / Height: size of the dark popup panel (default 640 × 520)
///    - Position Mode: NearMarker opens near tap; Centered always centers on screen
///    - Tap Offset:    nudge popup away from finger (NearMarker only)
///    - Video Width / Height: bounding box for video inside popup (default 616 × 346)
///    - Loop Video:    if enabled, video loops until user closes popup
///    - Close On End:  if enabled (and Loop Video is off), popup closes when playback ends
///    - Show Title:    toggle a header text strip above the video
///    - Title Text:    text for header (leave empty to suppress header even with Show Title on)
/// 6. Position the marker on the map in the Scene view
/// 7. Press Play — clicking the marker shows the video popup
/// </summary>

public enum PopupPositionMode { NearMarker, Centered }

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PopupVideo : MonoBehaviour, IPointerClickHandler
{
    [Header("Marker Appearance")]
    public Sprite markerSprite;
    public Color  markerColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("Popup Layout")]
    public float             popupWidth    = 640f;
    public float             popupHeight   = 520f;
    public PopupPositionMode positionMode  = PopupPositionMode.NearMarker;
    public Vector2           tapOffset     = new Vector2(20f, 20f);

    [Header("Video Content")]
    public VideoClip videoClip;
    public float     videoWidth  = 616f;
    public float     videoHeight = 346f;
    public bool      loopVideo   = false;
    public bool      closeOnEnd  = false;

    [Header("Title Header")]
    public bool   showTitle    = false;
    [TextArea(1, 2)]
    public string titleText    = "Location Name";
    public float  titleFontSize = 26f;
    public Color  titleColor   = Color.white;
    public float  headerHeight = 50f;

    private VideoPopupPanel _popupPanel;

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

        var go = new GameObject($"VideoPopup_{name}", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        _popupPanel = go.AddComponent<VideoPopupPanel>();
        _popupPanel.Build(canvas, this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _popupPanel.Show(eventData.position);
    }

    private void OnDestroy()
    {
        if (_popupPanel != null)
            Destroy(_popupPanel.gameObject);
    }
}

/// <summary>
/// Per-marker runtime-built popup panel containing a VideoPlayer and RenderTexture.
/// One panel per PopupVideo instance; created once in Start(), not per-click.
/// </summary>
public class VideoPopupPanel : MonoBehaviour
{
    private Canvas          _canvas;
    private PopupVideo      _config;
    private CanvasGroup     _group;
    private RectTransform   _content;
    private VideoPlayer     _videoPlayer;
    private RenderTexture   _renderTexture;
    private AspectRatioFitter _aspectFitter;

    public void Build(Canvas canvas, PopupVideo config)
    {
        _canvas = canvas;
        _config = config;

        if (config.closeOnEnd && config.loopVideo)
            Debug.LogWarning($"[PopupVideo] '{config.name}': closeOnEnd has no effect when loopVideo is true — loopPointReached never fires for looping clips.");

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

        const float closeSize  = 60f;
        const float sidePad    = 12f;
        const float closeInset = 4f;

        bool hasHeader = config.showTitle && !string.IsNullOrEmpty(config.titleText);
        float headerH  = hasHeader ? config.headerHeight : 0f;
        float videoTopOff = closeSize + sidePad + headerH;

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

        // ── Title header (optional) ───────────────────────────────────────────
        var titleGo = new GameObject("TitleHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(contentGo.transform, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin        = new Vector2(0f, 1f);
        titleRect.anchorMax        = new Vector2(1f, 1f);
        titleRect.pivot            = new Vector2(0.5f, 1f);
        titleRect.offsetMin        = new Vector2(sidePad, 0f);
        titleRect.offsetMax        = new Vector2(-(closeSize + sidePad), 0f);
        titleRect.sizeDelta        = new Vector2(0f, config.headerHeight);
        titleRect.anchoredPosition = new Vector2(0f, -(closeSize + sidePad));
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text             = config.titleText;
        titleTmp.fontSize         = config.titleFontSize;
        titleTmp.color            = config.titleColor;
        titleTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        titleTmp.enableWordWrapping = false;
        titleGo.SetActive(hasHeader);

        // ── RenderTexture ─────────────────────────────────────────────────────
        if (config.videoClip != null)
        {
            _renderTexture = new RenderTexture(
                Mathf.RoundToInt(config.videoWidth),
                Mathf.RoundToInt(config.videoHeight),
                0, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
        }

        // ── VideoDisplay: RawImage + AspectRatioFitter ────────────────────────
        var videoGo = new GameObject("VideoDisplay", typeof(RectTransform), typeof(RawImage));
        videoGo.transform.SetParent(contentGo.transform, false);
        var videoRect = videoGo.GetComponent<RectTransform>();
        videoRect.anchorMin        = new Vector2(0.5f, 1f);
        videoRect.anchorMax        = new Vector2(0.5f, 1f);
        videoRect.pivot            = new Vector2(0.5f, 1f);
        videoRect.sizeDelta        = new Vector2(config.videoWidth, config.videoHeight);
        videoRect.anchoredPosition = new Vector2(0f, -videoTopOff);

        var rawImage = videoGo.GetComponent<RawImage>();
        if (_renderTexture != null)
            rawImage.texture = _renderTexture;

        _aspectFitter = videoGo.AddComponent<AspectRatioFitter>();
        _aspectFitter.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
        _aspectFitter.aspectRatio = config.videoWidth / Mathf.Max(config.videoHeight, 1f);

        // ── VideoPlayer on PopupContent ───────────────────────────────────────
        if (config.videoClip != null)
        {
            _videoPlayer = contentGo.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake     = false;
            _videoPlayer.renderMode      = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture   = _renderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPlayer.isLooping       = config.loopVideo;
            _videoPlayer.clip            = config.videoClip;
            _videoPlayer.prepareCompleted += OnVideoPrepared;

            if (config.closeOnEnd && !config.loopVideo)
                _videoPlayer.loopPointReached += OnVideoLoopPointReached;
        }

        SetVisible(false);
    }

    public void Show(Vector2 screenPos)
    {
        if (_config.positionMode == PopupPositionMode.NearMarker)
            PositionNear(screenPos);
        else
            PositionCentered();

        SetVisible(true);

        if (_config.videoClip != null && _videoPlayer != null)
        {
            _videoPlayer.Stop();
            _videoPlayer.Play();
        }
    }

    public void Close()
    {
        if (_videoPlayer != null)
            _videoPlayer.Stop();
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

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (vp.texture == null)
        {
            Debug.LogWarning($"[PopupVideo] '{_config.name}': OnVideoPrepared — vp.texture is null (Android decode may be delayed).");
            return;
        }
        _aspectFitter.aspectRatio = (float)vp.texture.width / Mathf.Max(vp.texture.height, 1);
    }

    private void OnVideoLoopPointReached(VideoPlayer vp)
    {
        Close();
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.loopPointReached -= OnVideoLoopPointReached;
            _videoPlayer.Stop();
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }
}
