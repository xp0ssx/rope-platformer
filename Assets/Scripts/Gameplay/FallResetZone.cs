using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class FallResetZone : MonoBehaviour
{
    [SerializeField] private LevelReset levelReset;
    [SerializeField] private string playerObjectName = "Player";

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        if (levelReset == null)
        {
            levelReset = FindAnyObjectByType<LevelReset>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D attachedBody = other.attachedRigidbody;

        if (attachedBody != null && attachedBody.gameObject.name == playerObjectName)
        {
            levelReset.ResetLevel();
        }
    }
}
