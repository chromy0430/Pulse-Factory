using System.Collections;
using TMPro;
using UnityEngine;
using PulseFactory.Progression;

namespace PulseFactory.UI
{
    public sealed class PFHUD : MonoBehaviour
    {
        private BuildController build;
        private BuildPanelView panel;
        private ProgressionUI progression;
        private ProgressionBuildPolicy policy;
        private PulseTimer timer;
        private Canvas canvas;
        private TextMeshProUGUI credits, capacity, research, pulseTime, hint;
        private readonly UnityEngine.UI.Image[] segments = new UnityEngine.UI.Image[30];
        private readonly UnityEngine.UI.Image[] tabs = new UnityEngine.UI.Image[5];
        private Canvas[] legacy;
        private bool[] legacyEnabled;
        private GameObject[] oldShortcuts;
        private int filled = -1;
        private float nextRefresh;
        private string mode;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PFUI.Theme == null || FindFirstObjectByType<BuildController>() == null || FindFirstObjectByType<PFHUD>() != null) return;
            new GameObject("PF Interface").AddComponent<PFHUD>();
        }
        private IEnumerator Start()
        {
            yield return null;
            build = FindFirstObjectByType<BuildController>(); panel = FindFirstObjectByType<BuildPanelView>();
            progression = FindFirstObjectByType<ProgressionUI>(); timer = FindFirstObjectByType<PulseTimer>();
            policy = build != null ? build.GetComponent<ProgressionBuildPolicy>() : null;
            if (build == null || panel == null) { enabled = false; yield break; }
            Create();
            var list = new System.Collections.Generic.List<Canvas>();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == "ResourceBar_Canvas" || c.name == "Charge_Canvas") list.Add(c);
            legacy = list.ToArray(); legacyEnabled = new bool[legacy.Length];
            for (int i = 0; i < legacy.Length; i++) { legacyEnabled[i] = legacy[i].enabled; legacy[i].enabled = false; }
            oldShortcuts = new GameObject[2];
            if (progression != null)
            {
                var pc = progression.transform.Find("ProgressionCanvas");
                for (int i = 0; i < 2; i++)
                { var t = pc != null ? pc.Find(i == 0 ? "Research" : "Traits") : null; if (t != null) { oldShortcuts[i] = t.gameObject; t.gameObject.SetActive(false); } }
            }
            gameObject.AddComponent<PFBuildPresentation>().Initialize(build, panel);
            canvas.gameObject.AddComponent<CablePopTooltip>().Initialize((RectTransform)canvas.transform, build);
        }
        private void Create()
        {
            var root = PFUI.Rect("HUD", transform, Vector2.zero, Vector2.zero);
            canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var top = PFUI.Rect("StatusBar", root, Vector2.zero, Vector2.zero);
            PFUI.Stretch(top, new Vector2(0,1), Vector2.one, new Vector2(10,-54), new Vector2(-10,-8));
            Header(top, "Brand", 0, .28f, "Pulse", "PULSE FACTORY");
            credits = Header(top, "Credits", .28f, .53f, "Credit", "크레딧", "0");
            capacity = Header(top, "Capacity", .53f, .77f, "Factory", "공장 수용량", "0 / 3");
            research = Header(top, "ResearchPoints", .77f, 1, "Research", "연구 포인트", "0");
            credits.color = PFUI.Theme.gold; capacity.color = research.color = PFUI.Theme.mint;
            var pulse = PFUI.Rect("PulseBar", root, Vector2.zero, Vector2.zero);
            PFUI.Stretch(pulse, new Vector2(0,1), Vector2.one, new Vector2(10,-96), new Vector2(-10,-62)); PFUI.Box(pulse);
            PFUI.Text("Title", pulse, "다음 펄스", new Vector2(16,0), new Vector2(120,34),20);
            var bar = PFUI.Rect("Segments",pulse,Vector2.zero,Vector2.zero);
            PFUI.Stretch(bar,Vector2.zero,Vector2.one,new Vector2(146,8),new Vector2(-210,-8));
            for (int i=0;i<segments.Length;i++)
            {
                var r=PFUI.Rect("Segment"+i,bar,Vector2.zero,Vector2.zero);
                PFUI.Stretch(r,new Vector2(i/30f,0),new Vector2((i+1)/30f,1),new Vector2(2,0),new Vector2(-2,0));
                segments[i]=PFUI.Box(r,"Segment",false); segments[i].type=UnityEngine.UI.Image.Type.Simple;
            }
            pulseTime=PFUI.Text("Time",pulse,"",Vector2.zero,Vector2.zero,19);
            PFUI.Stretch(pulseTime.rectTransform,new Vector2(1,0),Vector2.one,new Vector2(-198,0),new Vector2(-14,0));
            pulseTime.alignment=TextAlignmentOptions.Right;
            var nav=PFUI.Rect("Navigation",root,Vector2.zero,Vector2.zero);
            PFUI.Stretch(nav,Vector2.zero,new Vector2(.75f,0),new Vector2(10,10),new Vector2(-8,60)); PFUI.Box(nav);
            string[] labels={"건설 [B]","연결","철거 [X]","연구 [R]","특성 [T]"};
            string[] icons={"Build","Cable","Trash","Research","Star"};
            for(int i=0;i<5;i++)
            {
                int index=i; var b=PFUI.Button(icons[i],nav,labels[i],Vector2.zero,new Vector2(170,40),()=>Navigate(index),icons[i]);
                PFUI.Stretch((RectTransform)b.transform,new Vector2(i/5f,0),new Vector2((i+1)/5f,1),new Vector2(7,5),new Vector2(-7,-5));
                tabs[i]=b.GetComponent<UnityEngine.UI.Image>();
                var label=b.GetComponentInChildren<TextMeshProUGUI>();
                PFUI.Stretch(label.rectTransform,Vector2.zero,Vector2.one,new Vector2(45,3),new Vector2(-5,-3)); label.fontSize=17;
            }
            var hintBox=PFUI.Rect("InputHint",root,Vector2.zero,Vector2.zero);
            PFUI.Stretch(hintBox,Vector2.zero,new Vector2(1,0),new Vector2(0,10),new Vector2(-10,60));
            hintBox.anchorMin=new Vector2(.75f,0); hintBox.offsetMin=new Vector2(0,10); PFUI.Box(hintBox);
            hint=PFUI.Text("Hint",hintBox,"",Vector2.zero,Vector2.zero,14);
            PFUI.Stretch(hint.rectTransform,Vector2.zero,Vector2.one,new Vector2(14,5),new Vector2(-14,-5));
        }
        private TextMeshProUGUI Header(RectTransform parent,string name,float min,float max,string icon,string label,string value=null)
        {
            var box=PFUI.Rect(name,parent,Vector2.zero,Vector2.zero);
            PFUI.Stretch(box,new Vector2(min,0),new Vector2(max,1),new Vector2(0,0),new Vector2(-8,0)); PFUI.Box(box);
            PFUI.Icon("Icon",box,icon,new Vector2(13,8),30);
            PFUI.Text("Label",box,label,new Vector2(52,0),new Vector2(value==null?260:135,46),19);
            if(value==null)return null;
            var t=PFUI.Text("Value",box,value,Vector2.zero,Vector2.zero,23);
            PFUI.Stretch(t.rectTransform,new Vector2(.64f,0),Vector2.one,new Vector2(0,0),new Vector2(-15,0));t.alignment=TextAlignmentOptions.Right;
            t.enableAutoSizing=true;t.fontSizeMin=13;t.fontSizeMax=23;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;
            return t;
        }
        public void Navigate(int index)
        {
            if(index>=3){panel.SetOpen(false);progression?.Open(index==3?ProgressionKind.Research:ProgressionKind.Trait);return;}
            progression?.Close();
            if(index==0)build.ToggleMenu(); else if(index==1)build.SelectCable(); else build.ToggleDemolition();
        }
        private void Update()
        {
            if(canvas==null||!isActiveAndEnabled)return;
            int count=Mathf.Clamp(Mathf.FloorToInt((timer!=null?timer.Progress:0)*segments.Length),0,segments.Length);
            if(count!=filled){filled=count;for(int i=0;i<segments.Length;i++)segments[i].color=i<count?PFUI.Theme.mint:new Color(.25f,.3f,.33f);}
            if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;
            credits.text=$"{CreditWallet.Instance?.CurrentCredit??0:N0}";
            research.text=$"{ProgressionState.Instance?.ResearchPoints??0:N0}";
            int used=0; if(policy!=null&&policy.buildings!=null)foreach(Transform t in policy.buildings)if(t.gameObject.activeSelf&&t.GetComponent<BuildingRuntime>()!=null)used++;
            int max=(policy!=null?policy.baseCapacity:0)+Mathf.FloorToInt(ProgressionState.Instance?.Total(ProgressionEffect.Capacity)??0);
            capacity.text=$"{used} / {max}";
            pulseTime.text=timer!=null?$"{timer.RemainingTime:00.0} s / {timer.CurrentInterval:0} s":"—";
            string current=ProgressionUI.IsOpen?(progression.CurrentKind==ProgressionKind.Research?"Research":"Trait"):build.BuildMode;
            if(current!=mode){mode=current; for(int i=0;i<5;i++)PFUI.State(tabs[i],i==(mode=="Research"?3:mode=="Trait"?4:mode=="Demolition"?2:mode=="Cable"?1:mode=="Menu"||mode=="Factory"?0:-1));}
            hint.text=ProgressionUI.IsOpen?"드래그 이동 · 휠 확대/축소\n노드 선택 · ESC 닫기":mode=="Cable"?"포트 선택 · Tab 후보 전환\n우클릭 / ESC 취소":mode=="Factory"?"좌클릭 배치\n우클릭 / ESC 취소":mode=="Demolition"?"대상 클릭 철거\nESC 취소":"B 건설 · R 연구 · T 특성\nX 철거";
        }
        private void OnDisable()
        {
            var presentation=GetComponent<PFBuildPresentation>();if(presentation!=null)presentation.enabled=false;
            if(canvas!=null)canvas.gameObject.SetActive(false);
            if(legacy!=null)for(int i=0;i<legacy.Length;i++)if(legacy[i]!=null)legacy[i].enabled=legacyEnabled[i];
            if(oldShortcuts!=null)foreach(var shortcut in oldShortcuts)if(shortcut!=null)shortcut.SetActive(true);
        }
        private void OnEnable()
        {
            var presentation=GetComponent<PFBuildPresentation>();if(presentation!=null)presentation.enabled=true;
            if(canvas!=null)canvas.gameObject.SetActive(true);
            if(legacy!=null)foreach(var c in legacy)if(c!=null)c.enabled=false;
            if(oldShortcuts!=null)foreach(var shortcut in oldShortcuts)if(shortcut!=null)shortcut.SetActive(false);
        }
    }
}
