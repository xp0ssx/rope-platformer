using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class CargoScanner : MonoBehaviour
{
    [SerializeField] private DoorController targetDoor;
    [SerializeField] private Rigidbody2D requiredBody;
    [SerializeField] private string requiredObjectName = "CargoCore";
    [SerializeField] private bool stayOpenAfterScan = true;

    [Header("Visual")]
    [SerializeField] private Renderer[] statusRenderers;
    [SerializeField] private Color idleColor = new(0.2f, 0.9f, 0.95f, 1f);
    [SerializeField] private Color activeColor = new(0.2f, 1f, 0.35f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private int cargoInsideCount;
    private bool isActivated;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        CacheRenderersIfNeeded();
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        CacheRenderersIfNeeded();
        UpdateState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsRequiredCargo(other))
        {
            return;
        }

        cargoInsideCount++;
        isActivated = true;
        UpdateState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsRequiredCargo(other))
        {
            return;
        }

        cargoInsideCount = Mathf.Max(0, cargoInsideCount - 1);

        if (!stayOpenAfterScan)
        {
            isActivated = cargoInsideCount > 0;
        }

        UpdateState();
    }

    private bool IsRequiredCargo(Collider2D other)
    {
        Rigidbody2D attachedBody = other.attachedRigidbody;

        if (attachedBody == null)
        {
            return false;
        }

        if (requiredBody != null)
        {
            return attachedBody == requiredBody;
        }

        return attachedBody.gameObject.name == requiredObjectName;
    }

    private void UpdateState()
    {
        bool shouldOpenDoor = stayOpenAfterScan ? isActivated : cargoInsideCount > 0;

        if (targetDoor != null)
        {
            targetDoor.SetOpen(shouldOpenDoor);
        }

        SetVisualColor(shouldOpenDoor ? activeColor : idleColor);
    }

    private void SetVisualColor(Color color)
    {
        foreach (Renderer statusRenderer in statusRenderers)
        {
            if (statusRenderer == null)
            {
                continue;
            }

            propertyBlock.Clear();
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            statusRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void CacheRenderersIfNeeded()
    {
        if (statusRenderers != null && statusRenderers.Length > 0)
        {
            return;
        }

        statusRenderers = GetComponentsInChildren<Renderer>();
    }
}
