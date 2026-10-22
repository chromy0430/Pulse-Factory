using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicitly invoked integration checks, with temporary data and input devices.
public static class BuildMenuPlayVerification
{
    private static IEnumerator<float> routine;
    private static double next;
    private static int frame;
    private static Mouse mouse;
    private static Keyboard keyboard;
    private static bool background;
    private static string output;
    private static readonly List<string> results = new();
    private static readonly List<GameObject> placed = new();
    private static BuildPanelView panel;
    private static BuildController build;
    private static BuildMenuListView menu;
    private static BuildMenuScroller scroller;
    private static UnityEngine.UI.ScrollRect scroll;
    private static UnityEngine.UI.Button previous, forward;
    private static readonly List<BuildingDefinitionSO> definitions = new();
    public static string Status { get; private set; } = "Idle";

    public static string Begin(string report)
    {
        if (!Application.isPlaying || routine != null) return "Requires idle Play Mode";
        output = report; results.Clear(); placed.Clear(); definitions.Clear();
        background = Application.runInBackground; Application.runInBackground = true;
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        panel = UnityEngine.Object.FindFirstObjectByType<BuildPanelView>();
        build = UnityEngine.Object.FindFirstObjectByType<BuildController>();
        menu = UnityEngine.Object.FindFirstObjectByType<BuildMenuListView>();
        scroller = UnityEngine.Object.FindFirstObjectByType<BuildMenuScroller>();
        scroll = scroller.GetComponent<UnityEngine.UI.ScrollRect>();
        previous = scroller.transform.Find("Previous").GetComponent<UnityEngine.UI.Button>();
        forward = scroller.transform.Find("Next").GetComponent<UnityEngine.UI.Button>();
        foreach (var item in menu.Items) if (!item.IsCable) definitions.Add(item.Definition);
        mouse = InputSystem.AddDevice<Mouse>("BuildMenuVerificationMouse");
        keyboard = InputSystem.AddDevice<Keyboard>("BuildMenuVerificationKeyboard");
        mouse.MakeCurrent(); keyboard.MakeCurrent();
        routine = Run(); next = 0; Status = "Running";
        EditorApplication.update += Step;
        return Status;
    }

    private static void Step()
    {
        if (!Application.isPlaying) { Finish("Interrupted"); return; }
        if (EditorApplication.timeSinceStartup < next || Time.frameCount <= frame + 2) return;
        try
        {
            if (!routine.MoveNext()) { Finish("Complete"); return; }
            frame = Time.frameCount; next = EditorApplication.timeSinceStartup + routine.Current;
        }
        catch (Exception ex) { results.Add("FAIL exception " + ex); Finish("Failed"); }
    }

