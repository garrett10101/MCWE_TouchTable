using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless build script for PC Standalone (Windows / macOS / Linux).
/// Called via: Unity -executeMethod PCBuilder.Build
/// Output: /tmp/TouchTable-PC/ (Linux), adjust OutputPath for other platforms.
/// </summary>
public static class PCBuilder
{
    private const string OutputPath = "/tmp/TouchTable-PC/TouchTable";

    public static void Build()
    {
        PlayerSettings.companyName   = "TouchTable";
        PlayerSettings.productName   = "TouchTable";
        PlayerSettings.applicationIdentifier = "com.touchtable.demo";

        var options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/TouchPop.unity",
                "Assets/Scenes/ScrollableText.unity",
                "Assets/Scenes/BackgroundBlend.unity",
            },
            locationPathName = OutputPath,
            target           = BuildTarget.StandaloneLinux64,
            options          = BuildOptions.None,
        };

        Debug.Log("[PCBuilder] Starting Linux standalone build...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[PCBuilder] Build succeeded: {OutputPath} ({summary.totalSize / 1024 / 1024} MB)");
        else
        {
            Debug.LogError($"[PCBuilder] Build FAILED: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
