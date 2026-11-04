using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Unity.Profiling;

// A single optional module. Port occupancy and limits always come from the live node.
public enum PowerPortPresentation { Billboard, Particles }

public sealed class PowerPortVisuals : MonoBehaviour
{
    [Required, SerializeField] private PowerConnectionController controller;
    [Required, AssetsOnly, SerializeField] private Material iconMaterial;
    [Required, AssetsOnly, SerializeField] private Shader fallbackShader;
    [SerializeField] private PowerPortPresentation presentation;
    [SerializeField, AssetsOnly] private ParticleSystem particleTemplate;
    [SerializeField, AssetsOnly] private Material particleMaterial;
    [SerializeField, Tooltip("Keep the lowest sibling above the model roof.")] private bool aboveModel = true;
    [SerializeField, Min(-.5f)] private float roofClearance = 0.12f;
    [SerializeField, Min(0)] private float outwardPadding = 0.25f;
    [SerializeField, Min(0.05f)] private float iconSize = 0.26f;
    [SerializeField] private float baseHeight = 0.35f;
    [SerializeField] private float hoverRise = 0.22f;
    [SerializeField] private float siblingDrop = 0.12f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.16f;
    [SerializeField] private bool forceFallback;
    private static readonly ProfilerMarker SyncMarker = new("PowerPorts.Sync");
    private static readonly ProfilerMarker PickMarker = new("PowerPorts.Pick");
    private static readonly ProfilerMarker DrawMarker = new("PowerPorts.Draw");
    private readonly PowerPortPlacement placement = new();
    private PowerPortParticles particles;
    private PowerPortPresentation activePresentation;
    private readonly List<PowerNode> owners = new();
    private readonly List<PowerPortSlot> ports = new();
    private PowerPortRenderData[] data = System.Array.Empty<PowerPortRenderData>();
    private PowerPortRenderer rendererBackend;
    private bool topologyDirty = true;
    private bool wasVisible;
    private int hovered = -1;
    [SerializeField, Min(3f)] private float minimumPickRadiusPixels = 10f;
    private readonly List<int> candidates = new(8);
    private Vector2 holdPoint;
    private bool holdCandidate;
    private PowerPortFocusOverlay focus;
    private Bounds bounds;
    public bool IsReady => isActiveAndEnabled && controller != null && (presentation == PowerPortPresentation.Particles
        ? particleMaterial != null || particleTemplate != null : iconMaterial != null && fallbackShader != null);
    public int IconCount => ports.Count;
    public int HoveredIndex => hovered;
    public long UploadedBytes => rendererBackend?.UploadedBytes ?? 0;
    public int UploadCalls => particles?.UploadCalls ?? rendererBackend?.UploadCalls ?? 0;
    public string RenderPath => particles != null ? "Particles" : rendererBackend == null ? "Inactive" : rendererBackend.IsIndirect ? "Indirect" : "CombinedMesh";
    public bool ForceFallback { get => forceFallback; set { forceFallback = value; Release(); } }

    public PowerPortPresentation Presentation { get => presentation; set { presentation = value; Release(); } }

    private void OnEnable()
    {
        PowerNode.TopologyChanged += OnTopologyChanged;
        topologyDirty = true;
    }

    private void OnDisable()
    {
        PowerNode.TopologyChanged -= OnTopologyChanged;
        ClearHover();
        Release();
    }

    private void OnTopologyChanged() => topologyDirty = true;
    public void RefreshLayout() => topologyDirty = true;
    private void OnValidate() => topologyDirty = true;
    private void Release()
    {
        focus?.Dispose(); focus = null;
        particles?.Dispose(); particles = null;
        rendererBackend?.Dispose();
        rendererBackend = null;
        wasVisible = false;
    }

