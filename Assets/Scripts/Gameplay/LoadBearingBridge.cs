using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class LoadBearingBridge : MonoBehaviour
{
    [SerializeField] private Rigidbody2D watchedBody;
    [SerializeField] private string watchedObjectName = "CargoCore";
    [SerializeField] private LinkableObject[] supportPoints;
    [SerializeField] private int requiredSupportLinks = 2;

    [Header("Physics")]
    [SerializeField] private bool freezeHorizontalMovement = true;
    [SerializeField] private bool freezeRotation = true;
    [SerializeField] private float stableGravityScale = 0f;
    [SerializeField] private float overloadedGravityScale = 2f;
    [SerializeField] private float unsupportedDownForce = 35f;
    [SerializeField] private float stableLinearDamping = 3f;
    [SerializeField] private float overloadedLinearDamping = 0.2f;
    [SerializeField] private float stableAngularDamping = 5f;
    [SerializeField] private float overloadedAngularDamping = 0.4f;

    [Header("Visual")]
    [SerializeField] private Renderer[] statusRenderers;
    [SerializeField] private Color stableColor = new(0.38f, 0.72f, 0.45f, 1f);
    [SerializeField] private Color overloadedColor = new(1f, 0.3f, 0.2f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Rigidbody2D body;
    private MaterialPropertyBlock propertyBlock;
    private int watchedContacts;
    private bool hasAppliedState;
    private bool wasOverloaded;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ApplyConstraints();
        propertyBlock = new MaterialPropertyBlock();
        CacheRenderersIfNeeded();
        ApplyBridgeState(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsWatchedBody(collision.rigidbody))
        {
            watchedContacts++;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (IsWatchedBody(collision.rigidbody))
        {
            watchedContacts = Mathf.Max(0, watchedContacts - 1);
        }
    }

    private void FixedUpdate()
    {
        int supportLinkCount = PhysicalLink.CountConnectedLinks(supportPoints, LinkType.Rope);
        bool isOverloaded = watchedContacts > 0 && supportLinkCount < requiredSupportLinks;

        ApplyBridgeState(isOverloaded);

        if (isOverloaded)
        {
            body.AddForce(Vector2.down * unsupportedDownForce, ForceMode2D.Force);
        }
    }

    private bool IsWatchedBody(Rigidbody2D otherBody)
    {
        if (otherBody == null)
        {
            return false;
        }

        if (watchedBody != null)
        {
            return otherBody == watchedBody;
        }

        return otherBody.gameObject.name == watchedObjectName;
    }

    private void ApplyConstraints()
    {
        RigidbodyConstraints2D constraints = body.constraints;

        if (freezeHorizontalMovement)
        {
            constraints |= RigidbodyConstraints2D.FreezePositionX;
        }

        if (freezeRotation)
        {
            constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        body.constraints = constraints;
    }

    private void ApplyBridgeState(bool isOverloaded)
    {
        body.gravityScale = isOverloaded ? overloadedGravityScale : stableGravityScale;
        body.linearDamping = isOverloaded ? overloadedLinearDamping : stableLinearDamping;
        body.angularDamping = isOverloaded ? overloadedAngularDamping : stableAngularDamping;

        if (hasAppliedState && wasOverloaded == isOverloaded)
        {
            return;
        }

        hasAppliedState = true;
        wasOverloaded = isOverloaded;
        SetVisualColor(isOverloaded ? overloadedColor : stableColor);
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
