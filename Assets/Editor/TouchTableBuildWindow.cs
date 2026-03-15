using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

/// <summary>
/// Interactive Unity Editor window for managing scene configuration and builds.
/// Open via: Tools → TouchTable → Build Manager
/// </summary>
public class TouchTableBuildWindow : EditorWindow
{
    // ── Tab state ────────────────────────────────────────────────────────────
    int _tab;          // 0=Scene Setup, 1=PC Build, 2=Mobile Build
    int _mobileSubTab; // 0=Android, 1=iOS

    // ── Scene Setup ──────────────────────────────────────────────────────────
    struct SceneEntry
    {
        public string path;
        public bool   enabled;
        public bool   existsOnDisk;
    }
    List<SceneEntry> _scenes     = new();
    bool             _scenesDirty;

    // ── PC Build ─────────────────────────────────────────────────────────────
    int    _pcPlatform;   // 0=Linux, 1=Windows, 2=macOS
    string _pcOutputPath;

    static readonly string[] PcPlatformNames  = { "Linux x64", "Windows x64", "macOS" };
    static readonly BuildTarget[] PcTargets   = { BuildTarget.StandaloneLinux64, BuildTarget.StandaloneWindows64, BuildTarget.StandaloneOSX };
    static readonly string[] PcExecutables    = { "TouchTable", "TouchTable.exe", "TouchTable.app" };

    // ── Mobile Build ─────────────────────────────────────────────────────────
    string       _mobileOutputPath;
    string       _iosOutputPath;
    List<string> _adbDevices      = new();
    int          _selectedDevice;
    string       _adbPath;
    bool         _adbAvailable;
    bool         _iosBuiltSuccessfully;

    // ── Status bar ───────────────────────────────────────────────────────────
    string _statusMessage;
    bool   _statusIsError;

    // ── EditorPrefs keys ─────────────────────────────────────────────────────
    const string PrefPCOutput      = "TouchTable.PCOutputPath";
    const string PrefPCPlatform    = "TouchTable.PCPlatform";
    const string PrefAndroidOutput = "TouchTable.AndroidOutputPath";
    const string PrefIOSOutput     = "TouchTable.IOSOutputPath";
    const string PrefMobileSubTab  = "TouchTable.MobileSubTab";

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/TouchTable/Build Manager")]
    public static void ShowWindow()
    {
        var win = GetWindow<TouchTableBuildWindow>("Build Manager");
        win.minSize = new Vector2(420, 320);
    }

    void OnEnable()
    {
        _pcPlatform        = EditorPrefs.GetInt(PrefPCPlatform, 0);
        _pcOutputPath      = EditorPrefs.GetString(PrefPCOutput, "/tmp/TouchTable-PC");
        _mobileOutputPath  = EditorPrefs.GetString(PrefAndroidOutput, "/tmp/TouchTable.apk");
        _iosOutputPath     = EditorPrefs.GetString(PrefIOSOutput, "/tmp/TouchTable-iOS");
        _mobileSubTab      = EditorPrefs.GetInt(PrefMobileSubTab, 0);

        ResolveAdbPath();
        ScanScenes();
    }

