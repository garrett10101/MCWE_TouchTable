using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI-friendly headless build script for PC Standalone (Linux / Windows / macOS).
/// Reads configuration from environment variables so GitHub Actions can
/// control the output path and target platform without modifying source.
///
/// Called via:  Unity -executeMethod PCBuilderCI.Build
///
/// Environment variables:
///   BUILD_OUTPUT_PATH  — full path including filename (e.g. /github/workspace/Builds/Linux/TouchTable)
///   BUILD_TARGET       — one of: Linux64 (default), Windows64, macOS
/// </summary>
public static class PCBuilderCI
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
        string outputPath = Environment.GetEnvironmentVariable("BUILD_OUTPUT_PATH")
                         ?? "/tmp/TouchTable-PC/TouchTable";

        string targetStr = Environment.GetEnvironmentVariable("BUILD_TARGET") ?? "Linux64";
        BuildTarget target = targetStr switch
        {
            "Windows64" => BuildTarget.StandaloneWindows64,
            "macOS"     => BuildTarget.StandaloneOSX,
            _           => BuildTarget.StandaloneLinux64,
        };

        PlayerSettings.companyName           = "TouchTable";
        PlayerSettings.productName           = "TouchTable";
        PlayerSettings.applicationIdentifier = "com.touchtable.demo";

        var options = new BuildPlayerOptions
        {
            scenes           = Scenes,
            locationPathName = outputPath,
            target           = target,
            options          = BuildOptions.None,
        };

        Debug.Log($"[PCBuilderCI] Building {target} → {outputPath}");
        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[PCBuilderCI] Build succeeded ({summary.totalSize / 1024 / 1024} MB)");
        else
        {
            Debug.LogError($"[PCBuilderCI] Build FAILED: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
