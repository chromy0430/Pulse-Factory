using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Two screen-space cues shared by every building. These graphics never receive pointer events.
public sealed class PowerPortFocusOverlay
{
    private readonly Canvas canvas;
    private readonly Cue[] cues;

    public PowerPortFocusOverlay()
    {
        canvas = new GameObject("Cable port focus", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var panel = Object.FindFirstObjectByType<BuildPanelView>();
        var sample = panel ? panel.GetComponentInChildren<TMP_Text>(true) : null;
        var font = sample ? sample.font : TMP_Settings.defaultFontAsset;
        cues = new[] {
            new Cue(canvas.transform, font, new Color(1f, .78f, .14f), true),
            new Cue(canvas.transform, font, new Color(.22f, 1f, .9f), false)
        };
    }

    public void Show(int index, Camera camera, PowerNode owner, PowerPortSlot port, Vector3 marker, bool output, int count, bool tooFar)
    {
        cues[index].Show(camera, owner, port, marker, output, count, tooFar);
        if (index == 1) cues[1].Avoid(cues[0], camera.pixelRect);
    }
    public void Hide(int index) => cues[index].Hide();
    public void Hide() { foreach (var cue in cues) cue.Hide(); }
    public void Dispose() { if (canvas) Object.Destroy(canvas.gameObject); }

    private sealed class Cue
    {
        private readonly RectTransform root, badge;
        private readonly RectTransform[] lines = new RectTransform[9];
        private readonly TextMeshProUGUI text;
        private readonly bool selected;
        private PowerNode owner;
        private BoxCollider box;
        private PowerPortSlot lastPort;
        private int lastCount;
        private bool lastOutput;
        private bool lastTooFar;
        private readonly Vector2[] corners = new Vector2[4];

        public Cue(Transform parent, TMP_FontAsset font, Color color, bool selected)
        {
            this.selected = selected;
            root = Rect(selected ? "Selected output" : "Port under pointer", parent);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = Rect("Stroke", root);
                lines[i].pivot = new Vector2(0f, .5f);
                Paint(lines[i], color);
            }
            badge = Rect("Label", root);
            Paint(badge, new Color(.035f, .055f, .07f, .94f));
            var label = Rect("Text", badge);
            text = label.gameObject.AddComponent<TextMeshProUGUI>();
            if (font) text.font = font;
            text.fontSize = 16; text.color = color; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center;
            label.anchorMax = Vector2.one; label.offsetMin = Vector2.zero; label.offsetMax = Vector2.zero;
            Hide();
        }

        public void Show(Camera cam, PowerNode node, PowerPortSlot port, Vector3 marker, bool output, int count, bool tooFar)
        {
            Vector3 p = cam.WorldToScreenPoint(marker);
            if (p.z < cam.nearClipPlane || !cam.pixelRect.Contains((Vector2)p)) { Hide(); return; }
            bool changed = owner != node;
            if (changed) { owner = node; box = node.GetComponent<BoxCollider>(); }
            if (!box) { Hide(); return; }
            if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
            if (changed || port != lastPort || lastCount != count || lastOutput != output || lastTooFar != tooFar)
            {
                lastPort = port; lastCount = count; lastOutput = output; lastTooFar = tooFar;
                string role = selected ? "출발 확정" : output ? "출발 후보" : "도착 후보";
                string cycle = count > 1 ? $"  ·  Tab 전환 ({count})" : "";
                string distance = tooFar ? "  ·  거리 초과" : "";
                text.text = $"{role}  ·  {node.DisplayName}  {port.Anchor.name.Replace("Port_", "")}{distance}{cycle}";
                badge.sizeDelta = new Vector2(text.GetPreferredValues(text.text).x + 18f, 27f);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 side = new Vector3(i == 0 || i == 3 ? -1 : 1, -1, i < 2 ? -1 : 1);
                Vector3 point = box.center + Vector3.Scale(box.size * .5f, side);
                corners[i] = cam.WorldToScreenPoint(box.transform.TransformPoint(point));
            }
            for (int i = 0; i < 4; i++) Stroke(lines[i], corners[i], corners[(i + 1) % 4]);
            Stroke(lines[4], cam.WorldToScreenPoint(port.Anchor.position), p);
            Vector2 center = p;
            Vector2 up = Vector2.up * 9, right = Vector2.right * 9;
            Stroke(lines[5], center + up, center + right);
            Stroke(lines[6], center + right, center - up);
            Stroke(lines[7], center - up, center - right);
            Stroke(lines[8], center - right, center + up);
            var area = cam.pixelRect;
            float yOffset = selected ? -40f : 14f;
            badge.anchoredPosition = new Vector2(
                Mathf.Clamp(selected ? p.x - badge.sizeDelta.x - 12f : p.x + 12f,
                    area.xMin, Mathf.Max(area.xMin, area.xMax - badge.sizeDelta.x)),
                Mathf.Clamp(p.y + yOffset, area.yMin, area.yMax - badge.sizeDelta.y));
        }

        public void Avoid(Cue other, Rect area)
        {
            if (!root.gameObject.activeSelf || !other.root.gameObject.activeSelf) return;
            var label = new Rect(badge.anchoredPosition, badge.sizeDelta);
            var obstacle = new Rect(other.badge.anchoredPosition, other.badge.sizeDelta);
            if (!label.Overlaps(obstacle)) return;
            float above = obstacle.yMax + 4f;
            float below = obstacle.yMin - label.height - 4f;
            badge.anchoredPosition = new Vector2(label.x, above + label.height <= area.yMax ? above : below);
        }

        public void Hide() { if (root.gameObject.activeSelf) root.gameObject.SetActive(false); }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        private static void Paint(RectTransform rt, Color color)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
        }

        private static void Stroke(RectTransform rt, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            rt.anchoredPosition = a;
            rt.sizeDelta = new Vector2(delta.magnitude, 2f);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