    void OnGUI()
    {
        _tab = GUILayout.Toolbar(_tab, new[] { "Scene Setup", "PC Build", "Mobile Build" });
        EditorGUILayout.Space(4);

        switch (_tab)
        {
            case 0: DrawSceneSetupTab(); break;
            case 1: DrawPCBuildTab();    break;
            case 2: DrawMobileBuildTab(); break;
        }

        // ── Status bar ───────────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMessage,
                _statusIsError ? MessageType.Error : MessageType.Info);
        }
    }

    // =========================================================================
    // Tab 0 — Scene Setup
    // =========================================================================

    void ScanScenes()
    {
        // Scenes already registered in EditorBuildSettings (preserves order)
        var buildSettingsMap = new Dictionary<string, bool>();
        foreach (var s in EditorBuildSettings.scenes)
            buildSettingsMap[s.path] = s.enabled;

        _scenes.Clear();

        // First: scenes already in EditorBuildSettings (in their current order)
        foreach (var kvp in buildSettingsMap)
        {
            _scenes.Add(new SceneEntry
            {
                path        = kvp.Key,
                enabled     = kvp.Value,
                existsOnDisk = File.Exists(kvp.Key),
            });
        }

        // Then: any newly discovered scenes not yet registered
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!buildSettingsMap.ContainsKey(path))
            {
                _scenes.Add(new SceneEntry
                {
                    path         = path,
                    enabled      = false,
                    existsOnDisk = File.Exists(path),
                });
            }
        }

        _scenesDirty = false;
    }

    void DrawSceneSetupTab()
    {
        // Header row
        EditorGUILayout.BeginHorizontal();
        int enabledCount = _scenes.Count(s => s.enabled);
        EditorGUILayout.LabelField($"{enabledCount} of {_scenes.Count} scenes enabled", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            ScanScenes();
            ShowStatus("Scenes refreshed.", false);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Scene rows
        for (int i = 0; i < _scenes.Count; i++)
        {
            var entry = _scenes[i];
            EditorGUILayout.BeginHorizontal();

            bool newEnabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(20));
            if (newEnabled != entry.enabled)
            {
                entry.enabled = newEnabled;
                _scenes[i]    = entry;
                _scenesDirty  = true;
            }

            EditorGUILayout.LabelField(Path.GetFileName(entry.path));

            if (!entry.existsOnDisk)
            {
                var style = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.9f, 0.75f, 0f) } };
                EditorGUILayout.LabelField("File not found", style, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledGroupScope(!_scenesDirty))
        {
            if (GUILayout.Button("Apply to Build Settings"))
            {
                EditorBuildSettings.scenes = _scenes
                    .Select(s => new EditorBuildSettingsScene(s.path, s.enabled))
                    .ToArray();
                _scenesDirty = false;
                ShowStatus("Build Settings updated.", false);
            }
        }
    }

    // =========================================================================
    // Tab 1 — PC Build
    // =========================================================================

    void DrawPCBuildTab()
    {
        // Platform popup
        EditorGUI.BeginChangeCheck();
        _pcPlatform = EditorGUILayout.Popup("Platform", _pcPlatform, PcPlatformNames);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetInt(PrefPCPlatform, _pcPlatform);

        // Output folder
        DrawBrowseFolder("Output Folder", ref _pcOutputPath);
        EditorPrefs.SetString(PrefPCOutput, _pcOutputPath);

        EditorGUILayout.Space(6);

        // Platform-switch warning
        BuildTarget selectedTarget = PcTargets[_pcPlatform];
        if (EditorUserBuildSettings.activeBuildTarget != selectedTarget)
        {
            EditorGUILayout.HelpBox(
                $"Active build target is {EditorUserBuildSettings.activeBuildTarget}. " +
                $"Unity will switch to {selectedTarget} before building — this may take a moment.",
                MessageType.Warning);
        }

        // Build-support check
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, selectedTarget))
        {
            EditorGUILayout.HelpBox(
                $"Build support module for {PcPlatformNames[_pcPlatform]} is not installed. " +
                "Install it via Unity Hub → Installs → Add Modules.",
                MessageType.Error);
        }

        // 0-scenes warning
        if (GetEnabledScenePaths().Length == 0)
            EditorGUILayout.HelpBox("No scenes are enabled in Build Settings. Builds will fail.", MessageType.Warning);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Build PC"))
            BuildPC();
    }

    void BuildPC()
    {
        string[] scenes = GetEnabledScenePaths();
        if (scenes.Length == 0)
        {
            ShowStatus("No enabled scenes — add scenes in the Scene Setup tab first.", true);
            return;
        }

        BuildTarget target = PcTargets[_pcPlatform];
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
        {
            ShowStatus($"Build support for {PcPlatformNames[_pcPlatform]} is not installed.", true);
            return;
        }

        string outPath = Path.Combine(_pcOutputPath, PcExecutables[_pcPlatform]);

        try
        {
            EditorUtility.DisplayProgressBar("Build Manager", $"Building {PcPlatformNames[_pcPlatform]}…", 0.1f);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = outPath,
                target           = target,
                options          = BuildOptions.None,
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                long mb = (long)(report.summary.totalSize / 1024 / 1024);
                ShowStatus($"Build succeeded: {outPath}  ({mb} MB)", false);
            }
            else
            {
                ShowStatus($"Build failed: {report.summary.result}", true);
            }
        }
        catch (System.Exception ex)
        {
            ShowStatus(ex.Message, true);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // =========================================================================
    // Tab 2 — Mobile Build
    // =========================================================================

    void DrawMobileBuildTab()
    {
        EditorGUI.BeginChangeCheck();
        _mobileSubTab = GUILayout.Toolbar(_mobileSubTab, new[] { "Android", "iOS" });
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetInt(PrefMobileSubTab, _mobileSubTab);

        EditorGUILayout.Space(4);

        if (_mobileSubTab == 0)
            DrawAndroidSubTab();
        else
            DrawIOSSubTab();
    }

    // ── Android ──────────────────────────────────────────────────────────────

    void DrawAndroidSubTab()
    {
        // ADB not found
        if (!_adbAvailable)
        {
            EditorGUILayout.HelpBox(
                "adb not found. Install Android SDK platform-tools and ensure adb is on PATH, " +
                "or install Android Build Support in Unity Hub.",
                MessageType.Warning);
        }

        // Device list
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Device", GUILayout.Width(60));
        if (_adbDevices.Count == 0)
        {
            using (new EditorGUI.DisabledGroupScope(true))
                EditorGUILayout.Popup(0, new[] { "No devices found" });
        }
        else
        {
            _selectedDevice = EditorGUILayout.Popup(_selectedDevice, _adbDevices.ToArray());
        }

        if (GUILayout.Button("Refresh Devices", GUILayout.Width(115)))
            RefreshAdbDevices();
        EditorGUILayout.EndHorizontal();

        // Output path
        DrawBrowseFile("Output APK", ref _mobileOutputPath, "apk");
        EditorPrefs.SetString(PrefAndroidOutput, _mobileOutputPath);

        EditorGUILayout.Space(4);

        // Build-support check
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorGUILayout.HelpBox(
                "Android Build Support module is not installed. Install it via Unity Hub → Installs → Add Modules.",
                MessageType.Error);
        }

        if (GetEnabledScenePaths().Length == 0)
            EditorGUILayout.HelpBox("No scenes are enabled in Build Settings. Builds will fail.", MessageType.Warning);

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build APK"))
            BuildAndroid(false);

        using (new EditorGUI.DisabledGroupScope(!_adbAvailable || _adbDevices.Count == 0))
        {
            if (GUILayout.Button("Build & Install"))
                BuildAndroid(true);
        }
        EditorGUILayout.EndHorizontal();
    }

    void ResolveAdbPath()
    {
        // 1. Try adb on PATH
        string fromPath = RunProcess(
            Application.platform == RuntimePlatform.WindowsEditor ? "where" : "which",
            "adb").Trim();
        if (!string.IsNullOrEmpty(fromPath) && File.Exists(fromPath.Split('\n')[0].Trim()))
        {
            _adbPath      = fromPath.Split('\n')[0].Trim();
            _adbAvailable = true;
            return;
        }

        // 2. EditorPrefs AndroidSdkRoot
        string sdkRoot = EditorPrefs.GetString("AndroidSdkRoot", "");
        if (!string.IsNullOrEmpty(sdkRoot))
        {
            string candidate = Path.Combine(sdkRoot, "platform-tools",
                Application.platform == RuntimePlatform.WindowsEditor ? "adb.exe" : "adb");
            if (File.Exists(candidate))
            {
                _adbPath      = candidate;
                _adbAvailable = true;
                return;
            }
        }

        // 3. Hardcoded fallback (Linux Unity installation)
        const string fallback = "/home/garrett/Unity/Hub/Editor/6000.3.11f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb";
        if (File.Exists(fallback))
        {
            _adbPath      = fallback;
            _adbAvailable = true;
            return;
        }

        _adbPath      = "adb"; // keep as last-ditch attempt
        _adbAvailable = false;
    }

    void RefreshAdbDevices()
    {
        _adbDevices.Clear();
        _selectedDevice = 0;

        if (!_adbAvailable)
        {
            ResolveAdbPath();
            if (!_adbAvailable)
            {
                ShowStatus("adb not found — cannot list devices.", true);
                return;
            }
        }

        string output = RunProcess(_adbPath, "devices");
        int skipped   = 0;

        foreach (string line in output.Split('\n').Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Contains("\tunauthorized") || line.Contains("\toffline"))
            {
                skipped++;
                continue;
            }
            if (line.Contains("\tdevice"))
                _adbDevices.Add(line.Split('\t')[0].Trim());
        }

        string msg = $"{_adbDevices.Count} device(s) found.";
        if (skipped > 0) msg += $" {skipped} unauthorized/offline device(s) skipped.";
        ShowStatus(msg, false);
        Repaint();
    }

    void BuildAndroid(bool installAfter)
    {
        string[] scenes = GetEnabledScenePaths();
        if (scenes.Length == 0)
        {
            ShowStatus("No enabled scenes — add scenes in the Scene Setup tab first.", true);
            return;
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            ShowStatus("Android Build Support module is not installed.", true);
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Build Manager", "Building Android APK…", 0.1f);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = _mobileOutputPath,
                target           = BuildTarget.Android,
                options          = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                ShowStatus($"Android build failed: {report.summary.result}", true);
                return;
            }

            long mb = (long)(report.summary.totalSize / 1024 / 1024);

            if (installAfter && _adbDevices.Count > 0)
            {
                EditorUtility.DisplayProgressBar("Build Manager", "Installing APK via adb…", 0.9f);
                RunAdbInstall(_adbDevices[_selectedDevice], _mobileOutputPath);
            }
            else
            {
                ShowStatus($"Build succeeded: {_mobileOutputPath}  ({mb} MB)", false);
            }
        }
        catch (System.Exception ex)
        {
            ShowStatus(ex.Message, true);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    void RunAdbInstall(string serial, string apkPath)
    {
        string output = RunProcess(_adbPath, $"-s {serial} install -r \"{apkPath}\"");
        if (output.Contains("Success"))
            ShowStatus($"APK installed successfully on {serial}.", false);
        else
            ShowStatus($"Install may have failed. adb output:\n{output}", true);
    }

    // ── iOS ──────────────────────────────────────────────────────────────────

    void DrawIOSSubTab()
    {
#if !UNITY_EDITOR_OSX
        EditorGUILayout.HelpBox("iOS builds require macOS. This button is disabled on non-macOS hosts.", MessageType.Warning);
#endif

        DrawBrowseFolder("Output Folder", ref _iosOutputPath);
        EditorPrefs.SetString(PrefIOSOutput, _iosOutputPath);

        EditorGUILayout.Space(4);

        if (GetEnabledScenePaths().Length == 0)
            EditorGUILayout.HelpBox("No scenes are enabled in Build Settings. Builds will fail.", MessageType.Warning);

        EditorGUILayout.Space(4);

#if UNITY_EDITOR_OSX
        if (GUILayout.Button("Build (Xcode Project)"))
            BuildIOS();
#else
        using (new EditorGUI.DisabledGroupScope(true))
            GUILayout.Button("Build (Xcode Project) — macOS only");
#endif

        if (_iosBuiltSuccessfully && Directory.Exists(_iosOutputPath))
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Open in Finder"))
                EditorUtility.RevealInFinder(_iosOutputPath);
        }
    }

    void BuildIOS()
    {
        string[] scenes = GetEnabledScenePaths();
        if (scenes.Length == 0)
        {
            ShowStatus("No enabled scenes — add scenes in the Scene Setup tab first.", true);
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Build Manager", "Building iOS Xcode project…", 0.1f);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = _iosOutputPath,
                target           = BuildTarget.iOS,
                options          = BuildOptions.None,
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                _iosBuiltSuccessfully = true;
                ShowStatus($"iOS Xcode project built at: {_iosOutputPath}", false);
            }
            else
            {
                _iosBuiltSuccessfully = false;
                ShowStatus($"iOS build failed: {report.summary.result}", true);
            }
        }
        catch (System.Exception ex)
        {
            _iosBuiltSuccessfully = false;
            ShowStatus(ex.Message, true);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // =========================================================================
    // Shared Helpers
    // =========================================================================

    string[] GetEnabledScenePaths() =>
        EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

    string RunProcess(string fileName, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = fileName,
                Arguments              = args,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            using var p = Process.Start(psi);
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return stdout + stderr;
        }
        catch
        {
            return string.Empty;
        }
    }

    void ShowStatus(string msg, bool isError)
    {
        _statusMessage = msg;
        _statusIsError = isError;
        Repaint();
    }

    void DrawBrowseFolder(string label, ref string path)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(90));
        path = EditorGUILayout.TextField(path);
        if (GUILayout.Button("Browse…", GUILayout.Width(70)))
        {
            string chosen = EditorUtility.SaveFolderPanel("Select Folder", path, "");
            if (!string.IsNullOrEmpty(chosen))
                path = chosen;
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawBrowseFile(string label, ref string path, string extension)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(90));
        path = EditorGUILayout.TextField(path);
        if (GUILayout.Button("Browse…", GUILayout.Width(70)))
        {
            string dir      = string.IsNullOrEmpty(path) ? "" : Path.GetDirectoryName(path);
            string fileName = string.IsNullOrEmpty(path) ? "TouchTable" : Path.GetFileNameWithoutExtension(path);
            string chosen   = EditorUtility.SaveFilePanel("Select File", dir, fileName, extension);
            if (!string.IsNullOrEmpty(chosen))
                path = chosen;
        }
        EditorGUILayout.EndHorizontal();
    }
}
