using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class FinishZone : MonoBehaviour
{
    [SerializeField] private string playerObjectName = "Player";

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
            isCompleted = true;
        }
    }

    private void OnGUI()
    {
        if (!isCompleted)
        {
            return;
        }

        const int width = 300;
        const int height = 70;

        Rect rect = new((Screen.width - width) * 0.5f, 24f, width, height);
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label("Level complete");
        GUILayout.Label("Press R to restart");
        GUILayout.EndArea();
    }
}
