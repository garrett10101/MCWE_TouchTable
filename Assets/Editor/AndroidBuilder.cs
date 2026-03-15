using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless build script for Android APK.
/// Called via: Unity -executeMethod AndroidBuilder.Build
/// </summary>
public static class AndroidBuilder
{
    private const string AndroidSdkRoot = "/home/garrett/Unity/Hub/Editor/6000.3.11f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK";
    private const string AndroidNdkRoot = "/home/garrett/Unity/Hub/Editor/6000.3.11f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK";
    private const string JdkRoot       = "/home/garrett/Unity/Hub/Editor/6000.3.11f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK";
    private const string OutputApk     = "/tmp/TouchTable.apk";

    public static void Build()
    {
        // Point Unity at the bundled SDK/NDK/JDK
        EditorPrefs.SetString("AndroidSdkRoot", AndroidSdkRoot);
        EditorPrefs.SetString("AndroidNdkRoot", AndroidNdkRoot);
        EditorPrefs.SetString("JdkPath",        JdkRoot);

        // Basic player settings
        PlayerSettings.companyName               = "TouchTable";
        PlayerSettings.productName               = "TouchTable";
        PlayerSettings.applicationIdentifier     = "com.touchtable.demo";
        PlayerSettings.Android.minSdkVersion     = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion  = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        var options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/TouchPop.unity",
                "Assets/Scenes/ScrollableText.unity",
                "Assets/Scenes/BackgroundBlend.unity",
                "Assets/Scenes/Map.unity",
            },
            locationPathName = OutputApk,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        Debug.Log("[AndroidBuilder] Starting build...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AndroidBuilder] Build succeeded: {OutputApk} ({summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            Debug.LogError($"[AndroidBuilder] Build FAILED: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
