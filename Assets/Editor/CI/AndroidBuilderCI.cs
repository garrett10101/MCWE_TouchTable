using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI-friendly headless build script for Android APK.
/// Resolves SDK/NDK/JDK paths from environment variables (set by the CI runner
/// or Unity's bundled toolchain) so the script works without hardcoded paths.
///
/// Called via:  Unity -executeMethod AndroidBuilderCI.Build
///
/// Environment variables (all optional — falls back to Unity's bundled toolchain):
///   BUILD_OUTPUT_PATH  — full path to output APK  (default: /tmp/TouchTable.apk)
///   ANDROID_SDK_ROOT / ANDROID_HOME — Android SDK root
///   ANDROID_NDK_ROOT               — Android NDK root
///   JAVA_HOME                      — JDK root
/// </summary>
public static class AndroidBuilderCI
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/TouchPop.unity",
        "Assets/Scenes/ScrollableText.unity",
        "Assets/Scenes/BackgroundBlend.unity",
        "Assets/Scenes/Map.unity",
    };

    public static void Build()
    {
        // Prefer environment overrides; fall back to Unity's own bundled toolchain.
        string unityData = EditorApplication.applicationContentsPath;
        string bundledBase = $"{unityData}/PlaybackEngines/AndroidPlayer";

        string sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
                      ?? Environment.GetEnvironmentVariable("ANDROID_HOME")
                      ?? $"{bundledBase}/SDK";

        string ndkRoot = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT")
                      ?? $"{bundledBase}/NDK";

        string jdkRoot = Environment.GetEnvironmentVariable("JAVA_HOME")
                      ?? $"{bundledBase}/OpenJDK";

        string outputApk = Environment.GetEnvironmentVariable("BUILD_OUTPUT_PATH")
                        ?? "/tmp/TouchTable.apk";

        EditorPrefs.SetString("AndroidSdkRoot", sdkRoot);
        EditorPrefs.SetString("AndroidNdkRoot", ndkRoot);
        EditorPrefs.SetString("JdkPath",        jdkRoot);

        Debug.Log($"[AndroidBuilderCI] SDK={sdkRoot}  NDK={ndkRoot}  JDK={jdkRoot}");

        PlayerSettings.companyName                       = "TouchTable";
        PlayerSettings.productName                       = "TouchTable";
        PlayerSettings.applicationIdentifier             = "com.touchtable.demo";
        PlayerSettings.Android.minSdkVersion             = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion          = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures       = AndroidArchitecture.ARM64;

        var options = new BuildPlayerOptions
        {
            scenes           = Scenes,
            locationPathName = outputApk,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        Debug.Log($"[AndroidBuilderCI] Building → {outputApk}");
        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[AndroidBuilderCI] Build succeeded ({summary.totalSize / 1024 / 1024} MB)");
        else
        {
            Debug.LogError($"[AndroidBuilderCI] Build FAILED: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
