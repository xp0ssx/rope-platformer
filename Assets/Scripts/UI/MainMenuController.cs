using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string tutorialSceneName = "MainScene";
    [SerializeField] private string finalLevelSceneName = "Level2Scene";

    public void LoadTutorial()
    {
        LoadScene(tutorialSceneName);
    }

    public void LoadFinalLevel()
    {
        LoadScene(finalLevelSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
