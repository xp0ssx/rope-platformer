using UnityEngine;

public sealed class DoorController : MonoBehaviour
{
    [SerializeField] private Vector3 openOffset = new(0f, 3f, 0f);
    [SerializeField] private float moveSpeed = 5f;

    private Vector3 closedPosition;
    private bool isOpen;

    private void Awake()
    {
        closedPosition = transform.position;
    }

    private void Update()
    {
        Vector3 targetPosition = isOpen ? closedPosition + openOffset : closedPosition;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    public void SetOpen(bool shouldOpen)
    {
        isOpen = shouldOpen;
    }
}
