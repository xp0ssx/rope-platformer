using UnityEngine;

public sealed class TutorialHint : MonoBehaviour
{
    [TextArea(2, 4)]
    [SerializeField] private string message;
    [SerializeField] private Transform player;
    [SerializeField] private float visibleDistance = 5.5f;
    [SerializeField] private Vector2 screenOffset = new(0f, -24f);
    [SerializeField] private int width = 260;
    [SerializeField] private int height = 70;
    [SerializeField] private bool keepOnScreen = true;

    private Camera targetCamera;

    private void Awake()
    {
        targetCamera = Camera.main;

        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            player = playerObject != null ? playerObject.transform : null;
        }
    }

    private void OnGUI()
    {
        if (targetCamera == null || player == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (Vector2.Distance(transform.position, player.position) > visibleDistance)
        {
            return;
        }

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(transform.position);

        if (screenPosition.z < 0f)
        {
            return;
        }

        Rect rect = new(
            screenPosition.x - width * 0.5f + screenOffset.x,
            Screen.height - screenPosition.y + screenOffset.y,
            width,
            height);

        if (keepOnScreen)
        {
            rect.x = Mathf.Clamp(rect.x, 8f, Screen.width - rect.width - 8f);
            rect.y = Mathf.Clamp(rect.y, 8f, Screen.height - rect.height - 8f);
        }

        GUI.Box(rect, message);
    }
}
