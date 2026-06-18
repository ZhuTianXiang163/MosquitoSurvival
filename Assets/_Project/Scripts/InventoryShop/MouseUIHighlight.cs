using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Handles mouse hover highlighting for UI buttons when using XRUIInputModule.
/// This script manually tracks mouse position and updates button highlight states.
/// </summary>
public class MouseUIHighlight : MonoBehaviour
{
    private GraphicRaycaster graphicRaycaster;
    private PointerEventData pointerEventData;
    private Selectable currentHighlighted;

    private void Awake()
    {
        graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            Debug.LogWarning("MouseUIHighlight: No GraphicRaycaster found in parent");
        }

        pointerEventData = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        if (graphicRaycaster == null || EventSystem.current == null)
            return;

        // Update pointer position to mouse position using new Input System
        if (Mouse.current != null)
        {
            pointerEventData.position = Mouse.current.position.ReadValue();
        }
        else
        {
            return;
        }

        // Perform raycast
        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);

        // Find the first selectable UI element
        Selectable newHighlighted = null;
        foreach (var result in results)
        {
            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsInteractable())
            {
                newHighlighted = selectable;
                break;
            }
        }

        // Update highlight state
        if (newHighlighted != currentHighlighted)
        {
            // Unhighlight previous
            if (currentHighlighted != null)
            {
                currentHighlighted.OnPointerExit(new PointerEventData(EventSystem.current));
            }

            // Highlight new
            if (newHighlighted != null)
            {
                newHighlighted.OnPointerEnter(new PointerEventData(EventSystem.current));
            }

            currentHighlighted = newHighlighted;
        }
    }
}
