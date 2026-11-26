using PulseFactory.Audio;
using PulseFactory.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PulseFactory.UI
{
    public sealed class FactoryPopTooltip : MonoBehaviour
    {
        private readonly Image[] choices = new Image[6];
        private RectTransform root;
        private RectTransform panel;
        private BuildController build;
        private BuildingRuntime factory;
        private Camera view;
        private Vector3 pickedLocal;
        private TextMeshProUGUI factoryName;

        public void Initialize(RectTransform canvas, BuildController controller)
        {
            root = canvas;
            build = controller;
            view = Camera.main;
            panel = PFUI.Rect("FactoryPopPanel", root, Vector2.zero, new Vector2(328, 136));
            panel.pivot = new Vector2(0, 1);
            PFUI.Box(panel);
            PFUI.Text("Title", panel, "공장 POP 효과음", new Vector2(14, 8), new Vector2(245, 33), 21);
            PFUI.Button("Close", panel, "×", new Vector2(278, 8), new Vector2(38, 32), Close);
            factoryName = PFUI.Text("Route", panel, "", new Vector2(14, 45), new Vector2(300, 28), 16);
            factoryName.textWrappingMode = TextWrappingModes.NoWrap;
            factoryName.overflowMode = TextOverflowModes.Ellipsis;
            for (int i = 0; i < choices.Length; i++)
            {
                int number = i;
                float x = i == 0 ? 12 : 83 + (i - 1) * 47;
                var button = PFUI.Button("Pop" + i, panel, i == 0 ? "자동" : i.ToString(),
                    new Vector2(x, 84), new Vector2(i == 0 ? 65 : 43, 37), () => Choose(number));
                choices[i] = button.GetComponent<Image>();
            }
            Close();
        }

        private void Update()
        {
            if (PulseFactory.Menu.PFMenuScreen.BlocksGameplay) return;
            var mouse = Mouse.current;
            if (!panel || mouse == null) return;
            if (!view) view = Camera.main;
            if (!view || !build || build.BuildMode != "Idle" || ProgressionUI.IsOpen ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                mouse.rightButton.wasPressedThisFrame)
            {
                Close();
                return;
            }

            Vector2 pointer = mouse.position.ReadValue();
            bool overPanel = panel.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(panel, pointer);
            if (mouse.leftButton.wasPressedThisFrame && !overPanel)
            {
                if (BuildPointer.IsOverUI()) Close();
                else OpenAt(pointer);
            }
            if (!panel.gameObject.activeSelf) return;
            if (!factory || !factory.isActiveAndEnabled)
            {
                Close();
                return;
            }

            Vector3 screen = view.WorldToScreenPoint(factory.transform.TransformPoint(pickedLocal));
            if (screen.z < view.nearClipPlane || !view.pixelRect.Contains(screen)) { Close(); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
            var bounds = root.rect;
            float x = Mathf.Clamp(local.x - bounds.xMin + 12, 0, Mathf.Max(0, bounds.width - panel.rect.width));
            float y = Mathf.Clamp(bounds.yMax - local.y + 10, 0, Mathf.Max(0, bounds.height - panel.rect.height));
            panel.anchoredPosition = new Vector2(x, -y);
        }

        private void OpenAt(Vector2 pointer)
        {
            if (!view.pixelRect.Contains(pointer) ||
                !Physics.Raycast(view.ScreenPointToRay(pointer), out var hit, view.farClipPlane,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Close();
                return;
            }

            factory = hit.collider.GetComponentInParent<BuildingRuntime>();
            if (!factory || !factory.isActiveAndEnabled) { Close(); return; }
            pickedLocal = factory.transform.InverseTransformPoint(hit.point);
            factoryName.text = factory.DisplayName;
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            RefreshSelection();
        }

        private void Choose(int number)
        {
            if (!factory || !factory.isActiveAndEnabled) { Close(); return; }
            factory.PopNumber = number;
            GameSounds.Pop(factory);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < choices.Length; i++) PFUI.State(choices[i], factory.PopNumber == i);
        }

        private void Close()
        {
            factory = null;
            if (panel && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
        }

        private void OnDisable() => Close();
    }
}
