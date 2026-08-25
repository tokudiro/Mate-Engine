using UnityEditor;

public static class CommandLineBuild
{
    public static void Build()
    {
        string[] scenes = { "Assets/MATE ENGINE - Scenes/Mate Engine Main.unity" };
        BuildPipeline.BuildPlayer(scenes, "Build/MateEngine.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
    }
}
