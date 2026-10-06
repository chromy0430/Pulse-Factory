using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class PowerNode : MonoBehaviour
{
    [Title("Power Node")]

    // ============================================================
    // Definition
    // ============================================================

    protected abstract BuildingDefinitionSO Definition { get; }

    [BoxGroup("Definition")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Building Definition")]
    private BuildingDefinitionSO DefinitionPreview =>
        Definition;


    // ============================================================
    // Ports
    // ============================================================

    [BoxGroup("Ports")]
    [SerializeField]
    [LabelText("Input Ports")]
    [ListDrawerSettings(
        ShowFoldout = true,
        DefaultExpandedState = true)]
    private List<Transform> inputPorts = new();

    [BoxGroup("Ports")]
    [SerializeField]
    [LabelText("Output Ports")]
    [ListDrawerSettings(
        ShowFoldout = true,
        DefaultExpandedState = true)]
    private List<Transform> outputPorts = new();


    // ============================================================
    // Connections
    // ============================================================

    [BoxGroup("Connections")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Incoming")]
    [ListDrawerSettings(
        ShowFoldout = true,
        DefaultExpandedState = true,
        IsReadOnly = true)]
    private readonly List<PowerCable> incomingCables = new();

    [BoxGroup("Connections")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Outgoing")]
    [ListDrawerSettings(
        ShowFoldout = true,
        DefaultExpandedState = true,
        IsReadOnly = true)]
    private readonly List<PowerCable> outgoingCables = new();


    // ============================================================
    // Runtime
    // ============================================================

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Input")]
    private string InputStatus =>
        $"{CurrentInputs} / {MaxInputs}";

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Output")]
    private string OutputStatus =>
        $"{CurrentOutputs} / {MaxOutputs}";

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Last Pulse ID")]
    private int lastPulseId = -1;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Received Pulses")]
    private int receivedPulseCount;


    // ============================================================
    // Properties
    // ============================================================

    public int MaxInputs =>
        Definition != null
            ? Definition.MaxInputs
            : 0;

    public int MaxOutputs =>
        Definition != null
            ? Definition.MaxOutputs
            : 0;

    public int CurrentInputs =>
        incomingCables.Count;

    public int CurrentOutputs =>
        outgoingCables.Count;

    public bool HasFreeInput =>
        CurrentInputs < MaxInputs;

    public bool HasFreeOutput =>
        CurrentOutputs < MaxOutputs;


    // ============================================================
    // Pulse
    // ============================================================

    public void ReceivePulse(PulseData pulse)
    {
        // 같은 Pulse가 순환해서 다시 들어오는 경우 차단
        if (lastPulseId == pulse.Id)
            return;

        lastPulseId = pulse.Id;

        receivedPulseCount++;

        PulseData processedPulse =
            ProcessPulse(pulse);

        ForwardPulse(processedPulse);
    }

    protected virtual PulseData ProcessPulse(
        PulseData pulse)
    {
        return pulse;
    }

    protected virtual void ForwardPulse(
        PulseData pulse)
    {
        foreach (PowerCable cable in outgoingCables)
        {
            if (cable == null)
                continue;

            cable.Transmit(pulse);
        }
    }


    // ============================================================
    // Connection
    // ============================================================

    public bool TryConnectTo(
        PowerNode target,
        PowerCable cablePrefab,
        PowerCableDefinitionSO cableDefinition,
        Transform cableParent = null)
    {
        if (target == null ||
            cablePrefab == null ||
            cableDefinition == null)
            return false;

        if (target == this)
        {
            Debug.LogWarning(
                "[PowerNode] 자기 자신과 연결할 수 없습니다.",
                this);

            return false;
        }

        if (!HasFreeOutput)
        {
            Debug.LogWarning(
                $"[PowerNode] {name}의 Output이 가득 찼습니다.",
                this);

            return false;
        }

        if (!target.HasFreeInput)
        {
            Debug.LogWarning(
                $"[PowerNode] {target.name}의 Input이 가득 찼습니다.",
                target);

            return false;
        }

        if (IsAlreadyConnected(target))
            return false;

        Transform outputPort =
            GetNextOutputPort();

        Transform inputPort =
            target.GetNextInputPort();

        if (outputPort == null ||
            inputPort == null)
        {
            Debug.LogError(
                "[PowerNode] 연결 가능한 Port Transform이 없습니다.",
                this);

            return false;
        }

        float distance =
            Vector3.Distance(
                outputPort.position,
                inputPort.position);

        if (distance > cableDefinition.MaxLength)
        {
            Debug.LogWarning(
                $"[PowerNode] 최대 전선 길이 초과: " +
                $"{distance:F2}m / {cableDefinition.MaxLength:F2}m",
                this);

            return false;
        }

        PowerCable cable =
            Instantiate(
                cablePrefab,
                cableParent);

        cable.Initialize(
            cableDefinition,
            this,
            target,
            outputPort,
            inputPort);

        outgoingCables.Add(cable);
        target.incomingCables.Add(cable);

        return true;
    }

    public void UnregisterOutgoingCable(
        PowerCable cable)
    {
        outgoingCables.Remove(cable);
    }

    public void UnregisterIncomingCable(
        PowerCable cable)
    {
        incomingCables.Remove(cable);
    }

    private bool IsAlreadyConnected(
        PowerNode target)
    {
        foreach (PowerCable cable in outgoingCables)
        {
            if (cable != null &&
                cable.TargetNode == target)
            {
                return true;
            }
        }

        return false;
    }

    private Transform GetNextInputPort()
    {
        int index =
            incomingCables.Count;

        if (index >= inputPorts.Count)
            return null;

        return inputPorts[index];
    }

    private Transform GetNextOutputPort()
    {
        int index =
            outgoingCables.Count;

        if (index >= outputPorts.Count)
            return null;

        return outputPorts[index];
    }
}