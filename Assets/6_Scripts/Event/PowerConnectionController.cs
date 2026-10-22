using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerConnectionController : MonoBehaviour
{
    [BoxGroup("References"), Required, SerializeField] private Camera mainCamera;
    [BoxGroup("References"), Required, SerializeField] private PowerCable cablePrefab;
    [BoxGroup("References"), Required, AssetsOnly, SerializeField] private PowerCableDefinitionSO cableDefinition;
    [BoxGroup("References"), Required, SerializeField] private Transform cableRoot;
    [BoxGroup("References"), SerializeField] private PowerPortVisuals portVisuals;
    [BoxGroup("Raycast"), SerializeField] private LayerMask powerNodeLayer;
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public bool IsBuildMode { get; private set; }
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] private PowerNode selectedSourceNode;
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] private PowerPortSlot selectedSourcePort;
    private int enabledFrame;

    public PowerNode SelectedSource => selectedSourceNode;
    public PowerPortSlot SelectedPort => selectedSourcePort;
    public Camera ViewCamera => mainCamera;
    public string Status => selectedSourceNode == null
        ? "CABLE | Select an output port"
        : "CABLE | Select an input port";
    public event System.Action<string> StatusChanged;

    private void Update()
    {
        if (!IsBuildMode) return;
        if (selectedSourcePort != null && (selectedSourceNode == null ||
            !selectedSourceNode.isActiveAndEnabled || !selectedSourceNode.HasFreeOutput ||
            !selectedSourcePort.IsFree || selectedSourcePort.Anchor == null))
            CancelCurrentConnection();
        if (Mouse.current == null || Time.frameCount <= enabledFrame) return;
        if (Mouse.current.rightButton.wasPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            SetBuildMode(false);
            return;
        }
        bool overUI = BuildPointer.IsOverUI();
        if (portVisuals != null && portVisuals.IsReady)
        {
            portVisuals.UpdatePointer(Mouse.current.position.ReadValue(), overUI);
            if (!overUI && Mouse.current.leftButton.wasPressedThisFrame)
                portVisuals.SelectHovered();
        }
        else if (!overUI && Mouse.current.leftButton.wasPressedThisFrame)
            HandleClick();
    }

    private void OnDisable() => SetBuildMode(false);

    public void SetBuildMode(bool value)
    {
        IsBuildMode = value && mainCamera != null && cablePrefab != null && cableDefinition != null;
        enabledFrame = Time.frameCount;
        CancelCurrentConnection();
    }

    private void HandleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, powerNodeLayer, QueryTriggerInteraction.Ignore))
            return;
        PowerNode node = hit.collider.GetComponentInParent<PowerNode>();
        if (node != null) SelectNode(node, hit.point);
    }

    // Compatibility path for existing callers when the optional marker module is absent.
    public void SelectNode(PowerNode node, Vector3 clickedWorldPosition)
    {
        if (node != null) SelectPort(node, node.FindClosestFreePort(clickedWorldPosition));
    }

    public bool CanSelectPort(PowerNode node, PowerPortSlot port)
    {
        if (!IsBuildMode || node == null || !node.isActiveAndEnabled ||
            port == null || port.Anchor == null || !port.IsFree) return false;
        bool owned = false;
        for (int i = 0; i < node.Ports.Count; i++)
            if (node.Ports[i] == port) { owned = true; break; }
        if (!owned) return false;
        return selectedSourceNode == null ? node.HasFreeOutput :
            node != selectedSourceNode && node.HasFreeInput;
    }

    public bool SelectPort(PowerNode node, PowerPortSlot port)
    {
        if (!CanSelectPort(node, port)) return false;
        if (selectedSourceNode == null)
        {
            selectedSourcePort = port;
            selectedSourceNode = node;
            StatusChanged?.Invoke(Status);
            return true;
        }
        bool connected = selectedSourceNode.TryConnectTo(
            node, selectedSourcePort, port, cablePrefab, cableDefinition, cableRoot);
        if (connected)
            Debug.Log($"[Cable] {selectedSourceNode.name} ({selectedSourcePort.Position}) -> {node.name} ({port.Position})");
        CancelCurrentConnection();
        if (!connected)
            StatusChanged?.Invoke($"Connection rejected. Max cable: {cableDefinition.MaxLength:0.0}m. Check ports and direction.");
        return connected;
    }

    public void CancelCurrentConnection()
    {
        selectedSourceNode = null;
        selectedSourcePort = null;
        if (portVisuals != null) portVisuals.ClearHover();
        StatusChanged?.Invoke(Status);
    }
}
