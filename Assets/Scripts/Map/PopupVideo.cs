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
///    - Popup Offset: canvas-space (X, Y) offset from the marker position (default 20, 20)
///    - Video Width / Height: bounding box for video inside popup (default 616 × 346)
///    - Loop Video:    if enabled, video loops until user closes popup
///    - Close On End:  if enabled (and Loop Video is off), popup closes when playback ends
///    - Show Title:    toggle a header text strip above the video
///    - Title Text:    text for header (leave empty to suppress header even with Show Title on)
/// 6. Position the marker on the map in the Scene view
/// 7. Press Play — clicking the marker shows the video popup
/// </summary>

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PopupVideo : MonoBehaviour, IPointerClickHandler
{
    [Header("Marker Appearance")]
    public Sprite markerSprite;
    public Color  markerColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("Popup Layout")]
    public float popupWidth    = 640f;
    public float popupHeight   = 520f;
    [Range(0f, 360f)]
    public float popupRotation = 0f;

    [Header("Popup Position")]
    public Vector2 popupOffset = new Vector2(20f, 20f);

    [Header("Close Button Style")]
    public float closeButtonSize    = 60f;
    public float closeButtonInset   = 4f;
    public Color closeButtonColor   = new Color(0.7f, 0.05f, 0.05f, 1f);
    public float closeLabelFontSize = 28f;
    public Color closeLabelColor    = Color.white;

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

    // Called by Unity Editor when the component is first added or Reset is clicked.
    // Pre-populates markerSprite with the built-in Knob as a visible placeholder.
    private void Reset()
    {
        markerSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = markerSprite;
    }

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (markerSprite != null) sr.sprite = markerSprite;
        sr.color = markerColor;

        var col = GetComponent<BoxCollider2D>();
        if (sr.sprite != null)
            col.size = sr.sprite.bounds.size;

    }

    private void Start()
    {
        // Physics2DRaycaster is required for IPointerClickHandler to receive events from 2D colliders.
        // Added in Start() (not Awake()) so Camera.main is reliably available on Android.
        if (Camera.main != null && Camera.main.GetComponent<Physics2DRaycaster>() == null)
            Camera.main.gameObject.AddComponent<Physics2DRaycaster>();

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
        _popupPanel.Show();
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

        // ── Backdrop: plain Image (no Button) so clicks pass through to the map ──
        var bd = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        bd.transform.SetParent(transform, false);
        var bdRect = bd.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero; bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = Vector2.zero; bdRect.offsetMax = Vector2.zero;
        bd.GetComponent<Image>().color = Color.clear;
        bd.GetComponent<Image>().raycastTarget = false;

        // ── PopupContent: dark panel, centered anchor, top-left pivot ──────────
        var contentGo = new GameObject("PopupContent", typeof(RectTransform), typeof(Image));
        contentGo.transform.SetParent(transform, false);
        _content = contentGo.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0.5f, 0.5f);
        _content.anchorMax = new Vector2(0.5f, 0.5f);
        _content.pivot     = new Vector2(0f, 1f);
        _content.sizeDelta = new Vector2(config.popupWidth, config.popupHeight);
        _content.localEulerAngles = new Vector3(0f, 0f, config.popupRotation);
        contentGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        const float sidePad = 12f;

        bool hasHeader = config.showTitle && !string.IsNullOrEmpty(config.titleText);
        float headerH  = hasHeader ? config.headerHeight : 0f;
        float videoTopOff = config.closeButtonSize + sidePad + headerH;

        // ── Title header (optional) ───────────────────────────────────────────
        var titleGo = new GameObject("TitleHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(contentGo.transform, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin        = new Vector2(0f, 1f);
        titleRect.anchorMax        = new Vector2(1f, 1f);
        titleRect.pivot            = new Vector2(0.5f, 1f);
        titleRect.offsetMin        = new Vector2(sidePad, 0f);
        titleRect.offsetMax        = new Vector2(-(config.closeButtonSize + sidePad), 0f);
        titleRect.sizeDelta        = new Vector2(0f, config.headerHeight);
        titleRect.anchoredPosition = new Vector2(0f, -(config.closeButtonSize + sidePad));
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

        // ── Close button ───────────────────────────────────────────────────────
        PopupUIHelper.CreateCloseButton(contentGo, Close,
            config.closeButtonSize, config.closeButtonInset,
            config.closeButtonColor, config.closeLabelFontSize, config.closeLabelColor);

        SetVisible(false);
    }

    public void Show()
    {
        var canvasRect = _canvas.transform as RectTransform;
        Camera worldCam = Camera.main;
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        _content.anchoredPosition = PopupUIHelper.GetPopupAnchoredPosition(
            _config.transform.position, _config.popupOffset,
            canvasRect, _config.popupWidth, _config.popupHeight,
            worldCam, uiCam);

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
