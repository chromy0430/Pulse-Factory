using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using PulseFactory.Audio;

[RequireComponent(typeof(LineRenderer))]
public class PowerCable : MonoBehaviour
{
    [Title("Power Cable")]


    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private LineRenderer lineRenderer;

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private Transform pulseVisual;

    [BoxGroup("References"), SerializeField]
    [Tooltip("선택 사항. 중앙을 원점으로 길이와 지름을 1로 맞춘 모델입니다. +Z가 도착 포트를 향합니다.")]
    private Transform modelSpan;


    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    public PowerCableDefinitionSO Definition { get; private set; }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    public PowerNode SourceNode { get; private set; }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    public PowerNode TargetNode { get; private set; }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Source Port")]
    public PowerPortSlot SourcePort { get; private set; }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Target Port")]
    public PowerPortSlot TargetPort { get; private set; }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Length")]
    [SuffixLabel("m", Overlay = true)]
    public float Length
    {
        get
        {
            if (SourcePort?.Anchor == null ||
                TargetPort?.Anchor == null)
            {
                return 0f;
            }

            return Vector3.Distance(
                SourcePort.Anchor.position,
                TargetPort.Anchor.position);
        }
    }

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Build Cost")]
    public int BuildCost =>
        Definition != null
            ? Definition.GetBuildCost(Length)
            : 0;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Transmitting")]
    public bool IsTransmitting =>
        pulseRoutine != null;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Queued Pulses")]
    private int QueuedPulseCount =>
        pulseQueue.Count;

    private void OnEnable() => PowerCablePicker.Register(this);
    private void OnDisable() => PowerCablePicker.Unregister(this);

    private readonly Queue<PulseData> pulseQueue =
        new();

    private Coroutine pulseRoutine;


    private void Reset()
    {
        lineRenderer =
            GetComponent<LineRenderer>();
    }

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer =
                GetComponent<LineRenderer>();
        }

        if (pulseVisual != null)
        {
            pulseVisual.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        SourcePort?.Disconnect(this);
        TargetPort?.Disconnect(this);

        if (SourceNode != null)
        {
            SourceNode.UnregisterOutgoingCable(
                this);
        }

        if (TargetNode != null)
        {
            TargetNode.UnregisterIncomingCable(
                this);
        }
    }


    public void Initialize(
        PowerCableDefinitionSO definition,
        PowerNode sourceNode,
        PowerNode targetNode,
        PowerPortSlot sourcePort,
        PowerPortSlot targetPort)
    {
        Definition = definition;

        SourceNode = sourceNode;
        TargetNode = targetNode;

        SourcePort = sourcePort;
        TargetPort = targetPort;

        ApplyDefinition();
        RefreshVisual();
    }

    private void ApplyDefinition()
    {
        if (Definition == null || lineRenderer == null) return;

        lineRenderer.startWidth = Definition.CableWidth;
        lineRenderer.endWidth = Definition.CableWidth;
        if (Definition.CableMaterial != null)
            lineRenderer.sharedMaterial = Definition.CableMaterial;
    }


    public void RefreshVisual()
    {
        if (lineRenderer == null || Definition == null ||
            SourcePort?.Anchor == null || TargetPort?.Anchor == null) return;

        Vector3 start = SourcePort.Anchor.position;
        Vector3 end = TargetPort.Anchor.position;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.enabled = modelSpan == null;
        if (modelSpan == null) return;

        Transform parent = modelSpan.parent;
        Vector3 a = parent.InverseTransformPoint(start);
        Vector3 b = parent.InverseTransformPoint(end);
        Vector3 delta = b - a;
        bool visible = delta.sqrMagnitude > 1e-12f;
        modelSpan.gameObject.SetActive(visible);
        if (!visible) return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, delta.normalized);
        modelSpan.SetLocalPositionAndRotation((a + b) * 0.5f, rotation);
        float width = Definition.CableWidth;
        float xScale = parent.TransformVector(rotation * Vector3.right).magnitude;
        float yScale = parent.TransformVector(rotation * Vector3.up).magnitude;
        modelSpan.localScale = new Vector3(width / xScale, width / yScale, delta.magnitude);
    }


    public void Transmit(
        PulseData pulse)
    {
        if (Definition == null ||
            TargetNode == null)
        {
            return;
        }

        pulseQueue.Enqueue(pulse);

        if (pulseRoutine == null)
        {
            pulseRoutine =
                StartCoroutine(
                    TransmitQueueRoutine());
        }
    }

    private IEnumerator TransmitQueueRoutine()
    {
        while (pulseQueue.Count > 0)
        {
            PulseData pulse =
                pulseQueue.Dequeue();

            yield return
                TransmitSinglePulse(pulse);
        }

        pulseRoutine = null;
    }

    private IEnumerator TransmitSinglePulse(
        PulseData pulse)
    {
        if (SourcePort?.Anchor == null ||
            TargetPort?.Anchor == null)
        {
            yield break;
        }

        GameSounds.Play(GameSounds.Travel);
        if (pulseVisual == null)
        {
            TargetNode.ReceivePulse(pulse);
            yield break;
        }

        pulseVisual.gameObject.SetActive(true);

        pulseVisual.position =
            SourcePort.Anchor.position;

        float speed = Definition.PulseSpeed;
        while (Vector3.Distance(
                   pulseVisual.position,
                   TargetPort.Anchor.position)
               > 0.01f)
        {
            pulseVisual.position =
                Vector3.MoveTowards(
                    pulseVisual.position,
                    TargetPort.Anchor.position,
                    speed *
                    Time.deltaTime);

            yield return null;
        }

        pulseVisual.position =
            TargetPort.Anchor.position;

        pulseVisual.gameObject.SetActive(false);

        TargetNode.ReceivePulse(pulse);
    }
}
