using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f; // pause gameplay
        Cursor.lockState = CursorLockMode.None; // if you lock the cursor during play
        Cursor.visible = true;
    }

    public void Hide()
    {
        gameOverPanel.SetActive(false);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
