using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple helper component for loading scenes from UI buttons.
/// Attach this script to a persistent GameObject (e.g. on your main menu canvas)
/// and configure the button OnClick events to call LoadSceneByName with the name
/// of the scene you want to load. A Quit method is provided for desktop testing
/// which will exit play mode or the application.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Loads a scene by its name. The scene must be added to the build settings.
    /// </summary>
    /// <param name="sceneName">The exact name of the scene to load.</param>
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Attempted to load a scene with an empty name.");
        }
    }

    /// <summary>
    /// Quits play mode (in the editor) or exits the application (on device).
    /// Useful for providing a quit button in desktop builds or during development.
    /// </summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}