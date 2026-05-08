using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class PressureButton : MonoBehaviour
{
    [SerializeField] private DoorController targetDoor;
    [SerializeField] private LayerMask pressingLayers = ~0;
    [SerializeField] private float minimumMass = 2f;

    private int pressingCount;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanPress(other))
        {
            return;
        }

        pressingCount++;
        UpdateDoor();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!CanPress(other))
        {
            return;
        }

        pressingCount = Mathf.Max(0, pressingCount - 1);
        UpdateDoor();
    }

    private bool CanPress(Collider2D other)
    {
        bool layerAllowed = (pressingLayers.value & (1 << other.gameObject.layer)) != 0;
        return layerAllowed && other.attachedRigidbody != null && other.attachedRigidbody.mass >= minimumMass;
    }

    private void UpdateDoor()
    {
        if (targetDoor != null)
        {
            targetDoor.SetOpen(pressingCount > 0);
        }
    }
}
