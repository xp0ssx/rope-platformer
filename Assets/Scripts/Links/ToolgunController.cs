using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ToolgunController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private LinkType currentLinkType = LinkType.Rigid;
    [SerializeField] private bool showDebugHud = true;

    private LinkableObject selectedObject;

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
        ReadCancel();
        ReadSelection();
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

        Vector2 screenPosition = mouse.position.ReadValue();
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z));
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
        link.Initialize(first, second, currentLinkType);
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
        const int height = 120;

        GUILayout.BeginArea(new Rect(12, 12, width, height), GUI.skin.box);
        GUILayout.Label($"Toolgun mode: {currentLinkType}");
        GUILayout.Label($"Selected: {(selectedObject == null ? "none" : selectedObject.name)}");
        GUILayout.Label("Left click: select/link");
        GUILayout.Label("Right click or Esc: cancel");
        GUILayout.Label("1: rigid  2: elastic");
        GUILayout.EndArea();
    }
}
