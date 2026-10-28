using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BuildMenuItemView : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text fallback;
    public BuildingDefinitionSO Definition { get; private set; }
    public bool IsCable { get; private set; }
    private Action<BuildingDefinitionSO> selected;
    private Action cableSelected;
    private Action<string> hovered;
    private string description;

    public void Bind(BuildingDefinitionSO definition, bool isCable, Action<BuildingDefinitionSO> select,
        Action selectCable, Action<string> showStatus)
    {
        Definition = definition;
        IsCable = isCable;
        selected = select;
        cableSelected = selectCable;
        hovered = showStatus;
        button.onClick.RemoveListener(Click);
        button.onClick.AddListener(Click);
        label.text = BuildMenuRules.Label(definition);
        if (!isCable)
        {
            icon.sprite = definition != null ? definition.Icon : null;
            icon.enabled = icon.sprite != null;
        }
        if (fallback != null)
        {
            fallback.gameObject.SetActive(!icon.enabled || icon.sprite == null);
            fallback.text = isCable ? "↗" : label.text.Length > 0 ? label.text.Substring(0, 1) : "?";
        }
        RefreshAvailability();
    }

    public void RefreshAvailability()
    {
        bool valid = BuildMenuRules.CanPlace(Definition, out string reason);
        button.interactable = IsCable || valid;
        description = IsCable ? "전선 연결 | 출발 포트와 도착 포트를 선택하세요." :
            valid ? label.text + " | 비용 " + Definition.BuildCost : label.text + " | " + reason;
        var color = icon.color;
        color.a = button.interactable ? 1f : 0.35f;
        icon.color = color;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered?.Invoke(description);

    private void Click()
    {
        if (!button.interactable) return;
        if (IsCable) cableSelected?.Invoke();
        else selected?.Invoke(Definition);
    }

    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Click); }
}
