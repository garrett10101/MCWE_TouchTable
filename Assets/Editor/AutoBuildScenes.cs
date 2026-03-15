using UnityEditor;

/// <summary>
/// Runs SceneBuilder.BuildAllScenes once on the next Unity startup, then deletes itself.
/// </summary>
[InitializeOnLoad]
public static class AutoBuildScenes
{
    static AutoBuildScenes()
    {
        if (!SessionState.GetBool("AutoBuildDone", false))
        {
            SessionState.SetBool("AutoBuildDone", true);
            EditorApplication.delayCall += () =>
            {
                SceneBuilder.BuildAllScenes();
                AssetDatabase.DeleteAsset("Assets/Editor/AutoBuildScenes.cs");
            };
        }
    }
}
