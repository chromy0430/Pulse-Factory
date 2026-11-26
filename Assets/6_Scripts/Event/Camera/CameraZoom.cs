using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Title(
        "Camera Zoom",
        "마우스 휠로 카메라를 확대 / 축소합니다.")]

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private CinemachineFollow cinemachineFollow;


    // ============================================================
    // Zoom Settings
    // ============================================================

    [BoxGroup("Zoom Settings")]
    [LabelText("Minimum Distance")]
    [MinValue(0.1f)]
    [SerializeField]
    private float minDistance = 8f;

    [BoxGroup("Zoom Settings")]
    [LabelText("Maximum Distance")]
    [MinValue(0.1f)]
    [SerializeField]
    private float maxDistance = 28f;

    [BoxGroup("Zoom Settings")]
    [LabelText("Zoom Step")]
    [MinValue(0.1f)]
    [SerializeField]
    private float zoomStep = 2f;

    [BoxGroup("Zoom Settings")]
    [LabelText("Zoom Smooth Time")]
    [SuffixLabel("sec", Overlay = true)]
    [PropertyRange(0.01f, 1f)]
    [SerializeField]
    private float zoomSmoothTime = 0.12f;


    // ============================================================
    // Runtime
    // ============================================================

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Current Distance")]
    private float currentDistance;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Target Distance")]
    private float targetDistance;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Default Distance")]
    private float defaultDistance;


    private Vector3 zoomDirection;

    private float zoomVelocity;


    // ============================================================
    // Unity
    // ============================================================

    private void Reset()
    {
        cinemachineFollow =
            GetComponent<CinemachineFollow>();
    }


    private void Awake()
    {
        if (cinemachineFollow == null)
        {
            cinemachineFollow =
                GetComponent<CinemachineFollow>();
        }

        if (cinemachineFollow == null)
        {
            Debug.LogError(
                "[CameraZoom] CinemachineFollow를 찾을 수 없습니다.",
                this);

            enabled = false;
            return;
        }

        Vector3 initialOffset =
            cinemachineFollow.FollowOffset;

        defaultDistance =
            initialOffset.magnitude;

        if (defaultDistance <= 0.001f)
        {
            Debug.LogError(
                "[CameraZoom] Follow Offset의 길이가 0입니다.",
                this);

            enabled = false;
            return;
        }

        // 현재 카메라 방향을 그대로 저장한다.
        // 줌을 해도 이 방향은 변하지 않는다.
        zoomDirection =
            initialOffset.normalized;

        currentDistance =
            defaultDistance;

        targetDistance =
            defaultDistance;
    }


    private void Update()
    {
        if (PulseFactory.Menu.PFMenuScreen.BlocksGameplay) return;
        ReadZoomInput();
        UpdateZoom();
    }


    // ============================================================
    // Input
    // ============================================================

    private void ReadZoomInput()
    {
        if (PulseFactory.Progression.ProgressionUI.IsOpen || BuildPointer.IsOverUI()) return;
        if (Mouse.current == null)
            return;

        float scroll =
            Mouse.current.scroll
                .ReadValue()
                .y;

        scroll *= PulseFactory.Menu.PFPreferences.ZoomFactor;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        // 휠 위
        // → 카메라 가까이
        // → Zoom In
        if (scroll > 0f)
        {
            targetDistance -=
                zoomStep;
        }

        // 휠 아래
        // → 카메라 멀리
        // → Zoom Out
        else
        {
            targetDistance +=
                zoomStep;
        }

        targetDistance =
            Mathf.Clamp(
                targetDistance,
                minDistance,
                maxDistance);
    }


    // ============================================================
    // Zoom
    // ============================================================

    private void UpdateZoom()
    {
        currentDistance =
            Mathf.SmoothDamp(
                currentDistance,
                targetDistance,
                ref zoomVelocity,
                zoomSmoothTime);

        cinemachineFollow.FollowOffset =
            zoomDirection *
            currentDistance;
    }


    // ============================================================
    // Debug
    // ============================================================

    [BoxGroup("Debug")]
    [Button(
        "Reset Zoom",
        ButtonSizes.Medium)]
    private void ResetZoom()
    {
        targetDistance =
            Mathf.Clamp(
                defaultDistance,
                minDistance,
                maxDistance);
    }


    [BoxGroup("Debug")]
    [Button("Zoom In")]
    private void DebugZoomIn()
    {
        targetDistance =
            Mathf.Clamp(
                targetDistance - zoomStep,
                minDistance,
                maxDistance);
    }


    [BoxGroup("Debug")]
    [Button("Zoom Out")]
    private void DebugZoomOut()
    {
        targetDistance =
            Mathf.Clamp(
                targetDistance + zoomStep,
                minDistance,
                maxDistance);
    }
}
