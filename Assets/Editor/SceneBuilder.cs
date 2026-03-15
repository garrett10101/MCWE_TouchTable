using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        // SceneLoader lives on the canvas root
        SceneLoader sceneLoader = canvasGo.AddComponent<SceneLoader>();

        // Title
        CreateText(canvas, "Title", "Touch Table Demo",
            anchoredPos: new Vector2(0f, 650f),
            sizeDelta: new Vector2(900f, 120f),
            fontSize: 52,
            style: FontStyle.Bold);

        // TouchPop button
        GameObject btn1 = CreateButton(canvas, "TouchPopButton", "Touch Pop (Whack-a-Mole)",
            anchoredPos: new Vector2(0f, 400f),
            sizeDelta: new Vector2(700f, 110f));
        UnityEventTools.AddStringPersistentListener(
            btn1.GetComponent<Button>().onClick,
            sceneLoader.LoadSceneByName,
            "TouchPop");

        // ScrollableText button
        GameObject btn2 = CreateButton(canvas, "ScrollableTextButton", "Scrollable Text",
            anchoredPos: new Vector2(0f, 240f),
            sizeDelta: new Vector2(700f, 110f));
        UnityEventTools.AddStringPersistentListener(
            btn2.GetComponent<Button>().onClick,
            sceneLoader.LoadSceneByName,
            "ScrollableText");

        // BackgroundBlend button
        GameObject btn3 = CreateButton(canvas, "BackgroundBlendButton", "Background Blend",
            anchoredPos: new Vector2(0f, 80f),
            sizeDelta: new Vector2(700f, 110f));
        UnityEventTools.AddStringPersistentListener(
            btn3.GetComponent<Button>().onClick,
            sceneLoader.LoadSceneByName,
            "BackgroundBlend");

        // Quit button
        GameObject quitBtn = CreateButton(canvas, "QuitButton", "Quit",
            anchoredPos: new Vector2(0f, -100f),
            sizeDelta: new Vector2(400f, 90f),
            normalColor: new Color(0.7f, 0.2f, 0.2f));
        UnityEventTools.AddVoidPersistentListener(
            quitBtn.GetComponent<Button>().onClick,
            sceneLoader.Quit);

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

        // Score label — top center
        GameObject scoreTextGo = CreateText(canvas, "ScoreText", "Score: 0",
            anchoredPos: new Vector2(0f, 870f),
            sizeDelta: new Vector2(600f, 80f),
            fontSize: 36,
            style: FontStyle.Bold);
        Text scoreText = scoreTextGo.GetComponent<Text>();

        // Back button — top left corner
        GameObject backBtn = CreateButton(canvas, "BackButton", "< Back",
            anchoredPos: new Vector2(-440f, 870f),
            sizeDelta: new Vector2(180f, 70f),
            normalColor: new Color(0.2f, 0.2f, 0.5f));
        UnityEventTools.AddStringPersistentListener(
            backBtn.GetComponent<Button>().onClick,
            sceneLoader.LoadSceneByName,
            "MainMenu");

        // TouchPopManager
        GameObject managerGo = new GameObject("TouchPopManager");
        TouchPopManager manager = managerGo.AddComponent<TouchPopManager>();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Target.prefab");
        manager.targetPrefab = prefab;
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

        // Header label
        CreateText(canvas, "Header", "Scrollable Text Demo",
            anchoredPos: new Vector2(0f, 800f),
            sizeDelta: new Vector2(800f, 100f),
            fontSize: 42,
            style: FontStyle.Bold);

        // Back button
        GameObject backBtn = CreateButton(canvas, "BackButton", "< Back",
            anchoredPos: new Vector2(-440f, 870f),
            sizeDelta: new Vector2(180f, 70f),
            normalColor: new Color(0.2f, 0.2f, 0.5f));
        UnityEventTools.AddStringPersistentListener(
            backBtn.GetComponent<Button>().onClick,
            sceneLoader.LoadSceneByName,
            "MainMenu");

        // Open Popup button
        GameObject openBtn = CreateButton(canvas, "OpenButton", "Open Info Panel",
            anchoredPos: new Vector2(0f, 600f),
            sizeDelta: new Vector2(600f, 100f));
        UnityEventTools.AddVoidPersistentListener(
            openBtn.GetComponent<Button>().onClick,
            controller.OpenPopup);

        // Popup Panel (full-screen overlay, starts hidden)
        GameObject popupPanel = CreatePopupPanel(canvas, out ScrollRect scrollRect, out Text contentText);

        // Close button (wired inside CreatePopupPanel, but we need the controller ref)
        Button closeBtn = popupPanel.transform.Find("CloseButton").GetComponent<Button>();
        UnityEventTools.AddVoidPersistentListener(closeBtn.onClick, controller.ClosePopup);

        // Assign ScrollableTextController fields
        controller.popupPanel = popupPanel;
        controller.scrollRect = scrollRect;
        controller.contentText = contentText;
        controller.longText = SampleLongText();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ScrollableText.unity");
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
    // Helpers: Scene Objects
    // -------------------------------------------------------------------------

    private static GameObject CreateCamera(Color background)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        go.AddComponent<AudioListener>();
        return go;
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
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
    // Helpers: UI Widgets
    // -------------------------------------------------------------------------

    private static GameObject CreateText(Transform parent, string name, string content,
        Vector2 anchoredPos, Vector2 sizeDelta, int fontSize = 30,
        FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 sizeDelta, Color? normalColor = null)
    {
        // Button root
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = go.AddComponent<Image>();
        img.color = normalColor ?? new Color(0.2f, 0.5f, 0.8f);

        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor ?? new Color(0.2f, 0.5f, 0.8f);
        colors.highlightedColor = (normalColor ?? new Color(0.2f, 0.5f, 0.8f)) * 1.2f;
        colors.pressedColor = (normalColor ?? new Color(0.2f, 0.5f, 0.8f)) * 0.8f;
        btn.colors = colors;
        btn.targetGraphic = img;

        // Label child
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);

        RectTransform lrt = labelGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        Text text = labelGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = Mathf.RoundToInt(sizeDelta.y * 0.38f);
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return go;
    }

    /// <summary>
    /// Creates the full popup panel hierarchy with ScrollRect and content text.
    /// Returns the panel root; scrollRect and contentText are set as out params.
    /// </summary>
    private static GameObject CreatePopupPanel(Transform canvas,
        out ScrollRect scrollRect, out Text contentText)
    {
        // Panel root — full screen
        GameObject panel = new GameObject("PopupPanel");
        panel.transform.SetParent(canvas, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(40f, 40f);
        panelRt.offsetMax = new Vector2(-40f, -40f);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.1f, 0.97f);

        // Close button — top right of panel
        GameObject closeBtn = CreateButton(panel.transform, "CloseButton", "X",
            anchoredPos: new Vector2(0f, 0f),
            sizeDelta: new Vector2(90f, 90f),
            normalColor: new Color(0.6f, 0.15f, 0.15f));
        RectTransform closeBtnRt = closeBtn.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = closeBtnRt.anchorMax = new Vector2(1f, 1f);
        closeBtnRt.pivot = new Vector2(1f, 1f);
        closeBtnRt.anchoredPosition = new Vector2(-10f, -10f);

        // Panel title
        GameObject titleGo = new GameObject("PanelTitle");
        titleGo.transform.SetParent(panel.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -10f);
        titleRt.sizeDelta = new Vector2(0f, 80f);
        Text titleText = titleGo.AddComponent<Text>();
        titleText.text = "Information";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // ScrollView
        GameObject scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(panel.transform, false);
        RectTransform scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(10f, 10f);
        scrollRt.offsetMax = new Vector2(-10f, -100f);   // leave room for title

        scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Viewport
        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;

        scrollRect.viewport = viewportRt;

        // Content
        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 10, 10);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        scrollRect.content = contentRt;

        // Content Text
        GameObject textGo = new GameObject("ContentText");
        textGo.transform.SetParent(contentGo.transform, false);
        contentText = textGo.AddComponent<Text>();
        contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        contentText.fontSize = 28;
        contentText.color = Color.white;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Overflow;
        contentText.alignment = TextAnchor.UpperLeft;

        LayoutElement le = textGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        return panel;
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

        // 4 background images — stacked full-screen, drawn in order (Morning at bottom)
        string[] spriteNames = { "Background_Morning", "Background_Afternoon", "Background_Evening", "Background_Night" };
        string[] timeLabels  = { "Morning", "Afternoon", "Evening", "Night" };
        Image[] bgImages = new Image[spriteNames.Length];

        for (int i = 0; i < spriteNames.Length; i++)
        {
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{spriteNames[i]}.png");

            GameObject bgGo = new GameObject(spriteNames[i]);
            bgGo.transform.SetParent(canvas, false);

            RectTransform rt = bgGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            Image img = bgGo.AddComponent<Image>();
            img.preserveAspect = false;
            if (spr != null)
                img.sprite = spr;

            // All start fully transparent except Morning (index 0)
            Color c = Color.white;
            c.a = (i == 0) ? 1f : 0f;
            img.color = c;

            bgImages[i] = img;
        }

        // Time-of-day label — top center, above the slider
        GameObject labelGo = CreateText(canvas, "TimeOfDayLabel", "Morning",
            anchoredPos: new Vector2(0f, -750f),
            sizeDelta: new Vector2(600f, 80f),
            fontSize: 40,
            style: FontStyle.Bold);
        Text timeLabel = labelGo.GetComponent<Text>();

        // Horizontal slider — near bottom of screen
        Slider slider = CreateSlider(canvas, "TimeOfDaySlider",
            anchoredPos: new Vector2(0f, -860f),
            sizeDelta: new Vector2(900f, 80f));

        // Back button — top left
        GameObject backBtn = CreateButton(canvas, "BackButton", "< Back",
            anchoredPos: new Vector2(-440f, 870f),
            sizeDelta: new Vector2(180f, 70f),
            normalColor: new Color(0.2f, 0.2f, 0.5f));
        UnityEventTools.AddStringPersistentListener(
            backBtn.GetComponent<Button>().onClick,
            sceneLoader.LoadSceneByName,
            "MainMenu");

        // Wire controller
        controller.backgroundImages = bgImages;
        controller.timeOfDaySlider   = slider;
        controller.timeOfDayLabel    = timeLabel;
        controller.labels            = timeLabels;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BackgroundBlend.unity");
    }

    // -------------------------------------------------------------------------
    // Helper: Slider
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a horizontal UI Slider with Background, Fill, and Handle children.
    /// </summary>
    private static Slider CreateSlider(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        // Root
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = anchoredPos;
        rootRt.sizeDelta = sizeDelta;

        Slider slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.direction = Slider.Direction.LeftToRight;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        slider.image = bgImg;

        // Fill Area
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

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(root.transform, false);
        RectTransform haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = new Vector2(0f, 0f);
        haRt.anchorMax = new Vector2(1f, 1f);
        haRt.offsetMin = new Vector2(10f, 0f);
        haRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.anchorMin = handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.sizeDelta = new Vector2(60f, 60f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        return slider;
    }

    // -------------------------------------------------------------------------
    // Sample text for ScrollableText scene
    // -------------------------------------------------------------------------

    private static string SampleLongText()
    {
        const string para =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor " +
            "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
            "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
            "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt " +
            "mollit anim id est laborum.\n\n";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 12; i++)
            sb.Append(para);
        return sb.ToString().TrimEnd();
    }
}
