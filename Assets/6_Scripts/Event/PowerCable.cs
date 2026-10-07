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


    // ============================================================
    // Runtime Data
    // ============================================================

    private readonly Queue<PulseData> pulseQueue =
        new();

    private Coroutine pulseRoutine;


    // ============================================================
    // Unity
    // ============================================================

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


    // ============================================================
    // Initialize
    // ============================================================

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


    // ============================================================
    // Cable Visual
    // ============================================================

    public void RefreshVisual()
    {
        if (lineRenderer == null ||
            SourcePort?.Anchor == null ||
            TargetPort?.Anchor == null)
        {
            return;
        }

        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(
            0,
            SourcePort.Anchor.position);

        lineRenderer.SetPosition(
            1,
            TargetPort.Anchor.position);
    }


    // ============================================================
    // Pulse
    // ============================================================

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

        if (pulseVisual == null)
        {
            TargetNode.ReceivePulse(pulse);
            yield break;
        }

        pulseVisual.gameObject.SetActive(true);

        pulseVisual.position =
            SourcePort.Anchor.position;

        while (Vector3.Distance(
                   pulseVisual.position,
                   TargetPort.Anchor.position)
               > 0.01f)
        {
            pulseVisual.position =
                Vector3.MoveTowards(
                    pulseVisual.position,
                    TargetPort.Anchor.position,
                    Definition.PulseSpeed *
                    Time.deltaTime);

            yield return null;
        }

        pulseVisual.position =
            TargetPort.Anchor.position;

        pulseVisual.gameObject.SetActive(false);

        TargetNode.ReceivePulse(pulse);
    }
}