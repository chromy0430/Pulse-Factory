using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerConnectionController : MonoBehaviour
{
    [BoxGroup("References"), Required, SerializeField] private Camera mainCamera;
    [BoxGroup("References"), Required, SerializeField] private PowerCable cablePrefab;
    [BoxGroup("References"), Required, AssetsOnly, SerializeField] private PowerCableDefinitionSO cableDefinition;
    [BoxGroup("References"), Required, SerializeField] private Transform cableRoot;
    [BoxGroup("Raycast"), SerializeField] private LayerMask powerNodeLayer;
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public bool IsBuildMode { get; private set; }
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] private PowerNode selectedSourceNode;
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] private PowerPortSlot selectedSourcePort;
    private int enabledFrame;

    public string Status => selectedSourceNode == null
        ? "CABLE | Select an output building"
        : "CABLE | Select an input building";
    public event System.Action<string> StatusChanged;

    private void Update()
    {
        if (!IsBuildMode || Mouse.current == null || Time.frameCount <= enabledFrame) return;
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SetBuildMode(false);
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame && !BuildPointer.IsOverUI())
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

    public void SelectNode(PowerNode node, Vector3 clickedWorldPosition)
    {
        if (!IsBuildMode || node == null) return;
        if (selectedSourceNode == null)
        {
            if (!node.HasFreeOutput)
            {
                StatusChanged?.Invoke("No free output port. Select another building.");
                return;
            }
            selectedSourcePort = node.FindClosestFreePort(clickedWorldPosition);
            if (selectedSourcePort == null) return;
            selectedSourceNode = node;
            StatusChanged?.Invoke(Status);
            return;
        }
        if (node == selectedSourceNode)
        {
            CancelCurrentConnection();
            return;
        }
        if (!node.HasFreeInput)
        {
            StatusChanged?.Invoke("Target has no free input. Select another building.");
            return;
        }
        PowerPortSlot targetPort = node.FindClosestFreePort(clickedWorldPosition);
        bool connected = targetPort != null && selectedSourceNode.TryConnectTo(
            node, selectedSourcePort, targetPort, cablePrefab, cableDefinition, cableRoot);
        if (connected)
            Debug.Log($"[Cable] {selectedSourceNode.name} ({selectedSourcePort.Position}) -> {node.name} ({targetPort.Position})");
        CancelCurrentConnection();
        if (!connected) StatusChanged?.Invoke($"Connection rejected. Max cable: {cableDefinition.MaxLength:0.0}m. Check ports and direction.");
    }

    private void CancelCurrentConnection()
    {
        selectedSourceNode = null;
        selectedSourcePort = null;
        StatusChanged?.Invoke(Status);
    }
}
