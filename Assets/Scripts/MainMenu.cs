using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private GameObject startButtonObject;
    [SerializeField] private GameObject quitButtonObject;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameScene";

    [Header("Optional")]
    [SerializeField] private GameObject firstSelected;

    private Button startButton;
    private Button quitButton;

    private void Awake()
    {
        if (startButtonObject != null)
            startButton = startButtonObject.GetComponent<Button>();

        if (quitButtonObject != null)
            quitButton = quitButtonObject.GetComponent<Button>();

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        if (firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed.");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}