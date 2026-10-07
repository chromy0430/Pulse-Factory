using Sirenix.OdinInspector;
using UnityEngine;

public sealed class BuildGrid : MonoBehaviour
{
    [BoxGroup("Board"), Required, SceneObjectsOnly, SerializeField]
    [ValidateInput(nameof(ValidateBoard), "Assign an enabled horizontal board with positive dimensions.")]
    private BoxCollider board;
    [BoxGroup("Grid"), MinValue(0.02f), SerializeField]
    private float cellSize = 0.1f;
    [BoxGroup("Grid"), MinValue(0f), SerializeField]
    private float clearance = 0.02f;
    [BoxGroup("Validation"), InfoBox("The board must be horizontal. Cell Size is measured in world metres.")]
    [ShowInInspector, ReadOnly] public Bounds BoardBounds => board != null ? board.bounds : default;
    [ShowInInspector, ReadOnly] public float CellSize => Mathf.Max(0.02f, cellSize);
    [ShowInInspector, ReadOnly] public bool IsConfigured => board != null && board.enabled &&
        board.gameObject.activeInHierarchy && Vector3.Dot(board.transform.up, Vector3.up) > 0.999f;
    public Quaternion Rotation => board != null ? board.transform.rotation : Quaternion.identity;
    public Vector3 SurfaceCenter => board.transform.TransformPoint(board.center + Vector3.up * board.size.y * 0.5f);
    public Vector2 HalfSize => new Vector2(
        Mathf.Abs(board.size.x * board.transform.lossyScale.x),
        Mathf.Abs(board.size.z * board.transform.lossyScale.z)) * 0.5f;
    private readonly Collider[] overlaps = new Collider[128];

    public bool TryProject(Ray ray, out Vector3 position)
    {
        position = default;
        if (!IsConfigured) return false;
        var plane = new Plane(Vector3.up, SurfaceCenter);
        if (!plane.Raycast(ray, out float distance) || distance > 1000f) return false;
        position = Snap(ray.GetPoint(distance));
        return true;
    }

    public Vector3 Snap(Vector3 point)
    {
        Vector3 local = Quaternion.Inverse(Rotation) * (point - SurfaceCenter);
        local.x = Mathf.Round(local.x / CellSize) * CellSize;
        local.z = Mathf.Round(local.z / CellSize) * CellSize;
        local.y = 0f;
        return SurfaceCenter + Rotation * local;
    }

    public bool CanPlace(Vector3 position, Vector2 footprint, out string reason)
    {
        reason = "Board is not configured";
        if (!IsConfigured || footprint.x <= 0f || footprint.y <= 0f) return false;
        Vector3 local = Quaternion.Inverse(Rotation) * (position - SurfaceCenter);
        Vector2 half = footprint * 0.5f;
        if (Mathf.Abs(local.x) + half.x > HalfSize.x + 0.0001f ||
            Mathf.Abs(local.z) + half.y > HalfSize.y + 0.0001f)
        {
            reason = "Outside board";
            return false;
        }
        // Every placeholder has a stable root collider; pulse animation moves only Visual.
        int count = Physics.OverlapBoxNonAlloc(position + Vector3.up * 0.5f,
            new Vector3(half.x + clearance, 0.49f, half.y + clearance),
            overlaps, Rotation, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length)
        {
            reason = "Too many nearby colliders";
            return false;
        }
        for (int i = 0; i < count; i++)
        {
            if (overlaps[i] == null || overlaps[i] == board) continue;
            if (overlaps[i].GetComponentInParent<PowerNode>() == null) continue;
            reason = "Overlaps another building";
            return false;
        }
        reason = "Left click to place | Right click / ESC to cancel";
        return true;
    }

    private bool ValidateBoard(BoxCollider value) => value != null && value.enabled &&
        value.size.x > 0f && value.size.z > 0f &&
        Vector3.Dot(value.transform.up, Vector3.up) > 0.999f;

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.02f, cellSize);
        clearance = Mathf.Max(0f, clearance);
    }

    private void OnDrawGizmosSelected()
    {
        if (!IsConfigured) return;
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(SurfaceCenter, Rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(HalfSize.x * 2f, 0.01f, HalfSize.y * 2f));
    }
}
