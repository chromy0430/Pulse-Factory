using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public sealed class BuildPanelView : MonoBehaviour
{
    [BoxGroup("Panel"), Required, SerializeField] private RectTransform panel;
    [BoxGroup("Panel"), Required, SerializeField] private CanvasGroup panelGroup;
    [BoxGroup("Buttons"), Required, SerializeField] private UnityEngine.UI.Button factoryA;
    [BoxGroup("Buttons"), Required, SerializeField] private UnityEngine.UI.Button factoryB;
    [BoxGroup("Buttons"), Required, SerializeField] private UnityEngine.UI.Button cable;
    [BoxGroup("Panel"), Required, SerializeField] private TMP_Text statusLabel;
    [BoxGroup("Animation"), MinValue(0.01f), SerializeField] private float slideDuration = 0.22f;
    [BoxGroup("Panel"), SerializeField] private BuildMenuListView menu;
    public event System.Action<BuildingDefinitionSO> BuildingSelected;
    public event System.Action CableSelected;
    [ShowInInspector, ReadOnly] public bool IsOpen { get; private set; }
    public event System.Action<int> Selected;
    private Tween slide;
    private float HiddenY => -panel.rect.height - 24f;

    private void Awake()
    {
        if (menu != null) menu.Initialize();
        panel.anchoredPosition = new Vector2(0f, HiddenY);
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        SetStatus("B | BUILD MENU");
    }

    private void OnEnable()
    {
        if (menu != null)
        {
            menu.BuildingSelected += OnBuilding;
            menu.CableSelected += OnCable;
            menu.Hovered += SetStatus;
        }
        else
        {
            factoryA.onClick.AddListener(SelectA);
            factoryB.onClick.AddListener(SelectB);
            cable.onClick.AddListener(SelectCable);
        }
    }

    private void OnDisable()
    {
        if (menu != null)
        {
            menu.BuildingSelected -= OnBuilding;
            menu.CableSelected -= OnCable;
            menu.Hovered -= SetStatus;
        }
        factoryA.onClick.RemoveListener(SelectA);
        factoryB.onClick.RemoveListener(SelectB);
        cable.onClick.RemoveListener(SelectCable);
        slide?.Kill();
        IsOpen = false;
        panel.anchoredPosition = new Vector2(0f, HiddenY);
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
    }

    private void OnBuilding(BuildingDefinitionSO definition) => BuildingSelected?.Invoke(definition);
    private void OnCable() => CableSelected?.Invoke();

    private void SelectA() => Selected?.Invoke(0);
    private void SelectB() => Selected?.Invoke(1);
    private void SelectCable() => Selected?.Invoke(2);

    public void SetOpen(bool open)
    {
        if (open && menu != null)
            foreach (var item in menu.Items) item.RefreshAvailability();
        IsOpen = open;
        slide?.Kill();
        panelGroup.interactable = open;
        // Closing panel still occludes world clicks during its slide.
        panelGroup.blocksRaycasts = true;
        if (!open && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        slide = panel.DOAnchorPosY(open ? 20f : HiddenY, slideDuration)
            .SetEase(open ? Ease.OutCubic : Ease.InCubic).SetUpdate(true)
            .OnComplete(() => panelGroup.blocksRaycasts = open);
    }

    public void SetStatus(string text)
    {
        if (statusLabel != null && statusLabel.text != text) statusLabel.text = text;
    }
}
