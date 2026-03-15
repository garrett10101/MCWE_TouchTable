using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utilities for Map.unity:
///   "Setup Map Popup"  — removes legacy popup objects.
///   "Add Back Button"  — adds a Back button to the Canvas that loads MainMenu.
///
/// Menu: Tools → TouchTable → Setup Map Popup / Add Back Button
/// </summary>
public static class MapPopupBuilder
{
    private const string ScenePath = "Assets/Scenes/Map.unity";

    [MenuItem("Tools/TouchTable/Setup Map Popup")]
    public static void SetupMapPopup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        bool changed = false;

        var panel = GameObject.Find("PopupPanel");
        if (panel != null) { Object.DestroyImmediate(panel); changed = true; }

        var manager = GameObject.Find("PopupManager");
        if (manager != null) { Object.DestroyImmediate(manager); changed = true; }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MapPopupBuilder] Removed legacy popup objects. Scene saved.");
        }
        else
        {
            Debug.Log("[MapPopupBuilder] No legacy popup objects found — scene is clean.");
        }
    }

    // -------------------------------------------------------------------------

    [MenuItem("Tools/TouchTable/Add Back Button")]
    public static void AddBackButton()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Remove any existing Back button so re-running is idempotent.
        var existing = GameObject.Find("BackButton");
        if (existing != null)
            Object.DestroyImmediate(existing);

        // Find the Canvas.
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("[MapPopupBuilder] No GameObject named 'Canvas' found in Map.unity.");
            return;
        }

        // Ensure a SceneLoader exists on the Canvas.
        var loader = canvasGO.GetComponent<SceneLoader>();
        if (loader == null)
            loader = canvasGO.AddComponent<SceneLoader>();

        // ── Button GameObject ──────────────────────────────────────────────
        var btnGO = new GameObject("BackButton");
        btnGO.layer = LayerMask.NameToLayer("UI");
        btnGO.transform.SetParent(canvasGO.transform, false);

        // RectTransform — top-left corner, 160×60, 20 px from each edge.
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta        = new Vector2(160f, 60f);

        // Background Image.
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        // Button component + onClick wired to SceneLoader.LoadSceneByName.
        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        colors.highlightedColor = new Color(0.30f, 0.30f, 0.30f, 0.95f);
        colors.pressedColor     = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        btn.colors = colors;

        UnityEventTools.AddStringPersistentListener(
            btn.onClick,
            loader.LoadSceneByName,
            "MainMenu");

        // ── Label ──────────────────────────────────────────────────────────
        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        labelGO.transform.SetParent(btnGO.transform, false);

        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin        = Vector2.zero;
        labelRT.anchorMax        = Vector2.one;
        labelRT.offsetMin        = Vector2.zero;
        labelRT.offsetMax        = Vector2.zero;

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "◀  Back";
        tmp.fontSize  = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        // ──────────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MapPopupBuilder] Back button added to Canvas and scene saved.");
    }
}
