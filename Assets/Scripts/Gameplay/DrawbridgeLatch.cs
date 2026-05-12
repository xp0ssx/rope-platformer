using UnityEngine;

public sealed class DrawbridgeLatch : MonoBehaviour
{
    [SerializeField] private Rigidbody2D leftLeafBody;
    [SerializeField] private Rigidbody2D rightLeafBody;
    [SerializeField] private Transform leftTip;
    [SerializeField] private Transform rightTip;
    [SerializeField] private float latchDistance = 0.65f;
    [SerializeField] private float maxHeightDifference = 0.45f;
    [SerializeField] private float breakForce = 2500f;

    [Header("Visual")]
    [SerializeField] private Renderer[] statusRenderers;
    [SerializeField] private Color openColor = new(1f, 0.72f, 0.18f, 1f);
    [SerializeField] private Color latchedColor = new(0.25f, 1f, 0.45f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private FixedJoint2D latchJoint;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        CacheRenderersIfNeeded();
        SetVisualColor(openColor);
    }

    private void FixedUpdate()
    {
        if (latchJoint != null || !CanLatch())
        {
            return;
        }

        Latch();
    }

    private bool CanLatch()
    {
        if (leftLeafBody == null || rightLeafBody == null || leftTip == null || rightTip == null)
        {
            return false;
        }

        float distance = Vector2.Distance(leftTip.position, rightTip.position);
        float heightDifference = Mathf.Abs(leftTip.position.y - rightTip.position.y);
        return distance <= latchDistance && heightDifference <= maxHeightDifference;
    }

    private void Latch()
    {
        latchJoint = leftLeafBody.gameObject.AddComponent<FixedJoint2D>();
        latchJoint.connectedBody = rightLeafBody;
        latchJoint.autoConfigureConnectedAnchor = false;
        latchJoint.anchor = leftLeafBody.transform.InverseTransformPoint(leftTip.position);
        latchJoint.connectedAnchor = rightLeafBody.transform.InverseTransformPoint(rightTip.position);
        latchJoint.enableCollision = false;
        latchJoint.breakForce = breakForce;
        latchJoint.breakTorque = breakForce;

        SetVisualColor(latchedColor);
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
