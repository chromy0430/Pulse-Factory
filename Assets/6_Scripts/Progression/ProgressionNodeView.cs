using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PulseFactory.UI;

namespace PulseFactory.Progression
{
    public sealed class ProgressionNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerMoveHandler, ISelectHandler, IDeselectHandler, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        private ProgressionNodeSO node;
        private ProgressionState state;
        private ProgressionTooltip tooltip;
        private ProgressionMapScroll map;
        private GameObject icon,lockIcon;
        private Image badge;
        private TextMeshProUGUI cost,level;
        private bool dragging,selected;
        public bool Locked=>lockIcon!=null&&lockIcon.activeSelf;
        public void Initialize(ProgressionNodeSO value,ProgressionState progress,ProgressionTooltip tip,ProgressionMapScroll scroll)
        {
            node=value;state=progress;tooltip=tip;map=scroll;
            GetComponent<Image>().color=Color.clear;
            GetComponent<Button>().transition=Selectable.Transition.None;
            var label=GetComponentInChildren<TextMeshProUGUI>();
            PFUI.Top(label.rectTransform,new Vector2(0,111),new Vector2(160,24));label.fontSize=17;
            label.alignment=TextAlignmentOptions.Center;
            label.enableAutoSizing=true;label.fontSizeMin=12;label.fontSizeMax=17;
            label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;
            var rect=PFUI.Rect("NodeFrame",transform,new Vector2(47,41),new Vector2(66,66));
            badge=PFUI.Box(rect,"Panel",false);
            var image=PFUI.Icon("Icon",rect,"Core",new Vector2(11,11),44);
            image.sprite=PFUI.Theme!=null?PFUI.Theme.NodeIcon(node):node.icon;icon=image.gameObject;
            var lockImage=PFUI.Icon("Locked",rect,"Lock",new Vector2(18,14),30);lockIcon=lockImage.gameObject;
            if(PFUI.Theme==null){lockImage.enabled=false;var g=lockIcon.AddComponent<ProgressionLockGraphic>();g.raycastTarget=false;}
            level=PFUI.Text("Level",rect,"",new Vector2(29,49),new Vector2(32,14),10);level.alignment=TextAlignmentOptions.Right;
            cost=PFUI.Text("Cost",transform,"",new Vector2(0,135),new Vector2(160,18),13);cost.alignment=TextAlignmentOptions.Center;
            cost.enableAutoSizing=true;cost.fontSizeMin=9;cost.fontSizeMax=13;cost.textWrappingMode=TextWrappingModes.NoWrap;
            Refresh();
        }
        public void SetSelected(bool value){selected=value;Refresh();}
        public void Refresh()
        {
            bool locked=!state.CanAccess(node,out _),complete=state.Level(node)>=node.MaxLevel;
            lockIcon.SetActive(locked);icon.SetActive(!locked);
            PFUI.State(badge,selected,complete,locked);
            level.text=node.kind==ProgressionKind.Trait?$"Lv.{state.Level(node)}":"";
            cost.text=complete?"완료":node.researchCost>0?$"{node.Cost(state.Level(node)):N0} C · {node.Cost(state.Level(node),true):N0} RP":$"{node.Cost(state.Level(node)):N0} C";
            if(PFUI.Theme!=null)cost.color=complete?PFUI.Theme.mint:locked?PFUI.Theme.muted:PFUI.Theme.gold;
        }
        private void OnDisable(){if(tooltip!=null)tooltip.Hide(this);dragging=false;}
        public void Hover(Vector2 position)
        {
            if (!dragging && !map.Panning) tooltip.Show(this, node, state, position);
        }
        public void OnPointerEnter(PointerEventData e) => Hover(e.position);
        public void OnPointerExit(PointerEventData e)=>tooltip.Hide(this);
        public void OnPointerMove(PointerEventData e) => Hover(e.position);
        public void OnSelect(BaseEventData e){if(e is PointerEventData)return;tooltip.Show(this,node,state,RectTransformUtility.WorldToScreenPoint(null,transform.position));}
        public void OnDeselect(BaseEventData e)=>tooltip.Hide(this);
        public void OnInitializePotentialDrag(PointerEventData e)=>map.OnInitializePotentialDrag(e);
        public void OnBeginDrag(PointerEventData e){dragging=true;e.eligibleForClick=false;tooltip.Hide(this);map.OnBeginDrag(e);}
        public void OnDrag(PointerEventData e)=>map.OnDrag(e);
        public void OnEndDrag(PointerEventData e){map.OnEndDrag(e);dragging=false;}
        public void OnScroll(PointerEventData e)
        {
            if(tooltip.Scroll(e.scrollDelta.y))return;
            var ui=GetComponentInParent<ProgressionUI>();if(ui!=null)ui.SetZoom(ui.Zoom*Mathf.Pow(1.12f,e.scrollDelta.y));
        }
    }
}
