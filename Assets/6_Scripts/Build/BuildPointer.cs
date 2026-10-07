using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class BuildPointer
{
    private static readonly List<RaycastResult> Hits = new(16);
    private static EventSystem cachedSystem;
    private static PointerEventData pointer;

    // Query current coordinates instead of depending on EventSystem Update order.
    public static bool IsOverUI()
    {
        if (EventSystem.current == null || Mouse.current == null) return false;
        if (cachedSystem != EventSystem.current || pointer == null)
        {
            cachedSystem = EventSystem.current;
            pointer = new PointerEventData(cachedSystem);
        }
        pointer.Reset();
        pointer.position = Mouse.current.position.ReadValue();
        Hits.Clear();
        cachedSystem.RaycastAll(pointer, Hits);
        for (int i = 0; i < Hits.Count; i++)
            if (Hits[i].module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }
}
