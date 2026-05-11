using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ToolgunController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private LinkType currentTool = LinkType.Rope;
    [SerializeField] private float lengthStep = 0.25f;
    [SerializeField] private float removeLinkHitDistance = 0.35f;
    [SerializeField] private bool showHud = true;

    private LinkableObject selectedObject;
    private readonly List<PhysicalLink> selectedLinks = new();

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        RemoveMissingSelectedLinks();
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
                ClearSelectedLinks();
                SetSelectedObject(null);
                return;
            }

            if ((keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame) && selectedLinks.Count > 0)
            {
                PhysicalLink[] linksToRemove = selectedLinks.ToArray();
                ClearSelectedLinks();

                foreach (PhysicalLink linkToRemove in linksToRemove)
                {
                    if (linkToRemove != null)
                    {
                        linkToRemove.DestroyLink();
                    }
                }

                return;
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.middleButton.wasPressedThisFrame || targetCamera == null)
        {
            return;
        }

        if (TryGetMouseWorldPosition(out Vector3 worldPosition) &&
            PhysicalLink.TryFindNearest(worldPosition, removeLinkHitDistance, out PhysicalLink nearestLink))
        {
            RemoveSelectedLink(nearestLink);
            nearestLink.DestroyLink();
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
        if (selectedLinks.Count == 0)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame)
            {
                AdjustSelectedLinks(-lengthStep);
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                AdjustSelectedLinks(lengthStep);
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;

            if (scrollY > 0.01f)
            {
                AdjustSelectedLinks(-lengthStep);
            }
            else if (scrollY < -0.01f)
            {
                AdjustSelectedLinks(lengthStep);
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
            ClearSelectedLinks();
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
            ClearSelectedLinks();
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
        SetSingleSelectedLink(link);
    }

    private void TrySelectLink(Vector3 worldPosition)
    {
        SetSelectedObject(null);
        bool additiveSelection = IsAdditiveSelectionPressed();

        if (PhysicalLink.TryFindNearest(worldPosition, removeLinkHitDistance, out PhysicalLink link))
        {
            if (additiveSelection)
            {
                ToggleSelectedLink(link);
            }
            else
            {
                SetSingleSelectedLink(link);
            }

            return;
        }

        if (!additiveSelection)
        {
            ClearSelectedLinks();
        }
    }

    private void AdjustSelectedLinks(float delta)
    {
        foreach (PhysicalLink link in selectedLinks)
        {
            if (link != null)
            {
                link.AdjustLength(delta);
            }
        }
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

    private void SetSingleSelectedLink(PhysicalLink link)
    {
        ClearSelectedLinks();
        AddSelectedLink(link);
    }

    private void ToggleSelectedLink(PhysicalLink link)
    {
        if (selectedLinks.Contains(link))
        {
            RemoveSelectedLink(link);
            return;
        }

        AddSelectedLink(link);
    }

    private void AddSelectedLink(PhysicalLink link)
    {
        if (link == null || selectedLinks.Contains(link))
        {
            return;
        }

        selectedLinks.Add(link);
        link.SetSelected(true);
    }

    private void RemoveSelectedLink(PhysicalLink link)
    {
        if (link == null)
        {
            return;
        }

        if (selectedLinks.Remove(link))
        {
            link.SetSelected(false);
        }
    }

    private void ClearSelectedLinks()
    {
        foreach (PhysicalLink link in selectedLinks)
        {
            if (link != null)
            {
                link.SetSelected(false);
            }
        }

        selectedLinks.Clear();
    }

    private void RemoveMissingSelectedLinks()
    {
        for (int i = selectedLinks.Count - 1; i >= 0; i--)
        {
            if (selectedLinks[i] == null)
            {
                selectedLinks.RemoveAt(i);
            }
        }
    }

    private static bool IsAdditiveSelectionPressed()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    private void OnGUI()
    {
        if (!showHud)
        {
            return;
        }

        const int width = 190;
        const int height = 64;

        GUILayout.BeginArea(new Rect(12, 12, width, height), GUI.skin.box);
        GUILayout.Label($"Tool: {currentTool}");
        GUILayout.Label(GetCompactSelectionLabel());
        GUILayout.EndArea();
    }

    private string GetCompactSelectionLabel()
    {
        if (selectedLinks.Count > 1)
        {
            return $"Links: {selectedLinks.Count}";
        }

        if (selectedLinks.Count == 1)
        {
            PhysicalLink link = selectedLinks[0];
            return $"{link.Type} load {link.Load01 * 100f:0}%";
        }

        if (selectedObject != null)
        {
            return $"Object: {selectedObject.name}";
        }

        return "1 Rope  2 Spring";
    }
}
