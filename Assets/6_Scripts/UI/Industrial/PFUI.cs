using TMPro;
using UnityEngine;
using PulseFactory.Audio;
using UnityEngine.Events;

namespace PulseFactory.UI
{
    public static class PFUI
    {
        public static PFUITheme Theme => PFUITheme.Current;
        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); Top(r, position, size); return r;
        }
        public static void Top(RectTransform r, Vector2 p, Vector2 s)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(p.x, -p.y); r.sizeDelta = s;
        }
        public static void Stretch(RectTransform r, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        { r.anchorMin = min; r.anchorMax = max; r.offsetMin = low; r.offsetMax = high; }
        public static UnityEngine.UI.Image Box(RectTransform r, string frame = "Panel", bool raycast = true)
        {
            var i = r.GetComponent<UnityEngine.UI.Image>() ?? r.gameObject.AddComponent<UnityEngine.UI.Image>();
            i.sprite = Theme != null ? Theme.Get(frame) : null;
            i.type = UnityEngine.UI.Image.Type.Sliced; i.color = Color.white; i.raycastTarget = raycast; return i;
        }
        public static TextMeshProUGUI Text(string name, Transform parent, string value, Vector2 pos, Vector2 size, float fontSize = 20)
        {
            var r = Rect(name, parent, pos, size); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (Theme != null && Theme.font != null) t.font = Theme.font;
            t.text = value; t.fontSize = fontSize; t.fontStyle = FontStyles.Bold; t.color = Theme != null ? Theme.text : Color.white;
            t.alignment = TextAlignmentOptions.MidlineLeft; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        public static UnityEngine.UI.Image Icon(string name, Transform parent, string key, Vector2 pos, float size)
        {
            var r = Rect(name, parent, pos, Vector2.one * size); var i = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            i.sprite = Theme != null ? Theme.Get(key) : null; i.preserveAspect = true; i.raycastTarget = false; return i;
        }
        public static UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 pos, Vector2 size, UnityAction action, string icon = null)
        {
            var r = Rect(name, parent, pos, size); var im = Box(r);
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = im; GameSounds.Bind(b); b.onClick.AddListener(action);
            var colors = b.colors; colors.highlightedColor = new Color(.75f,1,1); colors.pressedColor = new Color(.45f,.8f,.8f);
            colors.disabledColor = new Color(.4f,.45f,.48f); b.colors = colors;
            float left = icon == null ? 8 : 46;
            if (icon != null) Icon("Icon", r, icon, new Vector2(12, (size.y - 28) / 2), 28);
            var text = Text("Label", r, label, new Vector2(left, 3), new Vector2(size.x-left-8,size.y-6), 19);
            text.alignment = TextAlignmentOptions.Center;
            return b;
        }
        public static void State(UnityEngine.UI.Image image, bool selected, bool complete = false, bool locked = false)
        {
            if (Theme == null || image == null) return;
            image.sprite = Theme.Get(selected ? "PanelSelected" : complete ? "PanelComplete" : locked ? "PanelDisabled" : "Panel");
            image.color = Color.white;
        }
    }
}
