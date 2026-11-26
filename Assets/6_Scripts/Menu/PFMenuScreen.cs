using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PulseFactory.Audio;

namespace PulseFactory.Menu
{
    [DefaultExecutionOrder(-500)]
    public sealed class PFMenuScreen : MonoBehaviour
    {
        public bool titleScene;
        public GameObject title, dialog;
        public TMP_Text status, heading, subtitle;
        public Button resume, home, settings, quit, close, startNew, continueGame, titleOptions, titleQuit, save;
        public PFOptionsPanel options;
        private float previousScale, confirmNewUntil;
        private readonly System.Collections.Generic.List<(CanvasGroup group,bool interactable)> blockedCanvases=new();
        private bool open, leaving, restored;
        private static int consumedFrame=-1;
        private static bool loadOnStart;
        private static string message;
        private InputAction escape;
        private bool escapeRequested, escapeHeld;
        private void OnEnable()
        {
            active=this;
            escape=new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            escape.performed+=OnEscape;escape.Enable();
        }
        private void OnEscape(InputAction.CallbackContext context) => escapeRequested=true;
        private static PFMenuScreen active;
        public static bool BlocksGameplay => active && (active.open || active.leaving || Time.frameCount==consumedFrame);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {active=null;loadOnStart=false;message=null;consumedFrame=-1;}
        private void Awake()
        {
            active=this; dialog.SetActive(false);
            Bind(resume,Close); Bind(close,Close); Bind(home,ReturnHome); Bind(settings,()=>options.Tab(0));
            Bind(save,()=>SaveWorld()); Bind(quit,Quit); Bind(titleQuit,Quit);
            Bind(startNew,()=>Enter(false)); Bind(continueGame,()=>Enter(true)); Bind(titleOptions,Open);
            if (titleScene)
            {
                continueGame.interactable=PFLocalSave.Exists;
                status.text=message ?? (continueGame.interactable?"저장된 공장 불러오기":"저장된 공장이 없습니다");
                message=null;
            }
        }
        private IEnumerator Start()
        {
            yield return null;
            if (titleScene) { Select(continueGame.interactable?continueGame:startNew); yield break; }
            if (loadOnStart)
            {
                loadOnStart=false;
                try { PFLocalSave.Load(); }
                catch (Exception ex) {message="불러오기 실패 · "+ex.Message;leaving=true;SceneManager.LoadScene("Menu");yield break;}
            }
            restored=true;
        }
        private static void Bind(Button b,UnityEngine.Events.UnityAction a)
        {
            if (!b) return;
            GameSounds.Bind(b); b.onClick.AddListener(a);
        }
        private static void Select(Button b)
        {
            if (b && EventSystem.current) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
        private void Update()
        {
            bool down=Keyboard.current!=null && Keyboard.current.escapeKey.isPressed;
            bool pressed=escapeRequested || (down && !escapeHeld);
            escapeHeld=down;
            if (!pressed) return;
            escapeRequested=false;
            if (leaving) return;
            if (open) Close(); else Open();
        }
        public void Open()
        {
            if (open || leaving) return;
            consumedFrame=Time.frameCount; open=true;
            previousScale=Time.timeScale;
            if (!titleScene)
            {
                Time.timeScale=0;
                foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.gameObject==gameObject) continue;
                    if (!canvas.TryGetComponent<CanvasGroup>(out var group))
                        group=canvas.gameObject.AddComponent<CanvasGroup>();
                    blockedCanvases.Add((group,group.interactable)); group.interactable=false;
                }
            }
            if (title) title.SetActive(false);
            dialog.SetActive(true); options.BeginEdit();
            heading.text=titleScene?"설정":"일시정지";
            subtitle.text=titleScene?"메인 메뉴":"게임이 일시정지되었습니다";
            home.gameObject.SetActive(!titleScene); save.gameObject.SetActive(!titleScene);
            resume.GetComponentInChildren<TMP_Text>().text=titleScene?"돌아가기":"게임 재개";
            if(titleScene) ((RectTransform)quit.transform).anchoredPosition=((RectTransform)save.transform).anchoredPosition;
            Select(resume);
        }
        public void Close()
        {
            if (!open) return;
            consumedFrame=Time.frameCount;open=false;
            if (!titleScene) Time.timeScale=previousScale;
            foreach(var item in blockedCanvases) if(item.group) item.group.interactable=item.interactable;
            blockedCanvases.Clear();
            dialog.SetActive(false);
            if (title) {title.SetActive(true);Select(titleOptions);}
            else if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        public void Enter(bool load)
        {
            if (leaving || (load && !PFLocalSave.Exists)) return;
            if (!load && PFLocalSave.Exists && Time.unscaledTime>=confirmNewUntil)
            {
                confirmNewUntil=Time.unscaledTime+6;
                status.text="다시 누르면 새 게임을 시작합니다. 저장 시 기존 공장을 덮어씁니다.";
                return;
            }
            leaving=true;loadOnStart=load;Time.timeScale=1;SceneManager.LoadScene("Main");
        }
        private bool SaveWorld()
        {
            if (!restored) return false;
            try { PFLocalSave.Save();options.Feedback("공장을 저장했습니다");return true; }
            catch(Exception ex){options.Feedback("저장 실패 · "+ex.Message);return false;}
        }
        public void ReturnHome()
        {
            if (leaving || !SaveWorld()) return;
            Close();leaving=true;SceneManager.LoadScene("Menu");
        }
        public void Quit()
        {
            if (!titleScene && !SaveWorld()) return;
            Close();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void OnDisable()
        {
            escape?.Dispose();escape=null;escapeRequested=escapeHeld=false;
            if (open) Close();
            if (active==this) active=null;
        }
    }
}
