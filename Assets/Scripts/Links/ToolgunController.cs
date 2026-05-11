using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ToolgunController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private LinkType currentTool = LinkType.Rope;
    [SerializeField] private float lengthStep = 0.25f;
    [SerializeField] private float removeLinkHitDistance = 0.35f;
    [SerializeField] private bool showDebugHud = true;

    private LinkableObject selectedObject;
    private PhysicalLink selectedLink;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        ReadModeSwitch();
        ReadSelectedLinkEditing();
        ReadLinkRemoval();
        ReadCancel();
        ReadSelection();
    }

    private void ReadLinkRemoval()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.cKey.wasPressedThisFrame)
            {
                PhysicalLink.DestroyAllLinks();
                SetSelectedLink(null);
                SetSelectedObject(null);
                return;
            }

            if ((keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame) && selectedLink != null)
            {
                PhysicalLink linkToRemove = selectedLink;
                SetSelectedLink(null);
                linkToRemove.DestroyLink();
                return;
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.middleButton.wasPressedThisFrame || targetCamera == null)
        {
            return;
        }

        if (TryGetMouseWorldPosition(out Vector3 worldPosition) &&
            PhysicalLink.TryFindNearest(worldPosition, removeLinkHitDistance, out PhysicalLink link))
        {
            SetSelectedLink(null);
            link.DestroyLink();
            SetSelectedObject(null);
        }
    }

    private void ReadModeSwitch()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            currentTool = LinkType.Rope;
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            currentTool = LinkType.Spring;
        }
    }

    private void ReadSelectedLinkEditing()
    {
        if (selectedLink == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame)
            {
                selectedLink.AdjustLength(-lengthStep);
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                selectedLink.AdjustLength(lengthStep);
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;

            if (scrollY > 0.01f)
            {
                selectedLink.AdjustLength(-lengthStep);
            }
            else if (scrollY < -0.01f)
            {
                selectedLink.AdjustLength(lengthStep);
            }
        }
    }

    private void ReadCancel()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool keyboardCancel = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        bool mouseCancel = mouse != null && mouse.rightButton.wasPressedThisFrame;

        if (keyboardCancel || mouseCancel)
        {
            SetSelectedObject(null);
            SetSelectedLink(null);
        }
    }

    private void ReadSelection()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || targetCamera == null)
        {
            return;
        }

        if (!TryGetMouseWorldPosition(out Vector3 worldPosition))
        {
            return;
        }

        Collider2D hit = Physics2D.OverlapPoint(worldPosition, selectableLayers);

        if (hit == null)
        {
            TrySelectLink(worldPosition);
            return;
        }

        LinkableObject linkable = hit.GetComponentInParent<LinkableObject>();

        if (linkable == null)
        {
            TrySelectLink(worldPosition);
            return;
        }

        SelectOrLink(linkable);
    }

    private void SelectOrLink(LinkableObject linkable)
    {
        if (selectedObject == null)
        {
            SetSelectedLink(null);
            SetSelectedObject(linkable);
            return;
        }

        if (selectedObject == linkable)
        {
            SetSelectedObject(null);
            return;
        }

        CreateLink(selectedObject, linkable);
        SetSelectedObject(null);
    }

    private void CreateLink(LinkableObject first, LinkableObject second)
    {
        GameObject linkObject = new($"Link_{first.name}_{second.name}");
        PhysicalLink link = linkObject.AddComponent<PhysicalLink>();
        LinkSettings settings = new(currentTool);
        link.Initialize(first, second, settings);
        SetSelectedLink(link);
    }

    private void TrySelectLink(Vector3 worldPosition)
    {
        SetSelectedObject(null);

        if (PhysicalLink.TryFindNearest(worldPosition, removeLinkHitDistance, out PhysicalLink link))
        {
            SetSelectedLink(link);
            return;
        }

        SetSelectedLink(null);
    }

    private bool TryGetMouseWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        Mouse mouse = Mouse.current;

        if (mouse == null || targetCamera == null)
        {
            return false;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z));
        worldPosition.z = 0f;
        return true;
    }

    private void SetSelectedObject(LinkableObject linkable)
    {
        if (selectedObject != null)
        {
            selectedObject.SetSelected(false);
        }

        selectedObject = linkable;

        if (selectedObject != null)
        {
            selectedObject.SetSelected(true);
        }
    }

    private void SetSelectedLink(PhysicalLink link)
    {
        if (selectedLink != null)
        {
            selectedLink.SetSelected(false);
        }

        selectedLink = link;

        if (selectedLink != null)
        {
            selectedLink.SetSelected(true);
        }
    }

    private void OnGUI()
    {
        if (!showDebugHud)
        {
            return;
        }

        const int width = 330;
        const int height = 168;

        GUILayout.BeginArea(new Rect(12, 12, width, height), GUI.skin.box);
        GUILayout.Label($"Tool: {currentTool}");
        GUILayout.Label($"Object: {(selectedObject == null ? "none" : selectedObject.name)}");
        GUILayout.Label($"Link: {GetSelectedLinkLabel()}");
        GUILayout.Label("Left click: select object/link");
        GUILayout.Label("Right click or Esc: cancel");
        GUILayout.Label("1: Rope  2: Spring");
        GUILayout.Label("Q / wheel up: shorten selected link");
        GUILayout.Label("E / wheel down: lengthen selected link");
        GUILayout.Label("Middle: remove link  Del: selected  C: clear");
        GUILayout.EndArea();
    }

    private string GetSelectedLinkLabel()
    {
        if (selectedLink == null)
        {
            return "none";
        }

        return $"{selectedLink.Type}, length {selectedLink.TargetLength:0.0}, load {selectedLink.Load01 * 100f:0}%";
    }
}
