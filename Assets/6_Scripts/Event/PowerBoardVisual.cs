using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class PowerBoardVisual : MonoBehaviour
{
    private static readonly int EmissionPowerID =
        Shader.PropertyToID("_EmissionPower");

    [Title("Power Board Visual")]

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private Renderer targetRenderer;

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private PulseTimer pulseTimer;

    [BoxGroup("References")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private PulseEventChannelSO pulseEvent;


    [BoxGroup("Charge Settings")]
    [MinValue(0f)]
    [SerializeField]
    private float minEmissionPower = 0.1f;

    [BoxGroup("Charge Settings")]
    [MinValue(0f)]
    [SerializeField]
    private float chargedEmissionPower = 2f;

    [BoxGroup("Charge Settings")]
    [SerializeField]
    private AnimationCurve chargeCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f);


    [BoxGroup("Pulse Settings")]
    [MinValue(0f)]
    [SerializeField]
    private float pulseEmissionPower = 8f;

    [BoxGroup("Pulse Settings")]
    [SuffixLabel("sec", Overlay = true)]
    [PropertyRange(0.01f, 2f)]
    [SerializeField]
    private float pulseDuration = 0.25f;


    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [ProgressBar(0f, 1f)]
    private float ChargeProgress =>
        pulseTimer != null
            ? pulseTimer.Progress
            : 0f;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    private float currentEmissionPower;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    private bool isPulsing;


    private MaterialPropertyBlock propertyBlock;

    private Coroutine pulseRoutine;


    private void Reset()
    {
        targetRenderer =
            GetComponent<Renderer>();
    }

    private void Awake()
    {
        propertyBlock =
            new MaterialPropertyBlock();

        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        SetEmissionPower(
            minEmissionPower);
    }

    private void OnEnable()
    {
        if (pulseEvent != null)
            pulseEvent.Subscribe(OnPulse);
    }

    private void OnDisable()
    {
        if (pulseEvent != null)
            pulseEvent.Unsubscribe(OnPulse);

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        isPulsing = false;
    }

    private void Update()
    {
        if (pulseTimer == null ||
            isPulsing)
            return;

        float curve =
            chargeCurve.Evaluate(
                pulseTimer.Progress);

        float emission =
            Mathf.Lerp(
                minEmissionPower,
                chargedEmissionPower,
                curve);

        SetEmissionPower(emission);
    }

    private void OnPulse(
        PulseData pulse)
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine =
            StartCoroutine(
                PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        isPulsing = true;

        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / pulseDuration);

            SetEmissionPower(
                Mathf.Lerp(
                    pulseEmissionPower,
                    minEmissionPower,
                    t));

            yield return null;
        }

        SetEmissionPower(
            minEmissionPower);

        isPulsing = false;
        pulseRoutine = null;
    }

    private void SetEmissionPower(
        float value)
    {
        currentEmissionPower = value;

        if (targetRenderer == null)
            return;

        targetRenderer.GetPropertyBlock(
            propertyBlock);

        propertyBlock.SetFloat(
            EmissionPowerID,
            value);

        targetRenderer.SetPropertyBlock(
            propertyBlock);
    }
}