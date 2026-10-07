using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class BuildingPulseVisual : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Title(
        "Building Pulse Visual",
        "Pulse 수신 시 공장의 POP 연출을 재생합니다.")]

    [BoxGroup("References")]
    [Required]
    [LabelText("Scale Target")]
    [SerializeField]
    private Transform scaleTarget;

    [BoxGroup("References")]
    [Required]
    [LabelText("POP Effect")]
    [SerializeField]
    private ParticleSystem popEffect;


    // ============================================================
    // Scale Settings
    // ============================================================

    [BoxGroup("Scale Animation")]
    [LabelText("Scale Multiplier")]
    [MinValue(1f)]
    [SerializeField]
    private float scaleMultiplier = 1.12f;

    [BoxGroup("Scale Animation")]
    [LabelText("Scale Up Duration")]
    [SuffixLabel("sec", Overlay = true)]
    [MinValue(0.01f)]
    [SerializeField]
    private float scaleUpDuration = 0.08f;

    [BoxGroup("Scale Animation")]
    [LabelText("Return Duration")]
    [SuffixLabel("sec", Overlay = true)]
    [MinValue(0.01f)]
    [SerializeField]
    private float returnDuration = 0.12f;

    [BoxGroup("Scale Animation")]
    [LabelText("Scale Up Ease")]
    [SerializeField]
    private Ease scaleUpEase =
        Ease.OutQuad;

    [BoxGroup("Scale Animation")]
    [LabelText("Return Ease")]
    [SerializeField]
    private Ease returnEase =
        Ease.OutBack;


    // ============================================================
    // Runtime
    // ============================================================

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Playing")]
    private bool IsPlaying =>
        scaleSequence != null &&
        scaleSequence.IsActive() &&
        scaleSequence.IsPlaying();


    private Vector3 originalScale;

    private Sequence scaleSequence;


    // ============================================================
    // Unity
    // ============================================================

    private void Awake()
    {
        if (scaleTarget == null)
        {
            Debug.LogError(
                "[BuildingPulseVisual] Scale Target이 없습니다.",
                this);

            return;
        }

        originalScale =
            scaleTarget.localScale;
    }


    private void OnDisable()
    {
        KillScaleAnimation();

        if (scaleTarget != null)
        {
            scaleTarget.localScale =
                originalScale;
        }
    }


    // ============================================================
    // Pulse
    // ============================================================

    public void PlayPulse()
    {
        PlayScaleAnimation();
        PlayPopEffect();
    }


    private void PlayScaleAnimation()
    {
        if (scaleTarget == null)
            return;

        KillScaleAnimation();

        // 연속 Pulse가 들어와도 Scale이 누적되지 않게
        // 항상 원래 크기에서 시작
        scaleTarget.localScale =
            originalScale;

        scaleSequence =
            DOTween.Sequence();

        scaleSequence.Append(
            scaleTarget.DOScale(
                originalScale *
                scaleMultiplier,
                scaleUpDuration
            )
            .SetEase(scaleUpEase)
        );

        scaleSequence.Append(
            scaleTarget.DOScale(
                originalScale,
                returnDuration
            )
            .SetEase(returnEase)
        );

        scaleSequence.OnComplete(() =>
        {
            scaleTarget.localScale =
                originalScale;

            scaleSequence = null;
        });
    }


    private void PlayPopEffect()
    {
        if (popEffect == null)
            return;

        // 이전 POP이 남아있더라도
        // 처음부터 다시 재생
        popEffect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        popEffect.Play(true);
    }


    private void KillScaleAnimation()
    {
        if (scaleSequence == null)
            return;

        if (scaleSequence.IsActive())
        {
            scaleSequence.Kill();
        }

        scaleSequence = null;
    }


    // ============================================================
    // Debug
    // ============================================================

    [BoxGroup("Debug")]
    [Button(
        "TEST PULSE",
        ButtonSizes.Large)]
    [GUIColor(0.3f, 0.85f, 1f)]
    private void DebugPlayPulse()
    {
        if (!Application.isPlaying)
            return;

        PlayPulse();
    }
}