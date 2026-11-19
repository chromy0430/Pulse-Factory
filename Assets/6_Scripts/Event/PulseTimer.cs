using Sirenix.OdinInspector;
using UnityEngine;
using PulseFactory.Audio;

public class PulseTimer : MonoBehaviour
{
    [Title(
        "Pulse Timer",
        "전역 Pulse 주기를 관리합니다.")]

    [BoxGroup("Data")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private PulseSettingsSO settings;

    [BoxGroup("Data")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private PulseEventChannelSO pulseEvent;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [SuffixLabel("sec", Overlay = true)]
    public float CurrentInterval { get; private set; }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [SuffixLabel("sec", Overlay = true)]
    public float ElapsedTime { get; private set; }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [SuffixLabel("sec", Overlay = true)]
    public float RemainingTime =>
        Mathf.Max(0f, CurrentInterval - ElapsedTime);

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [ProgressBar(0f, 1f)]
    [LabelText("Charge Progress")]
    public float Progress =>
        CurrentInterval > 0f
            ? Mathf.Clamp01(
                ElapsedTime / CurrentInterval)
            : 0f;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    public int PulseCount { get; private set; }

    private void Awake()
    {
        if (settings == null || pulseEvent == null)
        {
            Debug.LogError(
                "[PulseTimer] Settings 또는 Event Channel이 없습니다.",
                this);

            enabled = false;
            return;
        }

        CurrentInterval =
            settings.BaseInterval;
    }

    private bool chargeCuePlayed;

    private void Update()
    {
        ElapsedTime += Time.deltaTime;

        // The charge sound is the final 0.88 seconds of the board's charge cycle.
        if (!chargeCuePlayed && RemainingTime <= 0.88f)
        {
            GameSounds.Play(GameSounds.Charge);
            chargeCuePlayed = true;
        }

        if (ElapsedTime < CurrentInterval)
            return;

        ElapsedTime -= CurrentInterval;

        ExecutePulse();
    }

    private void ExecutePulse()
    {
        chargeCuePlayed = false;
        PulseCount++;

        PulseData pulse =
            new PulseData(
                PulseCount,
                settings.BasePower);

        pulseEvent.Raise(pulse);
    }

    public void SetInterval(float interval)
    {
        CurrentInterval =
            Mathf.Clamp(
                interval,
                settings.MinimumInterval,
                settings.BaseInterval);

        ElapsedTime =
            Mathf.Min(
                ElapsedTime,
                CurrentInterval);
    }

    [BoxGroup("Debug")]
    [Button("FORCE PULSE", ButtonSizes.Large)]
    [GUIColor(0.3f, 0.85f, 1f)]
    private void DebugPulse()
    {
        ExecutePulse();
    }

    [BoxGroup("Debug")]
    [Button("Reset Timer")]
    private void ResetTimer()
    {
        chargeCuePlayed = false;
        ElapsedTime = 0f;
    }
}