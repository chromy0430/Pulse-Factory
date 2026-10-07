using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PulseChargeUI : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Title(
        "Pulse Charge UI",
        "Pulse 충전량과 남은 시간을 표시합니다.")]

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private PulseTimer pulseTimer;

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private Image fillImage;

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private TMP_Text timeText;

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private TMP_Text chargeText;

    [BoxGroup("References")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private PulseEventChannelSO pulseEvent;


    // ============================================================
    // Shake
    // ============================================================

    [BoxGroup("Shake")]
    [LabelText("Shake Start Time")]
    [SuffixLabel("sec", Overlay = true)]
    [MinValue(0f)]
    [SerializeField]
    private float shakeStartTime = 3f;

    [BoxGroup("Shake")]
    [LabelText("Max Shake Distance")]
    [SuffixLabel("px", Overlay = true)]
    [MinValue(0f)]
    [SerializeField]
    private float maxShakeDistance = 5f;

    [BoxGroup("Shake")]
    [LabelText("Min Shake Speed")]
    [MinValue(0f)]
    [SerializeField]
    private float minShakeSpeed = 8f;

    [BoxGroup("Shake")]
    [LabelText("Max Shake Speed")]
    [MinValue(0f)]
    [SerializeField]
    private float maxShakeSpeed = 28f;


    // ============================================================
    // Pulse Animation
    // ============================================================

    [BoxGroup("Pulse Animation")]
    [LabelText("Punch Scale")]
    [MinValue(0f)]
    [SerializeField]
    private float punchScale = 0.18f;

    [BoxGroup("Pulse Animation")]
    [LabelText("Punch Duration")]
    [SuffixLabel("sec", Overlay = true)]
    [MinValue(0.01f)]
    [SerializeField]
    private float punchDuration = 0.2f;


    // ============================================================
    // Runtime
    // ============================================================

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [ProgressBar(0f, 1f)]
    private float Progress =>
        pulseTimer != null
            ? pulseTimer.Progress
            : 0f;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    private float RemainingTime =>
        pulseTimer != null
            ? pulseTimer.RemainingTime
            : 0f;


    private RectTransform timeRect;
    private Vector2 originalPosition;


    // ============================================================
    // Unity
    // ============================================================

    private void Awake()
    {
        if (timeText != null)
        {
            timeRect =
                timeText.rectTransform;

            originalPosition =
                timeRect.anchoredPosition;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }
    }


    private void OnEnable()
    {
        if (pulseEvent != null)
        {
            pulseEvent.Subscribe(OnPulse);
        }
    }


    private void OnDisable()
    {
        if (pulseEvent != null)
        {
            pulseEvent.Unsubscribe(OnPulse);
        }

        if (timeRect != null)
        {
            timeRect.DOKill();

            timeRect.anchoredPosition =
                originalPosition;

            timeRect.localScale =
                Vector3.one;
        }
    }


    private void LateUpdate()
    {
        if (pulseTimer == null)
            return;

        UpdateFill();
        UpdateTexts();
        UpdateShake();
    }


    // ============================================================
    // UI
    // ============================================================

    private void UpdateFill()
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount =
            pulseTimer.Progress;
    }


    private void UpdateTexts()
    {
        if (timeText != null)
        {
            int seconds =
                Mathf.CeilToInt(
                    pulseTimer.RemainingTime);

            timeText.text =
                $"{seconds:00}s";
        }


        if (chargeText != null)
        {
            int percent =
                Mathf.RoundToInt(
                    pulseTimer.Progress * 100f);

            chargeText.text =
                $"CHARGE {percent}%";
        }
    }


    // ============================================================
    // Shake
    // ============================================================

    private void UpdateShake()
    {
        if (timeRect == null)
            return;


        float remaining =
            pulseTimer.RemainingTime;


        // 아직 Pulse가 멀리 있으면 흔들지 않음
        if (remaining > shakeStartTime)
        {
            timeRect.anchoredPosition =
                originalPosition;

            return;
        }


        float shakeProgress =
            Mathf.InverseLerp(
                shakeStartTime,
                0f,
                remaining);


        // 막판으로 갈수록 더 강하게
        float strength =
            Mathf.Lerp(
                0f,
                maxShakeDistance,
                shakeProgress * shakeProgress);


        // 막판으로 갈수록 더 빠르게
        float speed =
            Mathf.Lerp(
                minShakeSpeed,
                maxShakeSpeed,
                shakeProgress);


        float time =
            Time.unscaledTime *
            speed;


        float x =
            (Mathf.PerlinNoise(
                time,
                0f) - 0.5f) * 2f;

        float y =
            (Mathf.PerlinNoise(
                0f,
                time) - 0.5f) * 2f;


        timeRect.anchoredPosition =
            originalPosition +
            new Vector2(
                x,
                y) *
            strength;
    }


    // ============================================================
    // Pulse
    // ============================================================

    private void OnPulse(
        PulseData pulse)
    {
        if (timeRect == null)
            return;


        timeRect.DOKill();


        // 흔들렸던 위치 즉시 초기화
        timeRect.anchoredPosition =
            originalPosition;

        timeRect.localScale =
            Vector3.one;


        // Pulse 순간 한 번 튀어오름
        timeRect.DOPunchScale(
            Vector3.one * punchScale,
            punchDuration,
            5,
            0.5f);
    }
}