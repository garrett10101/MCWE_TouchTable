#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace WatershedSim
{
    /// <summary>
    /// Editor helper: builds the full Watershed Simulation scene hierarchy with one click.
    ///
    /// Usage:
    ///   1. Open (or create) the "Watershed Simulation" scene in Unity.
    ///   2. Run  Tools → Watershed → Build Scene Hierarchy.
    ///   3. Review the GameObjects and Inspector values created.
    ///   4. Save the scene (Ctrl+S / Cmd+S).
    ///   5. Add the scene to File → Build Settings.
    ///
    /// What it creates:
    ///
    ///   WatershedRoot
    ///   ├─ Terrain           (Terrain + TerrainCollider + WatershedTerrain)
    ///   ├─ WaterMesh         (MeshFilter + MeshRenderer + WaterRenderer)
    ///   ├─ SimController     (WaterSimulation + WatershedInteraction)
    ///   ├─ CameraRig         (WatershedCamera)  [pivot at terrain centre]
    ///   │   └─ Main Camera   (Camera + AudioListener + PhysicsRaycaster)
    ///   └─ UICanvas          (Canvas + CanvasScaler + GraphicRaycaster + WatershedUI)
    ///
    /// After building, manually wire any remaining Inspector references and tweak values.
    /// </summary>
    public static class WatershedSceneSetup
    {
        [MenuItem("Tools/Watershed/Build Scene Hierarchy")]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Watershed Scene",
                    "This will add GameObjects to the current scene.\n\nContinue?",
                    "Build", "Cancel"))
                return;

            Undo.SetCurrentGroupName("Build Watershed Scene");
            int undoGroup = Undo.GetCurrentGroup();

            // ── Root ──────────────────────────────────────────────────────────
            var root = new GameObject("WatershedRoot");
            Undo.RegisterCreatedObjectUndo(root, "Create Root");

            // ── Terrain ───────────────────────────────────────────────────────
            var terrainGO = UnityEngine.Terrain.CreateTerrainGameObject(null);
            terrainGO.name = "Terrain";
            Undo.RegisterCreatedObjectUndo(terrainGO, "Create Terrain");
            terrainGO.transform.SetParent(root.transform, false);
            terrainGO.transform.position = Vector3.zero;

            var terrainData = new TerrainData();
            terrainData.heightmapResolution = 129;
            terrainData.size = new Vector3(500f, 80f, 500f);
            AssetDatabase.CreateAsset(terrainData, "Assets/Scenes/WatershedTerrainData.asset");

            var terrain = terrainGO.GetComponent<UnityEngine.Terrain>();
            terrain.terrainData = terrainData;
            var tc = terrainGO.GetComponent<TerrainCollider>();
            if (tc != null) tc.terrainData = terrainData;

            var wt = Undo.AddComponent<WatershedTerrain>(terrainGO);
            // TerrainLayer (visual) — user can assign in Inspector
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // ── WaterMesh ─────────────────────────────────────────────────────
            var waterMeshGO = new GameObject("WaterMesh");
            Undo.RegisterCreatedObjectUndo(waterMeshGO, "Create WaterMesh");
            waterMeshGO.transform.SetParent(root.transform, false);
            waterMeshGO.AddComponent<MeshFilter>();
            waterMeshGO.AddComponent<MeshRenderer>();
            var wr = Undo.AddComponent<WaterRenderer>(waterMeshGO);

            // ── SimController ─────────────────────────────────────────────────
            var simGO = new GameObject("SimController");
            Undo.RegisterCreatedObjectUndo(simGO, "Create SimController");
            simGO.transform.SetParent(root.transform, false);
            var ws  = Undo.AddComponent<WaterSimulation>(simGO);
            var wi  = Undo.AddComponent<WatershedInteraction>(simGO);

            // Wire terrain provider via serialized field
            var wsSerObj = new SerializedObject(ws);
            var tpProp   = wsSerObj.FindProperty("terrainProvider");
            if (tpProp != null) { tpProp.objectReferenceValue = wt; wsSerObj.ApplyModifiedProperties(); }

            // Wire WaterRenderer references
            var wrSerObj = new SerializedObject(wr);
            wrSerObj.FindProperty("waterSim")?.Let(p => p.objectReferenceValue = ws);
            var tpProp2 = wrSerObj.FindProperty("terrainProvider");
            if (tpProp2 != null) tpProp2.objectReferenceValue = wt;
            wrSerObj.ApplyModifiedProperties();

            // ── CameraRig ─────────────────────────────────────────────────────
            var rigGO = new GameObject("CameraRig");
            Undo.RegisterCreatedObjectUndo(rigGO, "Create CameraRig");
            rigGO.transform.SetParent(root.transform, false);
            // Pivot at terrain centre
            rigGO.transform.position = new Vector3(250f, 0f, 250f);
            var wc = Undo.AddComponent<WatershedCamera>(rigGO);
            wc.pivotPoint = new Vector3(250f, 0f, 250f);
            wc.minDistance = 80f;
            wc.maxDistance = 600f;

            var camGO = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGO, "Create Camera");
            camGO.transform.SetParent(rigGO.transform, false);
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.Skybox;
            cam.fieldOfView     = 60f;
            cam.nearClipPlane   = 0.5f;
            cam.farClipPlane    = 3000f;
            camGO.AddComponent<AudioListener>();

            // Physics raycaster so EventSystem works in 3D
            var pr = camGO.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            pr.eventMask = ~0;

            // URP camera data
            var urpData = camGO.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderType = CameraRenderType.Base;

            // Wire camera into WatershedCamera and WatershedInteraction
            wc.GetType()
              .GetField("_cam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
              ?.SetValue(wc, camGO.transform);

            wi.waterSim       = ws;
            wi.watershedCamera = wc;
            wi.mainCamera     = cam;

            // ── EventSystem ───────────────────────────────────────────────────
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
                esGO.transform.SetParent(root.transform, false);
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // ── UICanvas ──────────────────────────────────────────────────────
            var uiGO = new GameObject("UICanvas");
            Undo.RegisterCreatedObjectUndo(uiGO, "Create UICanvas");
            uiGO.transform.SetParent(root.transform, false);
            var ui = Undo.AddComponent<WatershedUI>(uiGO);
            ui.waterSim   = ws;
            ui.interaction = wi;

            // ── Directional Light ─────────────────────────────────────────────
            var lightGO = new GameObject("Sun");
            Undo.RegisterCreatedObjectUndo(lightGO, "Create Sun");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            light.color     = new Color(1f, 0.95f, 0.85f);
            light.shadows   = LightShadows.Soft;

            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            Debug.Log("[WatershedSceneSetup] Scene hierarchy built. " +
                      "Review Inspector fields, then save the scene and add it to Build Settings.");
        }

        [MenuItem("Tools/Watershed/Add Water Source at Selection")]
        static void AddWaterSource()
        {
            var sel = Selection.activeGameObject;
            if (sel == null) { Debug.LogWarning("Select a GameObject first."); return; }
            Undo.AddComponent<WaterSource>(sel);
        }

        [MenuItem("Tools/Watershed/Add Pollution Source at Selection")]
        static void AddPollutionSource()
        {
            var sel = Selection.activeGameObject;
            if (sel == null) { Debug.LogWarning("Select a GameObject first."); return; }
            Undo.AddComponent<PollutionSource>(sel);
        }
    }

    // Small helper so we can use ?.Let(…) in object chains
    static class SerializedPropertyExtensions
    {
        public static SerializedProperty Let(this SerializedProperty p, System.Action<SerializedProperty> fn)
        { fn(p); return p; }
    }
}
#endif
