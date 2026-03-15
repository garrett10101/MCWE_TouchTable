using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility that programmatically creates and configures all four TouchTable
/// demo scenes (MainMenu, TouchPop, ScrollableText, BackgroundBlend) plus the Target prefab.
/// Run via: Tools > TouchTable > Build All Scenes
/// </summary>
public static class SceneBuilder
{
    [MenuItem("Tools/TouchTable/Build All Scenes")]
    public static void BuildAllScenes()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        CreateTargetPrefab();
        CreateMainMenuScene();
        CreateTouchPopScene();
        CreateScrollableTextScene();
        CreateBackgroundBlendScene();
        RegisterBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("TouchTable: All scenes and prefabs built successfully.");
    }

    // -------------------------------------------------------------------------
    // Target Prefab
    // -------------------------------------------------------------------------

    private static void CreateTargetPrefab()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Target.png");

        GameObject go = new GameObject("Target");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (sprite != null)
            sr.sprite = sprite;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        go.AddComponent<TouchTarget>();
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Target.prefab");
        Object.DestroyImmediate(go);
    }

    // -------------------------------------------------------------------------
    // MainMenu Scene
    // -------------------------------------------------------------------------

    private static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Color(0.08f, 0.08f, 0.15f));
        CreateEventSystem();

        GameObject canvasGo = CreateCanvas();
        Transform canvas = canvasGo.transform;
        SceneLoader sceneLoader = canvasGo.AddComponent<SceneLoader>();

        // Title — top-center
        GameObject title = CreateText(canvas, "Title", "Touch Table Demo", 48, FontStyle.Bold);
        AnchorTopCenter(title, 700f, 90f, 60f);

        // Three scene buttons — centered on screen, stacked
        GameObject btn1 = CreateButton(canvas, "TouchPopButton", "Touch Pop (Whack-a-Mole)", new Color(0.2f, 0.5f, 0.8f));
        AnchorCenter(btn1, 700f, 110f, 0f, 120f);
        UnityEventTools.AddStringPersistentListener(btn1.GetComponent<Button>().onClick, sceneLoader.LoadSceneByName, "TouchPop");

        GameObject btn2 = CreateButton(canvas, "ScrollableTextButton", "Scrollable Text", new Color(0.2f, 0.5f, 0.8f));
        AnchorCenter(btn2, 700f, 110f, 0f, -20f);
        UnityEventTools.AddStringPersistentListener(btn2.GetComponent<Button>().onClick, sceneLoader.LoadSceneByName, "ScrollableText");

        GameObject btn3 = CreateButton(canvas, "BackgroundBlendButton", "Background Blend", new Color(0.2f, 0.5f, 0.8f));
        AnchorCenter(btn3, 700f, 110f, 0f, -160f);
        UnityEventTools.AddStringPersistentListener(btn3.GetComponent<Button>().onClick, sceneLoader.LoadSceneByName, "BackgroundBlend");

        // Quit — bottom-center
        GameObject quitBtn = CreateButton(canvas, "QuitButton", "Quit", new Color(0.7f, 0.2f, 0.2f));
        AnchorBottomCenter(quitBtn, 300f, 80f, 40f);
        UnityEventTools.AddVoidPersistentListener(quitBtn.GetComponent<Button>().onClick, sceneLoader.Quit);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // -------------------------------------------------------------------------
    // TouchPop Scene
    // -------------------------------------------------------------------------

    private static void CreateTouchPopScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Color(0.05f, 0.15f, 0.05f));
        CreateEventSystem();

        GameObject canvasGo = CreateCanvas();
        Transform canvas = canvasGo.transform;
        SceneLoader sceneLoader = canvasGo.AddComponent<SceneLoader>();

        // Score label — top-center
        GameObject scoreTextGo = CreateText(canvas, "ScoreText", "Score: 0", 36, FontStyle.Bold);
        AnchorTopCenter(scoreTextGo, 400f, 70f, 15f);
        Text scoreText = scoreTextGo.GetComponent<Text>();

        // Back button — top-left
        GameObject backBtn = CreateButton(canvas, "BackButton", "< Back", new Color(0.2f, 0.2f, 0.5f));
        AnchorTopLeft(backBtn, 160f, 60f, 15f);
        UnityEventTools.AddStringPersistentListener(backBtn.GetComponent<Button>().onClick, sceneLoader.LoadSceneByName, "MainMenu");

        // TouchPopManager
        GameObject managerGo = new GameObject("TouchPopManager");
        TouchPopManager manager = managerGo.AddComponent<TouchPopManager>();
        manager.targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Target.prefab");
        manager.spawnArea = new Rect(-4f, -3f, 8f, 6f);
        manager.spawnIntervalMin = 0.5f;
        manager.spawnIntervalMax = 1.25f;
        manager.maxActiveTargets = 5;
        manager.targetLifetime = 2f;
        manager.scoreText = scoreText;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TouchPop.unity");
    }

    // -------------------------------------------------------------------------
    // ScrollableText Scene
    // -------------------------------------------------------------------------

    private static void CreateScrollableTextScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Color(0.1f, 0.08f, 0.15f));
        CreateEventSystem();

        GameObject canvasGo = CreateCanvas();
        Transform canvas = canvasGo.transform;
        SceneLoader sceneLoader = canvasGo.AddComponent<SceneLoader>();
        ScrollableTextController controller = canvasGo.AddComponent<ScrollableTextController>();

        // Header — top-center
        GameObject header = CreateText(canvas, "Header", "Scrollable Text Demo", 42, FontStyle.Bold);
        AnchorTopCenter(header, 700f, 80f, 20f);

        // Back button — top-left
        GameObject backBtn = CreateButton(canvas, "BackButton", "< Back", new Color(0.2f, 0.2f, 0.5f));
        AnchorTopLeft(backBtn, 160f, 60f, 15f);
        UnityEventTools.AddStringPersistentListener(backBtn.GetComponent<Button>().onClick, sceneLoader.LoadSceneByName, "MainMenu");

        // Open Popup button — centered
        GameObject openBtn = CreateButton(canvas, "OpenButton", "Open Info Panel", new Color(0.2f, 0.5f, 0.8f));
        AnchorCenter(openBtn, 500f, 100f, 0f, 0f);
        UnityEventTools.AddVoidPersistentListener(openBtn.GetComponent<Button>().onClick, controller.OpenPopup);

        // Popup Panel — full screen overlay
        GameObject popupPanel = CreatePopupPanel(canvas, out ScrollRect scrollRect, out Text contentText);
        Button closeBtn = popupPanel.transform.Find("CloseButton").GetComponent<Button>();
        UnityEventTools.AddVoidPersistentListener(closeBtn.onClick, controller.ClosePopup);

        // Bake text into scene so ContentSizeFitter can measure it on load
        string longText = SampleLongText();
        contentText.text = longText;

        controller.popupPanel = popupPanel;
        controller.scrollRect = scrollRect;
        controller.contentText = contentText;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ScrollableText.unity");
    }

    // -------------------------------------------------------------------------
    // BackgroundBlend Scene
    // -------------------------------------------------------------------------

    private static void CreateBackgroundBlendScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Color(0f, 0f, 0f));
        CreateEventSystem();

        GameObject canvasGo = CreateCanvas();
        Transform canvas = canvasGo.transform;
        SceneLoader sceneLoader = canvasGo.AddComponent<SceneLoader>();
        BackgroundBlendController controller = canvasGo.AddComponent<BackgroundBlendController>();

        // 4 background images — stretch-fill the canvas, stacked in order
        string[] spriteNames = { "Background_Morning", "Background_Afternoon", "Background_Evening", "Background_Night" };
        string[] timeLabels  = { "Morning", "Afternoon", "Evening", "Night" };
        Image[] bgImages = new Image[spriteNames.Length];

        for (int i = 0; i < spriteNames.Length; i++)
        {
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{spriteNames[i]}.png");
            GameObject bgGo = new GameObject(spriteNames[i]);
            bgGo.transform.SetParent(canvas, false);
            AnchorStretchFill(bgGo);
            Image img = bgGo.AddComponent<Image>();
            img.preserveAspect = false;
            if (spr != null) img.sprite = spr;
            Color c = Color.white;
            c.a = (i == 0) ? 1f : 0f;
            img.color = c;
            bgImages[i] = img;
        }

        // Time-of-day label — above slider, bottom-center
        GameObject labelGo = CreateText(canvas, "TimeOfDayLabel", "Morning", 38, FontStyle.Bold);
        AnchorBottomCenter(labelGo, 500f, 60f, 110f);
        Text timeLabel = labelGo.GetComponent<Text>();

        // Slider — bottom-stretch
        GameObject sliderGo = CreateSliderGo(canvas, "TimeOfDaySlider");
        AnchorBottomStretch(sliderGo, 70f, 30f);
        Slider slider = sliderGo.GetComponent<Slider>();

        // Back button — top-left (rendered on top of backgrounds)
        GameObject backBtn = CreateButton(canvas, "BackButton", "< Back", new Color(0.15f, 0.15f, 0.4f));
        AnchorTopLeft(backBtn, 160f, 60f, 15f);
        UnityEventTools.AddStringPersistentListener(backBtn.GetComponent<Button>().onClick, sceneLoader.LoadSceneByName, "MainMenu");

        controller.backgroundImages = bgImages;
        controller.timeOfDaySlider   = slider;
        controller.timeOfDayLabel    = timeLabel;
        controller.labels            = timeLabels;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BackgroundBlend.unity");
    }

    // -------------------------------------------------------------------------
    // Build Settings
    // -------------------------------------------------------------------------

    private static void RegisterBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/TouchPop.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ScrollableText.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/BackgroundBlend.unity", true),
        };
    }

    // -------------------------------------------------------------------------
    // Scene-level helpers
    // -------------------------------------------------------------------------

    private static GameObject CreateCamera(Color background)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        go.AddComponent<AudioListener>();
        return go;
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject CreateCanvas()
    {
        GameObject go = new GameObject("Canvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // -------------------------------------------------------------------------
    // Widget factories (return GameObject, caller sets anchor via helpers below)
    // -------------------------------------------------------------------------

    private static GameObject CreateText(Transform parent, string name, string content,
        int fontSize = 30, FontStyle style = FontStyle.Normal,
        TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        Image img = go.AddComponent<Image>();
        img.color = color;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = color;
        cb.highlightedColor = color * 1.25f;
        cb.pressedColor     = color * 0.75f;
        btn.colors = cb;
        btn.targetGraphic = img;

        // Label child — fills the button
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        Text text = labelGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return go;
    }

    // -------------------------------------------------------------------------
    // Anchor helpers — call after CreateText / CreateButton to position them
    // -------------------------------------------------------------------------

    /// <summary>Top-left corner, fixed size.</summary>
    private static void AnchorTopLeft(GameObject go, float w, float h, float margin)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(margin, -margin);
    }

    /// <summary>Top-center, fixed size, offset down from top edge.</summary>
    private static void AnchorTopCenter(GameObject go, float w, float h, float marginTop)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, -marginTop);
    }

    /// <summary>Center of screen, offset by (dx, dy).</summary>
    private static void AnchorCenter(GameObject go, float w, float h, float dx, float dy)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(dx, dy);
    }

    /// <summary>Bottom-center, fixed size, offset up from bottom edge.</summary>
    private static void AnchorBottomCenter(GameObject go, float w, float h, float marginBottom)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, marginBottom);
    }

    /// <summary>Bottom edge, full width stretch, fixed height.</summary>
    private static void AnchorBottomStretch(GameObject go, float h, float margin)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, margin);
        rt.sizeDelta = new Vector2(-60f, h);  // 30px padding each side
    }

    /// <summary>Full stretch fill — covers the entire parent.</summary>
    private static void AnchorStretchFill(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // -------------------------------------------------------------------------
    // Popup panel for ScrollableText
    // -------------------------------------------------------------------------

    private static GameObject CreatePopupPanel(Transform canvas,
        out ScrollRect scrollRect, out Text contentText)
    {
        // Panel — full stretch with small inset
        GameObject panel = new GameObject("PopupPanel");
        panel.transform.SetParent(canvas, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(20f, 20f);
        panelRt.offsetMax = new Vector2(-20f, -20f);
        panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.97f);
        // Use CanvasGroup to hide/show so ContentSizeFitter can always measure while active.
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Close button — top-right of panel
        GameObject closeBtn = CreateButton(panel.transform, "CloseButton", "X", new Color(0.6f, 0.15f, 0.15f));
        RectTransform closeBtnRt = closeBtn.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = closeBtnRt.anchorMax = new Vector2(1f, 1f);
        closeBtnRt.pivot = new Vector2(1f, 1f);
        closeBtnRt.sizeDelta = new Vector2(80f, 80f);
        closeBtnRt.anchoredPosition = new Vector2(-10f, -10f);

        // Panel title — disable raycastTarget so it doesn't block the close button
        GameObject titleGo = CreateText(panel.transform, "PanelTitle", "Information", 34, FontStyle.Bold);
        titleGo.GetComponent<Text>().raycastTarget = false;
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 70f);
        titleRt.anchoredPosition = new Vector2(0f, -10f);

        // ScrollView — fills panel below title
        GameObject scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(panel.transform, false);
        RectTransform scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(10f, 10f);
        scrollRt.offsetMax = new Vector2(-10f, -90f);

        scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Viewport — leave room on right for scrollbar (40px)
        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = new Vector2(-40f, 0f);
        Image viewportImg = viewportGo.AddComponent<Image>();
        viewportImg.color = Color.white; // Must be opaque — Mask uses alpha to define clip area
        viewportImg.raycastTarget = true; // Must be true so ScrollRect receives mouse wheel + touch drag
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportRt;

        // Content — top-anchored, full width; height set at runtime by controller
        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 2000f);
        scrollRect.content = contentRt;

        // Content text — stretch-fill the content area
        GameObject textGo = new GameObject("ContentText");
        textGo.transform.SetParent(contentGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20f, 10f);
        textRt.offsetMax = new Vector2(-20f, -10f);
        contentText = textGo.AddComponent<Text>();
        contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        contentText.fontSize = 28;
        contentText.color = Color.white;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Overflow;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.raycastTarget = false;

        // Vertical scrollbar — anchored to right edge of scroll view
        GameObject scrollbarGo = new GameObject("Scrollbar");
        scrollbarGo.transform.SetParent(scrollGo.transform, false);
        RectTransform scrollbarRt = scrollbarGo.AddComponent<RectTransform>();
        scrollbarRt.anchorMin = new Vector2(1f, 0f);
        scrollbarRt.anchorMax = new Vector2(1f, 1f);
        scrollbarRt.pivot = new Vector2(1f, 1f);
        scrollbarRt.offsetMin = Vector2.zero;
        scrollbarRt.offsetMax = Vector2.zero;
        scrollbarRt.sizeDelta = new Vector2(40f, 0f);
        Image scrollbarBg = scrollbarGo.AddComponent<Image>();
        scrollbarBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        Scrollbar scrollbar = scrollbarGo.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Scrollbar handle
        GameObject handleArea = new GameObject("Sliding Area");
        handleArea.transform.SetParent(scrollbarGo.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(5f, 5f);
        handleAreaRt.offsetMax = new Vector2(-5f, -5f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = handleRt.offsetMax = Vector2.zero;
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.6f, 0.6f, 0.7f, 1f);
        scrollbar.handleRect = handleRt;
        scrollbar.targetGraphic = handleImg;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Ensure CloseButton is the last sibling so it renders on top and receives input first
        closeBtn.transform.SetAsLastSibling();

        return panel;
    }

    // -------------------------------------------------------------------------
    // Slider for BackgroundBlend
    // -------------------------------------------------------------------------

    private static GameObject CreateSliderGo(Transform parent, string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.AddComponent<RectTransform>();

        Slider slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.direction = Slider.Direction.LeftToRight;

        // Background track
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        slider.image = bgImg;

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(5f, 0f);
        faRt.offsetMax = new Vector2(-15f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.sizeDelta = new Vector2(10f, 0f);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.7f, 0.2f);
        slider.fillRect = fillRt;

        // Handle slide area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(root.transform, false);
        RectTransform haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10f, 0f);
        haRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.anchorMin = handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.sizeDelta = new Vector2(70f, 70f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        return root;
    }

    // -------------------------------------------------------------------------
    // Sample text
    // -------------------------------------------------------------------------

    private static string SampleLongText()
    {
        const string para =
            "Water is the foundation of all life on Earth, yet it is one of our most threatened " +
            "natural resources. Freshwater makes up only about 3% of the Earth's total water supply, " +
            "and of that, less than 1% is readily accessible for human use. As global populations " +
            "grow and climate patterns shift, the demand for clean water is increasing while " +
            "availability in many regions is declining. Glaciers that feed major river systems are " +
            "retreating, groundwater aquifers are being drawn down faster than they can recharge, " +
            "and droughts are becoming more frequent and severe in many parts of the world.\n\n" +
            "Conserving water at the individual level is one of the most impactful actions a person " +
            "can take. Simple changes — fixing leaky faucets, taking shorter showers, running " +
            "dishwashers and washing machines only when full, and choosing drought-resistant plants " +
            "in gardens — can collectively save millions of gallons each year. In agriculture, which " +
            "accounts for roughly 70% of global freshwater withdrawals, adopting drip irrigation, " +
            "crop rotation, and soil moisture monitoring can dramatically reduce waste while " +
            "maintaining or even improving yields.\n\n" +
            "Protecting watersheds and natural water systems is equally critical. Forests, wetlands, " +
            "and grasslands act as natural sponges, absorbing rainfall and slowly releasing it into " +
            "streams and aquifers. When these ecosystems are cleared or degraded, runoff increases, " +
            "flooding becomes more severe, and groundwater recharge rates fall. Investing in the " +
            "restoration of these landscapes is one of the most cost-effective strategies for " +
            "securing long-term water supplies for both people and wildlife.\n\n" +
            "Policy and technology also play essential roles. Smart metering systems help utilities " +
            "and consumers identify leaks and track usage in real time. Desalination and water " +
            "recycling technologies are becoming more efficient and affordable, expanding the range " +
            "of water sources available to communities under stress. Meanwhile, international " +
            "agreements and transboundary water management frameworks are essential for rivers and " +
            "aquifers that cross national borders, where cooperation or conflict over water can " +
            "shape geopolitical stability for decades to come.\n\n";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 6; i++)
            sb.Append(para);
        return sb.ToString().TrimEnd();
    }
}
