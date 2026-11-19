using System;
using TMPro;
using UnityEngine;
using PulseFactory.Audio;
using UnityEngine.EventSystems;
using PulseFactory.UI;

public sealed class BuildMenuItemView : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text fallback;
    public BuildingDefinitionSO Definition { get; private set; }
    public bool IsCable { get; private set; }
    public static event Action<BuildingDefinitionSO> HoveredItem;
    private Action<BuildingDefinitionSO> selected;
    private Action cableSelected;
    private Action<string> hovered;
    private string description;
    private TextMeshProUGUI price, status;
    private UnityEngine.UI.Image stateIcon;
    private BuildController controller;
    private float nextRefresh;
    public void Bind(BuildingDefinitionSO definition, bool isCable, Action<BuildingDefinitionSO> select,
        Action selectCable, Action<string> showStatus)
    {
        Definition=definition; IsCable=isCable; selected=select;cableSelected=selectCable;hovered=showStatus;
        GameSounds.Bind(button);
        button.onClick.RemoveListener(Click);button.onClick.AddListener(Click);
        label.text=BuildMenuRules.Label(definition);
        icon.sprite=PFUI.Theme!=null?PFUI.Theme.BuildingIcon(definition):definition!=null?definition.Icon:icon.sprite;
        icon.enabled=icon.sprite!=null;
        if(fallback!=null){fallback.gameObject.SetActive(!icon.enabled);fallback.text=isCable?"↗":label.text.Length>0?label.text.Substring(0,1):"?";}
        if(PFUI.Theme!=null)Style();
        RefreshAvailability();
    }
    private void Style()
    {
        controller=FindFirstObjectByType<BuildController>();
        var root=(RectTransform)transform;root.sizeDelta=new Vector2(144,118);PFUI.Box(root);
        button.transition=UnityEngine.UI.Selectable.Transition.None;
        PFUI.Top(icon.rectTransform,new Vector2(12,12),new Vector2(36,36));
        PFUI.Top(label.rectTransform,new Vector2(54,8),new Vector2(82,45));
        label.font=PFUI.Theme.font;label.fontSize=16;label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.MidlineLeft;
        if(price==null)
        {
            price=PFUI.Text("Price",root,"",new Vector2(14,53),new Vector2(116,25),20);
            stateIcon=PFUI.Icon("State",root,"Check",new Vector2(12,89),18);
            status=PFUI.Text("Availability",root,"",new Vector2(36,86),new Vector2(100,24),14);
        }
    }
    private void Update()
    {
        if(price==null||Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+.15f;RefreshAvailability();
    }
    public void RefreshAvailability()
    {
        bool valid=BuildMenuRules.CanPlace(Definition,out string reason);
        button.interactable=IsCable||valid;
        description=IsCable?"전선 연결 | 출발 포트와 도착 포트를 선택하세요.":valid?label.text+" | 비용 "+Definition.BuildCost:label.text+" | "+reason;
        var color=icon.color;color.a=button.interactable?1f:.4f;icon.color=color;
        if(price==null)return;
        bool affordable=IsCable||ConstructionReceipt.CanPay(Definition!=null?Definition.BuildCost:0,out _);
        bool active=controller!=null&&(IsCable?controller.BuildMode=="Cable":controller.SelectedDefinition==Definition);
        price.text=IsCable?"포트 선택":$"{Definition.BuildCost:N0} C";price.color=affordable?PFUI.Theme.gold:PFUI.Theme.danger;
        status.text=!button.interactable?"연구 필요":!affordable?"크레딧 부족":active?"선택됨":"건설 가능";
        status.color=!button.interactable?PFUI.Theme.muted:!affordable?PFUI.Theme.danger:PFUI.Theme.mint;
        stateIcon.sprite=PFUI.Theme.Get(!button.interactable?"Lock":!affordable?"Close":"Check");stateIcon.color=status.color;
        PFUI.State(GetComponent<UnityEngine.UI.Image>(),active,false,!button.interactable);
    }
    public void OnPointerEnter(PointerEventData e){hovered?.Invoke(description);HoveredItem?.Invoke(Definition);}
    private void Click(){if(!button.interactable)return;if(IsCable)cableSelected?.Invoke();else selected?.Invoke(Definition);}
    private void OnDestroy(){if(button!=null)button.onClick.RemoveListener(Click);}
}