    private static void Check(bool pass, string name) => results.Add((pass ? "PASS " : "FAIL ") + name);
    private static void Key(Key key = UnityEngine.InputSystem.Key.None) => InputSystem.QueueStateEvent(keyboard,
        key == UnityEngine.InputSystem.Key.None ? new KeyboardState() : new KeyboardState(key));
    private static void Point(Vector2 position, bool pressed = false, bool right = false)
    {
        var state = new MouseState { position = position };
        if (pressed) state = state.WithButton(right ? MouseButton.Right : MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
    }
    private static Vector2 Center(Component value)
    {
        var rect = (RectTransform)value.transform;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
    private static int VisibleItems()
    {
        int count = 0; var corners = new Vector3[4];
        foreach (var item in menu.Items)
        {
            ((RectTransform)item.transform).GetWorldCorners(corners);
            float min = scroll.viewport.InverseTransformPoint(corners[0]).x;
            float max = scroll.viewport.InverseTransformPoint(corners[2]).x;
            if (max > scroll.viewport.rect.xMin + 0.01f && min < scroll.viewport.rect.xMax - 0.01f) count++;
        }
        return count;
    }

    private static IEnumerator<float> Run()
    {
        Check(menu.Items.Count == 18 && definitions.Count == 17, "Editor-generated catalog loaded at runtime, including 15 new opted-in SO assets");
        Check(scroller.VisibleCount == 8 && VisibleItems() == 8, "Initial viewport clips to exactly eight entries");
        Check(EventSystem.current != null && menu.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null, "EventSystem and Canvas GraphicRaycaster present");
        Check(scroll.horizontal && !scroll.vertical && scroll.viewport.GetComponent<UnityEngine.UI.RectMask2D>() != null, "Horizontal ScrollRect and RectMask2D wired");
        Check(scroll.content.GetComponent<UnityEngine.UI.ContentSizeFitter>() != null && scroll.content.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null, "Content auto layout and size fitter wired");
        Key(UnityEngine.InputSystem.Key.B); yield return 0.15f;
        Key(); yield return 0.3f;
        Check(panel.IsOpen, "Actual B input opens construction menu");
        Check(!previous.interactable && forward.interactable, "Left boundary disables previous arrow");
        int before = UnityEngine.Object.FindObjectsByType<BuildingRuntime>(FindObjectsSortMode.None).Length;
        Point(Center(forward)); yield return 0.1f;
        Point(Center(forward), true); yield return 0.1f;
        Point(Center(forward)); yield return 0.3f;
        Check(scroller.TargetFirst == 1 && scroll.horizontalNormalizedPosition > 0f, "Actual mouse click on right arrow reveals next entry");
        Check(panel.IsOpen && before == UnityEngine.Object.FindObjectsByType<BuildingRuntime>(FindObjectsSortMode.None).Length, "Arrow click does not select or place a building");
        Check(VisibleItems() == 8 && previous.interactable, "Scrolled viewport retains eight visible icons");
        for (int i = 0; i < 40; i++) forward.onClick.Invoke();
        yield return 0.35f;
        Check(scroller.TargetFirst == 10 && Mathf.Abs(scroll.horizontalNormalizedPosition - 1f) < 0.001f, "Rapid right clicks clamp to final slot target=" + scroller.TargetFirst + " normalized=" + scroll.horizontalNormalizedPosition);
        Check(!forward.interactable && previous.interactable, "Right boundary disables next arrow");
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Center(menu.Items[0]) }, hits);
        bool hiddenHit = false;
        foreach (var hit in hits) if (hit.gameObject.transform.IsChildOf(menu.Items[0].transform)) hiddenHit = true;
        Check(!hiddenHit, "Hidden clipped button is absent from UI raycast results");
        var invalid = menu.Items[menu.Items.Count - 2];
        Check(!invalid.GetComponent<UnityEngine.UI.Button>().interactable && invalid.transform.Find("Icon/Fallback").gameObject.activeSelf, "Incomplete SO remains visible, disabled, with text fallback");
        Point(Center(invalid)); yield return 0.15f;
        var status = (TMPro.TMP_Text)new SerializedObject(panel).FindProperty("statusLabel").objectReferenceValue;
        Check(status.text.Contains("Prefab"), "Hovering disabled entry explains missing Prefab");
        Point(Center(menu.Items[menu.Items.Count - 1])); yield return 0.1f;
        Point(Center(menu.Items[menu.Items.Count - 1]), true); yield return 0.1f;
        Point(Center(menu.Items[menu.Items.Count - 1])); yield return 0.3f;
        Check(UnityEngine.Object.FindFirstObjectByType<PowerConnectionController>().IsBuildMode && !panel.IsOpen, "Cable entry still enters wire construction mode");
        Key(UnityEngine.InputSystem.Key.Escape); yield return 0.1f; Key(); yield return 0.1f;
        Check(!UnityEngine.Object.FindFirstObjectByType<PowerConnectionController>().IsBuildMode, "ESC cancels cable mode");
        Key(UnityEngine.InputSystem.Key.B); yield return 0.1f; Key(); yield return 0.3f;
        for (int i = 0; i < 40; i++) previous.onClick.Invoke();
        yield return 0.3f;
        Check(scroller.TargetFirst == 0 && Mathf.Abs(scroll.horizontalNormalizedPosition) < 0.001f, "Left arrow restores initial entries and clamps target=" + scroller.TargetFirst + " normalized=" + scroll.horizontalNormalizedPosition);
        var dragStart = Center(menu.Items[4]);
        Point(dragStart); yield return 0.1f;
        Point(dragStart, true); yield return 0.1f;
        Point(dragStart + Vector2.left * 15f, true); yield return 0.1f;
        Point(dragStart + Vector2.left * 180f, true); yield return 0.15f;
        Point(dragStart + Vector2.left * 180f); yield return 0.15f;
        Check(scroll.horizontalNormalizedPosition > 0.01f && panel.IsOpen, "Mouse drag scrolls without selecting a building");
        int expected = Mathf.Clamp(Mathf.RoundToInt(scroll.horizontalNormalizedPosition * 10) + 1, 0, 10);
        scroller.Step(1); yield return 0.3f;
        Check(scroller.TargetFirst == expected, "Arrow resumes from dragged scroll position");
        for (int i = 0; i < 40; i++) scroller.Step(-1);
        yield return 0.3f;
        scroller.Step(1); yield return 0.25f;
        var exact = menu.Items[3];
        Point(Center(exact)); yield return 0.1f; Point(Center(exact), true); yield return 0.1f; Point(Center(exact)); yield return 0.3f;
        Check(build.SelectedDefinition == exact.Definition && !panel.IsOpen, "Scrolled button selects the exact SO rather than a fixed index");
        var grid = UnityEngine.Object.FindFirstObjectByType<BuildGrid>();
        Vector3 free = default; bool found = false;
        for (int x = -3; x <= 3 && !found; x++)
            for (int z = -3; z <= 3; z++)
            {
                var candidate = grid.Snap(grid.SurfaceCenter + grid.Rotation * new Vector3(x, 0, z));
                if (!grid.CanPlace(candidate, exact.Definition.PlacementSize, out _)) continue;
                free = candidate; found = true; break;
            }
        Check(found, "Found valid board cell for newly registered building");
        var created = found ? build.PlaceAt(free) : null;
        if (created != null) placed.Add(created.gameObject);
        Check(created != null && new SerializedObject(created).FindProperty("definition").objectReferenceValue == exact.Definition, "New catalog SO places a building with matching runtime definition");
        Key(UnityEngine.InputSystem.Key.Escape); yield return 0.1f; Key(); yield return 0.1f;
        Check(build.SelectedDefinition == null, "ESC cancels building placement");
        Key(UnityEngine.InputSystem.Key.B); yield return 0.1f; Key(); yield return 0.3f;
        Point(new Vector2(20, 20), true, true); yield return 0.1f; Point(new Vector2(20, 20)); yield return 0.3f;
        Check(!panel.IsOpen, "Right click closes menu");
        build.ToggleMenu(); yield return 0.3f;
        foreach (int total in new[] { 1, 2, 8, 9, 17 })
        {
            var subset = new List<BuildingDefinitionSO>();
            for (int i = 0; i < total - 1; i++) subset.Add(definitions[i]);
            menu.Populate(subset); yield return 0.2f;
            Check(menu.Items.Count == total && scroller.VisibleCount == Mathf.Min(8, total) && VisibleItems() == Mathf.Min(8, total), "Auto layout / clipping with " + total + " total entries");
            Check(previous.gameObject.activeSelf == (total > 8) && forward.gameObject.activeSelf == (total > 8), "Arrow visibility with " + total + " total entries");
        }
        var s = new SerializedObject(menu); s.FindProperty("scroller").objectReferenceValue = null; s.ApplyModifiedPropertiesWithoutUndo();
        scroller.enabled = false;
        menu.Populate(new[] { definitions[0] }); yield return 0.15f;
        Check(menu.Items.Count == 2, "Menu still generates entries with optional scrolling module detached");
        s.Update(); s.FindProperty("scroller").objectReferenceValue = scroller; s.ApplyModifiedPropertiesWithoutUndo(); scroller.enabled = true;
        menu.Populate(definitions); yield return 0.25f;
        Check(menu.Items.Count == 18 && VisibleItems() == 8, "Restoring module rebuilds cleanly without duplicated entries");
        Check(menu.Items[0].transform.Find("Label").GetComponent<TMPro.TMP_Text>().font.HasCharacters("조립 공장"), "Korean building names have valid font glyphs");
        Point(new Vector2(20, 20)); yield return 0.1f;
    }

    private static void Finish(string status)
    {
        EditorApplication.update -= Step;
        routine?.Dispose(); routine = null;
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        foreach (var item in placed) if (item != null) UnityEngine.Object.Destroy(item);
        Application.runInBackground = background;
        Status = status;
        File.WriteAllLines(output, results);
    }
}
