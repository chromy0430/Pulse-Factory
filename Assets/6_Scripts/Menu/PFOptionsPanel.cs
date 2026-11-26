using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PulseFactory.Audio;
namespace PulseFactory.Menu
{
    public sealed class PFOptionsPanel : MonoBehaviour
    {
        public Button[] tabs;
        public GameObject[] pages;
        public Slider master,music,effects,zoom;
        public TMP_Text[] values;
        public Toggle mute,fullscreen,invert;
        public Button apply,defaults;
        public TMP_Text feedback;
        private PFPreferences draft;
        private void Awake()
        {
            for(int i=0;i<tabs.Length;i++){int k=i;GameSounds.Bind(tabs[i]);tabs[i].onClick.AddListener(()=>Tab(k));}
            GameSounds.Bind(apply);GameSounds.Bind(defaults);
            apply.onClick.AddListener(Apply);defaults.onClick.AddListener(()=>Display(PFPreferences.Defaults()));
            master.onValueChanged.AddListener(_=>Labels());music.onValueChanged.AddListener(_=>Labels());
            effects.onValueChanged.AddListener(_=>Labels());zoom.onValueChanged.AddListener(_=>Labels());
        }
        public void BeginEdit(){Display(PFPreferences.Current.Copy());Tab(0); Feedback("");}
        public void Tab(int index)
        {
            for(int i=0;i<pages.Length;i++)
            {
                pages[i].SetActive(i==index);
                PulseFactory.UI.PFUI.State(tabs[i].GetComponent<Image>(),i==index);
            }
        }
        private void Display(PFPreferences value)
        {
            draft=value;master.value=value.master;music.value=value.music;effects.value=value.effects;
            zoom.value=value.zoom;mute.isOn=value.mute;fullscreen.isOn=value.fullscreen;invert.isOn=value.invertWheel;Labels();
        }
        private void Labels()
        {
            values[0].text=master.value.ToString("P0");values[1].text=music.value.ToString("P0");
            values[2].text=effects.value.ToString("P0");values[3].text=zoom.value.ToString("0.0")+"×";
        }
        private void Apply()
        {
            draft.master=master.value;draft.music=music.value;draft.effects=effects.value;draft.mute=mute.isOn;
            draft.fullscreen=fullscreen.isOn;draft.zoom=zoom.value;draft.invertWheel=invert.isOn;
            draft.Apply();Feedback("설정을 적용했습니다");
        }
        public void Feedback(string value)=>feedback.text=value;
    }
}
