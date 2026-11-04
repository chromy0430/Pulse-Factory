using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class PowerNode : MonoBehaviour
{
    public static event System.Action TopologyChanged;
    protected virtual void OnEnable() => TopologyChanged?.Invoke();
    protected virtual void OnDisable() => TopologyChanged?.Invoke();
    protected virtual void OnDestroy()
    {
        for (int i = outgoingCables.Count - 1; i >= 0; i--)
            if (outgoingCables[i] != null) Destroy(outgoingCables[i].gameObject);
        for (int i = incomingCables.Count - 1; i >= 0; i--)
            if (incomingCables[i] != null) Destroy(incomingCables[i].gameObject);
    }

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
    [TableList(
        AlwaysExpanded = true,
        DrawScrollView = false)]
    [LabelText("Connection Ports")]
    private List<PowerPortSlot> ports = new();


    // ============================================================
    // Connections
    // ============================================================

    [BoxGroup("Connections")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Incoming Cables")]
    private readonly List<PowerCable> incomingCables = new();

    [BoxGroup("Connections")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Outgoing Cables")]
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

    public IReadOnlyList<PowerPortSlot> Ports =>
        ports;

    public string DisplayName => Definition != null ? Definition.DisplayName : name;

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
        CurrentInputs < MaxInputs &&
        HasFreePort();

    public bool HasFreeOutput =>
        CurrentOutputs < MaxOutputs &&
        HasFreePort();


    // ============================================================
    // Pulse
    // ============================================================

    public void ReceivePulse(
        PulseData pulse)
    {
        // 같은 Pulse가 순환 구조를 통해
        // 다시 들어오는 것을 차단
        if (lastPulseId == pulse.Id)
            return;

        lastPulseId = pulse.Id;
        receivedPulseCount++;

        PulseData processedPulse =
            ProcessPulse(pulse);

        if (processedPulse.TryGetOutgoing(out var outgoing))
            ForwardPulse(outgoing);
    }


    protected virtual PulseData ProcessPulse(
        PulseData pulse)
    {
        return pulse;
    }


    protected virtual void ForwardPulse(
        PulseData pulse)
    {
        for (int i = 0;
             i < outgoingCables.Count;
             i++)
        {
            PowerCable cable =
                outgoingCables[i];

            if (cable == null)
                continue;

            cable.Transmit(pulse);
        }
    }


    // ============================================================
    // Automatic Connection
    // ============================================================

    public bool TryConnectTo(
        PowerNode target,
        PowerCable cablePrefab,
        PowerCableDefinitionSO cableDefinition,
        Transform cableParent = null)
    {
        if (!ValidateConnectionRequest(
                target,
                cablePrefab,
                cableDefinition))
        {
            return false;
        }

        // 현재는 가장 가까운 Free Port끼리 자동 연결한다.
        // 나중에 플레이어가 직접 포트를 클릭하는 시스템에서는
        // 아래의 포트 지정 TryConnectTo()를 사용하면 된다.

        if (!TryFindBestPortPair(
                target,
                out PowerPortSlot sourcePort,
                out PowerPortSlot targetPort))
        {
            Debug.LogWarning(
                "[PowerNode] 사용 가능한 Port 조합을 찾을 수 없습니다.",
                this);

            return false;
        }

        return TryConnectTo(
            target,
            sourcePort,
            targetPort,
            cablePrefab,
            cableDefinition,
            cableParent);
    }


    // ============================================================
    // Manual Port Connection
    // ============================================================

    public bool TryConnectTo(
        PowerNode target,
        PowerPortSlot sourcePort,
        PowerPortSlot targetPort,
        PowerCable cablePrefab,
        PowerCableDefinitionSO cableDefinition,
        Transform cableParent = null)
    {
        if (!ValidateConnectionRequest(
                target,
                cablePrefab,
                cableDefinition))
        {
            return false;
        }

        if (sourcePort == null ||
            targetPort == null)
        {
            return false;
        }

        if (!ports.Contains(sourcePort))
        {
            Debug.LogError(
                "[PowerNode] Source Port가 이 노드의 Port가 아닙니다.",
                this);

            return false;
        }

        if (!target.ports.Contains(targetPort))
        {
            Debug.LogError(
                "[PowerNode] Target Port가 대상 노드의 Port가 아닙니다.",
                target);

            return false;
        }

        if (!sourcePort.IsFree ||
            !targetPort.IsFree)
        {
            Debug.LogWarning(
                "[PowerNode] 이미 사용 중인 Port입니다.",
                this);

            return false;
        }

        if (sourcePort.Anchor == null ||
            targetPort.Anchor == null)
        {
            Debug.LogError(
                "[PowerNode] Port Anchor가 설정되지 않았습니다.",
                this);

            return false;
        }

        float distance =
            Vector3.Distance(
                sourcePort.Anchor.position,
                targetPort.Anchor.position);

        if (distance >
            cableDefinition.MaxLength)
        {
            Debug.LogWarning(
                $"[PowerNode] 전선 최대 길이 초과: " +
                $"{distance:F2}m / " +
                $"{cableDefinition.MaxLength:F2}m",
                this);

            return false;
        }

        PowerCable cable =
            Instantiate(
                cablePrefab,
                cableParent);

        bool sourceConnected =
            sourcePort.TryConnect(
                PowerPortRole.Output,
                cable);

        bool targetConnected =
            targetPort.TryConnect(
                PowerPortRole.Input,
                cable);

        if (!sourceConnected ||
            !targetConnected)
        {
            sourcePort.Disconnect(cable);
            targetPort.Disconnect(cable);

            Destroy(cable.gameObject);

            return false;
        }

        cable.Initialize(
            cableDefinition,
            this,
            target,
            sourcePort,
            targetPort);

        outgoingCables.Add(cable);
        target.incomingCables.Add(cable);

        return true;
    }


    // ============================================================
    // Port Search
    // ============================================================

    private bool TryFindBestPortPair(
        PowerNode target,
        out PowerPortSlot bestSourcePort,
        out PowerPortSlot bestTargetPort)
    {
        bestSourcePort = null;
        bestTargetPort = null;

        float bestDistance =
            float.MaxValue;

        foreach (PowerPortSlot sourcePort in ports)
        {
            if (sourcePort == null ||
                !sourcePort.IsFree ||
                sourcePort.Anchor == null)
            {
                continue;
            }

            foreach (PowerPortSlot targetPort
                     in target.ports)
            {
                if (targetPort == null ||
                    !targetPort.IsFree ||
                    targetPort.Anchor == null)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        sourcePort.Anchor.position,
                        targetPort.Anchor.position);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;

                bestSourcePort =
                    sourcePort;

                bestTargetPort =
                    targetPort;
            }
        }

        return
            bestSourcePort != null &&
            bestTargetPort != null;
    }


    private bool HasFreePort()
    {
        foreach (PowerPortSlot port in ports)
        {
            if (port != null &&
                port.IsFree &&
                port.Anchor != null)
            {
                return true;
            }
        }

        return false;
    }
    
    public PowerPortSlot FindClosestFreePort(
        Vector3 worldPosition)
    {
        PowerPortSlot closestPort = null;

        float closestDistance =
            float.MaxValue;

        foreach (PowerPortSlot port in ports)
        {
            if (port == null ||
                !port.IsFree ||
                port.Anchor == null)
            {
                continue;
            }

            float distance =
                Vector3.SqrMagnitude(
                    port.Anchor.position -
                    worldPosition
                );

            if (distance >= closestDistance)
                continue;

            closestDistance =
                distance;

            closestPort =
                port;
        }

        return closestPort;
    }


    // ============================================================
    // Validation
    // ============================================================

    private bool ValidateConnectionRequest(
        PowerNode target,
        PowerCable cablePrefab,
        PowerCableDefinitionSO cableDefinition)
    {
        if (target == null ||
            cablePrefab == null ||
            cableDefinition == null)
        {
            return false;
        }

        if (target == this)
        {
            Debug.LogWarning(
                "[PowerNode] 자기 자신에게 연결할 수 없습니다.",
                this);

            return false;
        }

        if (!HasFreeOutput)
        {
            Debug.LogWarning(
                $"[PowerNode] {name}의 Output 한도에 도달했습니다.",
                this);

            return false;
        }

        if (!target.HasFreeInput)
        {
            Debug.LogWarning(
                $"[PowerNode] {target.name}의 Input 한도에 도달했습니다.",
                target);

            return false;
        }

        if (IsAlreadyConnected(target))
        {
            Debug.LogWarning(
                $"[PowerNode] {name} → {target.name}은 이미 연결되어 있습니다.",
                this);

            return false;
        }

        return true;
    }


    private bool IsAlreadyConnected(
        PowerNode target)
    {
        foreach (PowerCable cable
                 in outgoingCables)
        {
            if (cable != null &&
                cable.TargetNode == target)
            {
                return true;
            }
        }

        return false;
    }


    // ============================================================
    // Cable Removal
    // ============================================================

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
}
