using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ToolgunController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private LinkType currentLinkType = LinkType.Rigid;
    [SerializeField] private float lengthMultiplier = 1f;
    [SerializeField] private float maxForceBeforeBreak = 800f;
    [SerializeField] private float lengthStep = 0.1f;
    [SerializeField] private float forceStep = 100f;
    [SerializeField] private float removeLinkHitDistance = 0.35f;
    [SerializeField] private bool showDebugHud = true;

    private LinkableObject selectedObject;
    private PhysicalLink lastCreatedLink;

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
        ReadParameterControls();
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
                lastCreatedLink = null;
                SetSelectedObject(null);
                return;
            }

            if ((keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame) && lastCreatedLink != null)
            {
                lastCreatedLink.DestroyLink();
                lastCreatedLink = null;
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
            if (lastCreatedLink == link)
            {
                lastCreatedLink = null;
            }

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
            currentLinkType = LinkType.Rigid;
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            currentLinkType = LinkType.Elastic;
        }
    }

    private void ReadParameterControls()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame)
            {
                lengthMultiplier -= lengthStep;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                lengthMultiplier += lengthStep;
            }

            if (keyboard.zKey.wasPressedThisFrame)
            {
                maxForceBeforeBreak -= forceStep;
            }

            if (keyboard.xKey.wasPressedThisFrame)
            {
                maxForceBeforeBreak += forceStep;
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;

            if (scrollY > 0.01f)
            {
                lengthMultiplier += lengthStep;
            }
            else if (scrollY < -0.01f)
            {
                lengthMultiplier -= lengthStep;
            }
        }

        lengthMultiplier = Mathf.Clamp(lengthMultiplier, 0.35f, 2.5f);
        maxForceBeforeBreak = Mathf.Clamp(maxForceBeforeBreak, 100f, 3000f);
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
            return;
        }

        LinkableObject linkable = hit.GetComponentInParent<LinkableObject>();

        if (linkable == null)
        {
            return;
        }

        SelectOrLink(linkable);
    }

    private void SelectOrLink(LinkableObject linkable)
    {
        if (selectedObject == null)
        {
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
        LinkSettings settings = new(currentLinkType, lengthMultiplier, maxForceBeforeBreak);
        link.Initialize(first, second, settings);
        lastCreatedLink = link;
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

    private void OnGUI()
    {
        if (!showDebugHud)
        {
            return;
        }

        const int width = 300;
        const int height = 178;

        GUILayout.BeginArea(new Rect(12, 12, width, height), GUI.skin.box);
        GUILayout.Label($"Toolgun mode: {currentLinkType}");
        GUILayout.Label($"Length: {lengthMultiplier:0.0}x   Strength: {maxForceBeforeBreak:0}");
        GUILayout.Label($"Selected: {(selectedObject == null ? "none" : selectedObject.name)}");
        GUILayout.Label("Left click: select/link");
        GUILayout.Label("Right click or Esc: cancel");
        GUILayout.Label("1/2: type  Q/E or wheel: length");
        GUILayout.Label("Z/X: strength");
        GUILayout.Label("Middle: remove link  C: clear");
        GUILayout.EndArea();
    }
}
