using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PowerCable : MonoBehaviour
{
    [Title("Power Cable")]

    // ============================================================
    // References
    // ============================================================

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private LineRenderer lineRenderer;

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private Transform pulseVisual;


    // ============================================================
    // Runtime
    // ============================================================

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
    [SuffixLabel("m", Overlay = true)]
    public float Length =>
        sourcePort != null && targetPort != null
            ? Vector3.Distance(
                sourcePort.position,
                targetPort.position)
            : 0f;

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
    public bool IsTransmitting =>
        pulseRoutine != null;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Queued Pulses")]
    private int QueuedPulseCount =>
        pulseQueue.Count;


    private Transform sourcePort;
    private Transform targetPort;

    private readonly Queue<PulseData> pulseQueue = new();

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
        if (SourceNode != null)
            SourceNode.UnregisterOutgoingCable(this);

        if (TargetNode != null)
            TargetNode.UnregisterIncomingCable(this);
    }


    public void Initialize(
        PowerCableDefinitionSO definition,
        PowerNode source,
        PowerNode target,
        Transform sourcePortTransform,
        Transform targetPortTransform)
    {
        Definition = definition;

        SourceNode = source;
        TargetNode = target;

        sourcePort = sourcePortTransform;
        targetPort = targetPortTransform;

        ApplyDefinition();
        RefreshVisual();
    }

    private void ApplyDefinition()
    {
        if (Definition == null ||
            lineRenderer == null)
            return;

        lineRenderer.startWidth =
            Definition.CableWidth;

        lineRenderer.endWidth =
            Definition.CableWidth;

        if (Definition.CableMaterial != null)
        {
            lineRenderer.sharedMaterial =
                Definition.CableMaterial;
        }
    }

    public void RefreshVisual()
    {
        if (sourcePort == null ||
            targetPort == null ||
            lineRenderer == null)
            return;

        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(
            0,
            sourcePort.position);

        lineRenderer.SetPosition(
            1,
            targetPort.position);
    }


    public void Transmit(PulseData pulse)
    {
        if (Definition == null ||
            TargetNode == null)
            return;

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
        if (pulseVisual == null)
        {
            TargetNode.ReceivePulse(pulse);
            yield break;
        }

        pulseVisual.gameObject.SetActive(true);

        pulseVisual.position =
            sourcePort.position;

        while (Vector3.Distance(
                   pulseVisual.position,
                   targetPort.position) > 0.01f)
        {
            pulseVisual.position =
                Vector3.MoveTowards(
                    pulseVisual.position,
                    targetPort.position,
                    Definition.PulseSpeed *
                    Time.deltaTime);

            yield return null;
        }

        pulseVisual.position =
            targetPort.position;

        pulseVisual.gameObject.SetActive(false);

        TargetNode.ReceivePulse(pulse);
    }
}