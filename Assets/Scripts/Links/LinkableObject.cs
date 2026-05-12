using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class LinkableObject : MonoBehaviour
{
    [SerializeField] private Rigidbody2D bodyOverride;
    [SerializeField] private Transform linkAnchor;
    [SerializeField] private Renderer[] selectionRenderers;
    [SerializeField] private Color selectedColor = new(0.15f, 0.85f, 1f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock propertyBlock;

    public Rigidbody2D Body { get; private set; }

    public Vector3 LinkPosition => linkAnchor != null ? linkAnchor.position : transform.position;

    private void Awake()
    {
        Body = bodyOverride != null ? bodyOverride : GetComponent<Rigidbody2D>();

        if (Body == null)
        {
            Body = GetComponentInParent<Rigidbody2D>();
        }

        CacheRenderersIfNeeded();
    }

    public void SetSelected(bool isSelected)
    {
        CacheRenderersIfNeeded();

        propertyBlock ??= new MaterialPropertyBlock();

        foreach (Renderer selectionRenderer in selectionRenderers)
        {
            if (selectionRenderer == null)
            {
                continue;
            }

            propertyBlock.Clear();

            if (isSelected)
            {
                propertyBlock.SetColor(BaseColorId, selectedColor);
                propertyBlock.SetColor(ColorId, selectedColor);
            }

            selectionRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void CacheRenderersIfNeeded()
    {
        if (selectionRenderers != null && selectionRenderers.Length > 0)
        {
            return;
        }

        selectionRenderers = GetComponentsInChildren<Renderer>();
    }
}
