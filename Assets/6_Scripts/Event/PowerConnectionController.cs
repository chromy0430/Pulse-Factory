using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerConnectionController : MonoBehaviour
{
    [Title("Power Connection Controller")]

    // ============================================================
    // References
    // ============================================================

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


    // ============================================================
    // Raycast
    // ============================================================

    [BoxGroup("Raycast")]
    [SerializeField]
    private LayerMask powerNodeLayer;


    // ============================================================
    // Build Mode
    // ============================================================

    [BoxGroup("Build Mode")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Cable Build Mode")]
    public bool IsBuildMode { get; private set; }


    // ============================================================
    // Runtime
    // ============================================================

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Source Node")]
    private PowerNode selectedSourceNode;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Source Port")]
    private PowerPortSlot selectedSourcePort;


    // ============================================================
    // Unity
    // ============================================================

    private void Update()
    {
        if (Keyboard.current == null ||
            Mouse.current == null)
            return;


        // --------------------------------------------------------
        // C : 전선 건설 모드
        // --------------------------------------------------------

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleBuildMode();
        }


        if (!IsBuildMode)
            return;


        // --------------------------------------------------------
        // 우클릭 : 현재 연결 취소
        // --------------------------------------------------------

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelCurrentConnection();
            return;
        }


        // --------------------------------------------------------
        // 좌클릭
        // --------------------------------------------------------

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }


    // ============================================================
    // Build Mode
    // ============================================================

    private void ToggleBuildMode()
    {
        IsBuildMode =
            !IsBuildMode;

        CancelCurrentConnection();

        Debug.Log(
            IsBuildMode
                ? "[Cable] Build Mode ON"
                : "[Cable] Build Mode OFF"
        );
    }


    // ============================================================
    // Click
    // ============================================================

    private void HandleClick()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            mainCamera.ScreenPointToRay(
                mousePosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                1000f,
                powerNodeLayer))
        {
            return;
        }

        PowerNode node =
            hit.collider.GetComponentInParent<PowerNode>();

        if (node == null)
            return;


        // 첫 번째 클릭
        if (selectedSourceNode == null)
        {
            SelectSource(
                node,
                hit.point
            );

            return;
        }


        // 두 번째 클릭
        SelectTarget(
            node,
            hit.point
        );
    }


    // ============================================================
    // Source
    // ============================================================

    private void SelectSource(
        PowerNode node,
        Vector3 clickedWorldPosition)
    {
        if (!node.HasFreeOutput)
        {
            Debug.LogWarning(
                $"[Cable] {node.name}은 더 이상 Output을 만들 수 없습니다."
            );

            return;
        }

        PowerPortSlot port =
            node.FindClosestFreePort(
                clickedWorldPosition
            );

        if (port == null)
            return;

        selectedSourceNode =
            node;

        selectedSourcePort =
            port;

        Debug.Log(
            $"[Cable] Source : " +
            $"{node.name} / {port.Position}"
        );
    }


    // ============================================================
    // Target
    // ============================================================

    private void SelectTarget(
        PowerNode targetNode,
        Vector3 clickedWorldPosition)
    {
        if (targetNode == selectedSourceNode)
        {
            CancelCurrentConnection();
            return;
        }

        if (!targetNode.HasFreeInput)
        {
            Debug.LogWarning(
                $"[Cable] {targetNode.name}은 더 이상 Input을 받을 수 없습니다."
            );

            return;
        }

        PowerPortSlot targetPort =
            targetNode.FindClosestFreePort(
                clickedWorldPosition
            );

        if (targetPort == null)
            return;


        bool success =
            selectedSourceNode.TryConnectTo(
                targetNode,
                selectedSourcePort,
                targetPort,
                cablePrefab,
                cableDefinition,
                cableRoot
            );

        if (success)
        {
            Debug.Log(
                $"[Cable] " +
                $"{selectedSourceNode.name} " +
                $"({selectedSourcePort.Position}) → " +
                $"{targetNode.name} " +
                $"({targetPort.Position})"
            );
        }

        CancelCurrentConnection();
    }


    // ============================================================
    // Cancel
    // ============================================================

    private void CancelCurrentConnection()
    {
        selectedSourceNode = null;
        selectedSourcePort = null;
    }


    // ============================================================
    // Debug
    // ============================================================

    [BoxGroup("Debug")]
    [Button("Toggle Cable Mode")]
    private void DebugToggle()
    {
        ToggleBuildMode();
    }
}