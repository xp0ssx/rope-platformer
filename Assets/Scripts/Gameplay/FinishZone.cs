using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class FinishZone : MonoBehaviour
{
    [SerializeField] private string playerObjectName = "Player";
    [SerializeField] private MonoBehaviour[] disableOnComplete;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private float loadDelay = 1.2f;
    [SerializeField] private string completionTitle = "Level complete";
    [SerializeField] private string completionMessage = "Loading next scene...";

    private bool isCompleted;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D attachedBody = other.attachedRigidbody;

        if (attachedBody != null && attachedBody.gameObject.name == playerObjectName)
        {
            CompleteLevel(attachedBody);
        }
    }

    private void CompleteLevel(Rigidbody2D attachedBody)
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;

        if (playerBody == null)
        {
            playerBody = attachedBody;
        }

        foreach (MonoBehaviour behaviour in disableOnComplete)
        {
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }

        playerBody.linearVelocity = Vector2.zero;
        Invoke(nameof(LoadNextScene), loadDelay);
    }

    private void OnGUI()
    {
        if (!isCompleted)
        {
            return;
        }

        const int width = 360;
        const int height = 120;

        Rect rect = new((Screen.width - width) * 0.5f, 32f, width, height);
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label(completionTitle);
        GUILayout.Label(completionMessage);
        GUILayout.EndArea();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
