using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildWechatMinigame
{
    [MenuItem("Pocket City/Build WeChat Mini Game")]
    public static void Build()
    {
        string buildPath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "..",
            "miniprogram"
        );

        string[] scenes = {
            "Assets/Scenes/PocketCityPrototype.unity"
        };

        PlayerSettings.WebGL.memorySize = 256;
        PlayerSettings.WebGL.emscriptenArgs = "-s ASSERTIONS=0";
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.threadsSupport = false;

        var buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildOptions);
        Debug.Log("WebGL build completed: " + buildPath);
    }
}
