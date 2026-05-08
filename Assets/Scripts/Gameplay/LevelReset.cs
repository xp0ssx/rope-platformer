using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class LevelReset : MonoBehaviour
{
    [SerializeField] private Key resetKey = Key.R;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard[resetKey].wasPressedThisFrame)
        {
            ResetLevel();
        }
    }

    public void ResetLevel()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