    private void Rebuild()
    {
        placement.Clear(); owners.Clear(); ports.Clear();
        foreach (var node in FindObjectsByType<PowerNode>(FindObjectsSortMode.None))
        {
            if (!node.isActiveAndEnabled) continue;
            placement.Register(node);
            foreach (var port in node.Ports)
            {
                if (port?.Anchor == null) continue;
                owners.Add(node);
                ports.Add(port);
            }
        }
        data = new PowerPortRenderData[ports.Count];
        ClearHover();
        topologyDirty = false;
        rendererBackend?.SetCount(ports.Count);
        particles?.SetCount(ports.Count);
        Synchronize();
    }

    private void LateUpdate()
    {
        if (!IsReady || !controller.IsBuildMode)
        {
            if (wasVisible) { ClearHover(); particles?.Hide(); focus?.Hide(); wasVisible = false; }
            return;
        }
        if (activePresentation != presentation) Release();
        activePresentation = presentation;
        if (presentation == PowerPortPresentation.Particles && particles == null)
            particles = new PowerPortParticles(particleTemplate, particleMaterial);
        if (presentation == PowerPortPresentation.Billboard && rendererBackend == null)
            rendererBackend = new PowerPortRenderer(iconMaterial, fallbackShader, forceFallback);
        if (!wasVisible) topologyDirty = true;
        if (topologyDirty) Rebuild();
        wasVisible = true;
        using (SyncMarker.Auto()) Synchronize();
        using (DrawMarker.Auto())
        {
            rendererBackend?.Draw(controller.ViewCamera, data, bounds, Time.unscaledTime);
            particles?.Draw(data, bounds, Time.unscaledTime, controller.ViewCamera);
        }
        focus ??= new PowerPortFocusOverlay();
        int source = controller.SelectedPort != null ? ports.IndexOf(controller.SelectedPort) : -1;
        ShowFocus(0, source, true);
        ShowFocus(1, hovered, false);
    }

    private void ShowFocus(int slot, int index, bool source)
    {
        if (index < 0 || index >= ports.Count || !owners[index] || !ports[index].Anchor)
        {
            focus.Hide(slot);
            return;
        }
        bool tooFar = !source && controller.SelectedPort?.Anchor != null &&
            Vector3.Distance(controller.SelectedPort.Anchor.position, ports[index].Anchor.position) > controller.MaximumCableLength;
        focus.Show(slot, controller.ViewCamera, owners[index], ports[index], MarkerPosition(index),
            source || controller.SelectedSource == null, source ? 0 : candidates.Count, tooFar);
    }

    // Poll cached anchors/state for external movement or SO edits; no uploads when unchanged.
    private void Synchronize()
    {
        if (ports.Count == 0) return;
        float now = Time.unscaledTime;
        bool boundsDirty = false;
        int groupStart = 0;
        while (groupStart < ports.Count)
        {
            var owner = owners[groupStart];
            int end = groupStart;
            bool dirty = false;
            bool isSource = owner != null && owner == controller.SelectedSource;
            bool available = owner != null && owner.isActiveAndEnabled &&
                (controller.SelectedSource == null ? owner.HasFreeOutput : owner.HasFreeInput);
            float roof = owner != null ? placement.Height(owner, aboveModel, roofClearance, iconSize * 0.5f, siblingDrop) : 0;
            while (end < ports.Count && owners[end] == owner)
            {
                var port = ports[end];
                if (owner == null || port.Anchor == null) { topologyDirty = true; end++; continue; }
                var previous = data[end];
                Vector3 center = placement.Center(owner, port.Anchor, baseHeight, roof, outwardPadding);
                bool selected = isSource && port == controller.SelectedPort;
                bool yellow = port.IsFree && (selected || (!isSource && available));
                float shade = selected ? 1.35f : 1f;
                var tint = yellow ? new Vector4(shade, 0.78f * shade, 0.08f, 1) :
                    new Vector4(0.30f, 0.32f, 0.34f, 1);
                var position = new Vector4(center.x, center.y, center.z, iconSize);
                float target = hovered >= 0 && owners[hovered] == owner
                    ? (end == hovered ? hoverRise : -siblingDrop) : 0f;
                var motion = previous.Motion;
                if (!Mathf.Approximately(motion.y, target))
                    motion = new Vector4(previous.Offset(now), target, now, hoverDuration);
                data[end] = new PowerPortRenderData { PositionSize = position, Tint = tint, Motion = motion };
                if (previous.PositionSize != position) boundsDirty = true;
                if (previous.PositionSize != position || previous.Tint != tint || previous.Motion != motion)
                    dirty = true;
                end++;
            }
            if (dirty) { rendererBackend?.Upload(data, groupStart, end - groupStart); particles?.MarkDirty(); }
            groupStart = end;
        }
        if (boundsDirty)
        {
            bounds = new Bounds(data[0].Center, Vector3.zero);
            for (int i = 1; i < data.Length; i++) bounds.Encapsulate(data[i].Center);
            bounds.Expand(2f * (iconSize + Mathf.Abs(hoverRise) + Mathf.Abs(siblingDrop)));
        }
    }

