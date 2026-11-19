using TMPro;
using PulseFactory.UI;
using UnityEngine;
using PulseFactory.Audio;

namespace PulseFactory.Progression
{
    public static class ProgressionUIFactory
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size;
            return rect;
        }
        public static UnityEngine.UI.Image Image(RectTransform rect, Color color)
        {
            if(PFUI.Theme!=null) return PFUI.Box(rect);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; return image;
        }
        public static TextMeshProUGUI Text(string name, Transform parent, string value, Vector2 pos, Vector2 size, float fontSize = 18)
        {
            var rect = Rect(name, parent, pos, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            var owner = parent.GetComponentInParent<ProgressionUI>();
            if (owner != null && owner.font != null) text.font = owner.font;
            if(PFUI.Theme!=null&&PFUI.Theme.font!=null)text.font=PFUI.Theme.font;
            text.text = value; text.fontSize = fontSize; text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
        public static UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction clicked)
        {
            var rect = Rect(name, parent, pos, size);
            Image(rect, new Color(.16f, .27f, .35f));
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            GameSounds.Bind(button);
            button.onClick.AddListener(clicked);
            var text = Text("Label", rect, label, new Vector2(6, 2), size - new Vector2(12, 4));
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }
        public static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = insetMin; rect.offsetMax = insetMax;
        }
    }
}
