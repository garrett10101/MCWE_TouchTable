using UnityEditor;

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
