#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

/// <summary>
/// Build script for multi-platform deployment
/// </summary>
public static class Builder
{
    private static readonly string[] Scenes = new[] { "Assets/Scenes/MainScene.unity" };
    
    [MenuItem("Build/Build All Platforms")]
    public static void BuildAll()
    {
        BuildAndroid();
        BuildDesktop();
        BuildWebGL();
    }
    
    [MenuItem("Build/Build Android")]
    public static void BuildAndroid()
    {
        string path = "Builds/Android/V16BeaconTracker.apk";
        EnsureDirectory(Path.GetDirectoryName(path));
        
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(options);
        LogBuildResult(report, "Android");
    }
    
    [MenuItem("Build/Build Desktop (Linux)")]
    public static void BuildDesktop()
    {
        string path = "Builds/Desktop/V16BeaconTracker";
        EnsureDirectory(Path.GetDirectoryName(path));
        
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(options);
        LogBuildResult(report, "Desktop Linux");
    }
    
    [MenuItem("Build/Build Desktop (Windows)")]
    public static void BuildDesktopWindows()
    {
        string path = "Builds/Windows/V16BeaconTracker.exe";
        EnsureDirectory(Path.GetDirectoryName(path));
        
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(options);
        LogBuildResult(report, "Desktop Windows");
    }
    
    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        string path = "Builds/WebGL";
        EnsureDirectory(path);
        
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(options);
        LogBuildResult(report, "WebGL");
    }
    
    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    private static void LogBuildResult(BuildReport report, string platform)
    {
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"✓ {platform} build succeeded: {report.summary.outputPath}");
            Debug.Log($"  Size: {report.summary.totalSize / 1024 / 1024} MB");
            Debug.Log($"  Time: {report.summary.totalTime.TotalSeconds:F1} seconds");
        }
        else
        {
            Debug.LogError($"✗ {platform} build failed with {report.summary.totalErrors} errors");
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error)
                    {
                        Debug.LogError($"  {message.content}");
                    }
                }
            }
        }
    }
}
#endif
