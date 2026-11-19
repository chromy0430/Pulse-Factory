using TMPro;
using UnityEngine;
using PulseFactory.Progression;

namespace PulseFactory.UI
{
    public sealed class PFBuildPresentation : MonoBehaviour
    {
        private BuildController build;
        private BuildPanelView panel;
        private Canvas canvas;
        private RectTransform sidebar, statusBox;
        private TextMeshProUGUI title, description, facts, availability, status;
        private UnityEngine.UI.Image icon;
        private BuildingDefinitionSO hovered;
        private float nextRefresh;
        private GameObject originalStatus;
        private bool originalStatusActive;
        private float originalOpenOffset;
        public void Initialize(BuildController controller, BuildPanelView view)
        {
            build=controller;panel=view; originalOpenOffset=panel.OpenOffset; panel.OpenOffset=78;
            var panelRect=panel.PanelRect;
            var fitter=panelRect.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if(fitter!=null)fitter.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            panelRect.anchorMin=new Vector2(0,0);panelRect.anchorMax=new Vector2(1,0);panelRect.sizeDelta=new Vector2(-20,panelRect.sizeDelta.y);
            PFUI.Box(panelRect);
            var canvasScaler=panel.GetComponent<UnityEngine.UI.CanvasScaler>();
            if(canvasScaler!=null){canvasScaler.referenceResolution=new Vector2(1280,720);canvasScaler.matchWidthOrHeight=.5f;}
            var heading=panelRect.Find("Title")?.GetComponent<TMP_Text>();
            if(heading!=null){heading.text="건설할 공장 선택";heading.fontSize=22;heading.font=PFUI.Theme.font;}
            var row=panelRect.Find("MenuRow");
            if(row!=null)
            {
                var rowRect=(RectTransform)row;rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,132);
                var rowSize=row.GetComponent<UnityEngine.UI.LayoutElement>()??row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();rowSize.minHeight=rowSize.preferredHeight=132;
                var viewport=row.Find("Viewport"); if(viewport!=null)viewport.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,0);
                if(viewport!=null){var size=viewport.GetComponent<UnityEngine.UI.LayoutElement>();size.minHeight=size.preferredHeight=132;}
                var scroller=row.GetComponent<BuildMenuScroller>(); if(scroller!=null)scroller.ApplyPresentation(144,8);
                foreach(var button in row.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    if(button.name=="Previous"||button.name=="Next")PFUI.Box((RectTransform)button.transform);
            }
            var oldStatus=panel.transform.Find("BuildStatusBackground");if(oldStatus!=null){originalStatus=oldStatus.gameObject;originalStatusActive=originalStatus.activeSelf;originalStatus.SetActive(false);}
            var root=PFUI.Rect("BuildDetailsCanvas",transform,Vector2.zero,Vector2.zero);
            canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=60;
            var scale=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scale.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            sidebar=PFUI.Rect("BuildingDetails",root,Vector2.zero,new Vector2(288,326));
            sidebar.anchorMin=sidebar.anchorMax=sidebar.pivot=new Vector2(1,1);sidebar.anchoredPosition=new Vector2(-10,-110);PFUI.Box(sidebar);
            title=PFUI.Text("Title",sidebar,"",new Vector2(18,12),new Vector2(252,34),26);
            icon=PFUI.Icon("Icon",sidebar,"Factory",new Vector2(18,60),56);
            description=PFUI.Text("Description",sidebar,"",new Vector2(88,57),new Vector2(180,64),16);description.fontStyle=FontStyles.Normal;
            facts=PFUI.Text("Facts",sidebar,"",new Vector2(20,132),new Vector2(248,99),17);facts.lineSpacing=10;
            availability=PFUI.Text("Availability",sidebar,"",new Vector2(20,238),new Vector2(248,84),16);
            statusBox=PFUI.Rect("BuildStatus",root,new Vector2(10,110),new Vector2(620,38));PFUI.Box(statusBox);
            status=PFUI.Text("Text",statusBox,"",new Vector2(14,3),new Vector2(592,32),17);
            status.enableAutoSizing=true;status.fontSizeMin=12;status.fontSizeMax=17;
            status.textWrappingMode=TextWrappingModes.NoWrap;status.overflowMode=TextOverflowModes.Ellipsis;
            BuildMenuItemView.HoveredItem+=Hover;
        }
        private void Hover(BuildingDefinitionSO value) => hovered=value;
        private void Update()
        {
            if(canvas==null)return;
            var hud=GetComponent<PFHUD>();canvas.enabled=!ProgressionUI.IsOpen && hud!=null && hud.enabled;
            if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;
            var definition=build.SelectedDefinition!=null?build.SelectedDefinition:panel.IsOpen?hovered:null;
            sidebar.gameObject.SetActive(definition!=null);
            statusBox.gameObject.SetActive(build.BuildMode!="Idle" && !panel.IsOpen);
            status.text=panel.Status;
            if(definition==null)return;
            title.text=BuildMenuRules.Label(definition);icon.sprite=PFUI.Theme.BuildingIcon(definition);
            description.text=definition.name.Contains("Research")?"전력을 받으면\n연구를 지원합니다.":definition.name.Contains("Amplifier")?"다음 공장으로 보내는\n펄스를 증폭합니다.":definition.name.Contains("Capacitor")?"펄스를 저장한 뒤\n모아서 방출합니다.":"펄스를 받아 설정된\n생산 효과를 실행합니다.";
            float refund=Mathf.Clamp01(.5f+(ProgressionState.Instance?.Total(ProgressionEffect.RefundRatio)??0));
            int baseProduction=0;
            foreach(var effect in definition.Effects)if(effect is CreditProductionEffectSO production)baseProduction+=production.BaseCreditPerPulse;
            facts.text=$"건설 비용          <color=#FFCF64>{definition.BuildCost:N0} C</color>\n입력 / 출력        {definition.MaxInputs} / {definition.MaxOutputs}\n철거 환급          {refund:P0}";
            if(baseProduction>0)description.text=$"전력 1의 펄스당\n기본 {baseProduction:N0} C 생산";
            bool unlocked=BuildMenuRules.CanPlace(definition,out var reason);
            bool affordable=ConstructionReceipt.CanPay(definition.BuildCost,out var costReason);
            var policy=build.GetComponent<ProgressionBuildPolicy>();string capacityReason=null;
            bool room=policy==null||policy.CanBuild(out capacityReason);
            availability.color=unlocked&&affordable&&room?PFUI.Theme.mint:PFUI.Theme.danger;
            availability.text=!unlocked?reason:!affordable?costReason:!room?capacityReason:build.SelectedDefinition!=null?build.PlacementStatus:"크레딧 충분 · 수용량 여유\n배치할 위치를 선택하세요.";
            availability.text+="\n배치 후 전선 연결 필요";
        }
        private void OnDestroy(){BuildMenuItemView.HoveredItem-=Hover;if(canvas!=null)Destroy(canvas.gameObject);}
        private void OnDisable()
        {
            if(canvas!=null)canvas.enabled=false;
            if(originalStatus!=null)originalStatus.SetActive(originalStatusActive);
            if(panel!=null)panel.OpenOffset=originalOpenOffset;
        }
        private void OnEnable()
        {
            if(originalStatus!=null)originalStatus.SetActive(false);
            if(panel!=null)panel.OpenOffset=78;
        }
    }
}