    public void UpdatePointer(Vector2 screen, bool blockedByUI, bool cycle = false)
    {
        using (PickMarker.Auto())
        {
            if (topologyDirty) Rebuild();
            if (blockedByUI) ClearHover();
            else hovered = Pick(screen, cycle);
        }
    }

    private int Pick(Vector2 screen, bool cycle)
    {
        candidates.Clear();
        var camera = controller.ViewCamera;
        if (camera == null || !camera.pixelRect.Contains(screen)) return -1;
        int result = -1;
        float nearest = float.MaxValue;
        float closestDepth = float.MaxValue;
        for (int i = 0; i < ports.Count; i++)
        {
            if (!controller.CanSelectPort(owners[i], ports[i])) continue;
            Vector3 center = data[i].Center + Vector3.up * data[i].Offset(Time.unscaledTime);
            Vector3 projected = camera.WorldToScreenPoint(center);
            if (projected.z < camera.nearClipPlane || projected.z > camera.farClipPlane) continue;
            float radius = Mathf.Abs(camera.WorldToScreenPoint(center + camera.transform.right * iconSize * 0.5f).x - projected.x);
            radius = Mathf.Max(minimumPickRadiusPixels, radius);
            Vector2 rest = camera.WorldToScreenPoint(data[i].Center);
            Vector2 sweep = (Vector2)projected - rest;
            float t = Mathf.Clamp01(Vector2.Dot(screen - rest, sweep) / Mathf.Max(sweep.sqrMagnitude, .0001f));
            float score = (rest + sweep * t - screen).sqrMagnitude;
            if (score > radius * radius) continue;
            candidates.Add(i);
            if (score > nearest || (Mathf.Approximately(score, nearest) && projected.z >= closestDepth)) continue;
            nearest = score; closestDepth = projected.z; result = i;
        }
        if (cycle && candidates.Count > 1)
        {
            int current = candidates.IndexOf(hovered >= 0 ? hovered : result);
            holdPoint = screen; holdCandidate = true;
            return candidates[(current + 1) % candidates.Count];
        }
        if (holdCandidate && (screen - holdPoint).sqrMagnitude <= 16f && candidates.Contains(hovered)) return hovered;
        holdCandidate = false;
        return result;
    }

    public void SelectHovered()
    {
        int index = hovered;
        if (index >= 0 && index < ports.Count) controller.SelectPort(owners[index], ports[index]);
    }

    public void ClearHover() { hovered = -1; candidates.Clear(); holdCandidate = false; }
    public Vector3 MarkerPosition(int index) => data[index].Center + Vector3.up * data[index].Offset(Time.unscaledTime);
    public PowerPortSlot PortAt(int index) => ports[index];
    public PowerNode OwnerAt(int index) => owners[index];
    public Color ColorAt(int index) => data[index].Tint;
}
