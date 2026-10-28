using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    // ============================================================
    // Settings
    // ============================================================

    [Title(
        "Camera Movement",
        "WASD를 사용하여 카메라 Target을 XZ 평면에서 이동합니다.")]

    [BoxGroup("Movement")]
    [LabelText("Move Speed")]
    [SuffixLabel("m/s", Overlay = true)]
    [MinValue(0.1f)]
    [SerializeField]
    private float moveSpeed = 8f;


    // ============================================================
    // Bounds
    // ============================================================

    [BoxGroup("Movement Bounds")]
    [LabelText("Use Bounds")]
    [SerializeField]
    private bool useBounds = true;

    [BoxGroup("Movement Bounds")]
    [ShowIf(nameof(useBounds))]
    [LabelText("Min X")]
    [SerializeField]
    private float minX = -9f;

    [BoxGroup("Movement Bounds")]
    [ShowIf(nameof(useBounds))]
    [LabelText("Max X")]
    [SerializeField]
    private float maxX = 9f;

    [BoxGroup("Movement Bounds")]
    [ShowIf(nameof(useBounds))]
    [LabelText("Min Z")]
    [SerializeField]
    private float minZ = -9f;

    [BoxGroup("Movement Bounds")]
    [ShowIf(nameof(useBounds))]
    [LabelText("Max Z")]
    [SerializeField]
    private float maxZ = 9f;


    // ============================================================
    // Runtime
    // ============================================================

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Move Input")]
    private Vector2 moveInput;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Position")]
    private Vector3 CurrentPosition =>
        transform.position;


    // ============================================================
    // Unity
    // ============================================================

    private void Update()
    {
        if (PulseFactory.Progression.ProgressionUI.IsOpen) return;
        ReadInput();
        Move();
    }


    // ============================================================
    // Input
    // ============================================================

    private void ReadInput()
    {
        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;

        moveInput =
            new Vector2(
                horizontal,
                vertical
            ).normalized;
    }


    // ============================================================
    // Movement
    // ============================================================

    private void Move()
    {
        Vector3 direction =
            new Vector3(
                moveInput.x,
                0f,
                moveInput.y
            );

        Vector3 nextPosition =
            transform.position +
            direction *
            moveSpeed *
            Time.deltaTime;

        if (useBounds)
        {
            nextPosition.x =
                Mathf.Clamp(
                    nextPosition.x,
                    minX,
                    maxX
                );

            nextPosition.z =
                Mathf.Clamp(
                    nextPosition.z,
                    minZ,
                    maxZ
                );
        }

        transform.position =
            nextPosition;
    }


    // ============================================================
    // Debug
    // ============================================================

    [BoxGroup("Debug")]
    [Button("Reset Position")]
    private void ResetPosition()
    {
        transform.position =
            Vector3.zero;
    }
}
