using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PowerConnectionController : MonoBehaviour
{
    [Title("Power Connection Controller")]

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private Camera mainCamera;

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private PowerCable cablePrefab;

    [BoxGroup("References")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private PowerCableDefinitionSO cableDefinition;

    [BoxGroup("References")]
    [SerializeField]
    private Transform cableRoot;


    [BoxGroup("Raycast")]
    [SerializeField]
    private LayerMask powerNodeLayer;


    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Selected Source")]
    private PowerNode selectedSource;


    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelSelection();
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        TrySelectNode();
    }

    private void TrySelectNode()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            mainCamera.ScreenPointToRay(
                mousePosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                1000f,
                powerNodeLayer))
            return;

        PowerNode node =
            hit.collider.GetComponentInParent<PowerNode>();

        if (node == null)
            return;

        HandleNode(node);
    }

    private void HandleNode(
        PowerNode node)
    {
        if (selectedSource == null)
        {
            if (!node.HasFreeOutput)
                return;

            selectedSource = node;

            Debug.Log(
                $"[Connection] Source: {node.name}");

            return;
        }

        if (selectedSource == node)
        {
            CancelSelection();
            return;
        }

        bool success =
            selectedSource.TryConnectTo(
                node,
                cablePrefab,
                cableDefinition,
                cableRoot);

        if (success)
        {
            Debug.Log(
                $"[Connection] {selectedSource.name} → {node.name}");
        }

        selectedSource = null;
    }

    [BoxGroup("Debug")]
    [Button("Cancel Selection")]
    private void CancelSelection()
    {
        selectedSource = null;
    }
}