using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public sealed class BuildController : MonoBehaviour
{
    [BoxGroup("References"), Required, SerializeField] private Camera mainCamera;
    [BoxGroup("References"), Required, SerializeField] private BuildGrid grid;
    [BoxGroup("References"), Required, SerializeField] private BuildPanelView panel;
    [BoxGroup("References"), Required, SerializeField] private PowerConnectionController cables;
    [BoxGroup("References"), Required, SerializeField] private Transform buildingRoot;
    [BoxGroup("Buildings"), Required, AssetsOnly, SerializeField]
    [ValidateInput(nameof(ValidateDefinition), "Definition needs a BuildingRuntime prefab and a positive footprint.")] private BuildingDefinitionSO factoryA;
    [BoxGroup("Buildings"), Required, AssetsOnly, SerializeField]
    [ValidateInput(nameof(ValidateDefinition), "Definition needs a BuildingRuntime prefab and a positive footprint.")] private BuildingDefinitionSO factoryB;
    [BoxGroup("Preview"), Required, AssetsOnly, SerializeField] private Material validMaterial;
    [BoxGroup("Preview"), Required, AssetsOnly, SerializeField] private Material invalidMaterial;
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public BuildingDefinitionSO SelectedDefinition { get; private set; }
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public bool CanPlace { get; private set; }
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public string BuildMode =>
        panel != null && panel.IsOpen ? "Menu" : SelectedDefinition != null ? "Factory" :
        cables != null && cables.IsBuildMode ? "Cable" : "Idle";
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public Vector3 PreviewPosition { get; private set; }
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public string PlacementStatus { get; private set; }
    private BuildGhost ghost;
    private int selectionFrame;

    private bool ValidateDefinition(BuildingDefinitionSO value) => value != null &&
        value.Prefab != null && value.Prefab.GetComponent<BuildingRuntime>() != null &&
        value.PlacementSize.x > 0f && value.PlacementSize.y > 0f;

    private void OnEnable()
    {
        if (panel != null) panel.Selected += SelectTool;
        if (cables != null) cables.StatusChanged += OnCableStatus;
    }

    private void OnDisable()
    {
        if (panel != null) panel.Selected -= SelectTool;
        if (cables != null) cables.StatusChanged -= OnCableStatus;
        CancelMode();
        if (panel != null) panel.SetOpen(false);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
        {
            ToggleMenu();
            return;
        }
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
        {
            CancelMode();
            panel.SetOpen(false);
            return;
        }
        if (SelectedDefinition == null || Mouse.current == null || ghost == null) return;
        bool projected = grid.TryProject(mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue()), out var position);
        bool show = projected && !BuildPointer.IsOverUI() && !panel.IsOpen;
        ghost.gameObject.SetActive(show);
        CanPlace = false;
        if (!show) return;
        PreviewPosition = position;
        CanPlace = grid.CanPlace(position, SelectedDefinition.PlacementSize, out string reason);
        PlacementStatus = reason;
        ghost.Refresh(grid, position, CanPlace);
        panel.SetStatus(SelectedDefinition.name + " | " + reason);
        if (Time.frameCount > selectionFrame && Mouse.current.leftButton.wasPressedThisFrame && CanPlace)
            PlaceAt(position);
    }

    public void ToggleMenu()
    {
        bool open = !panel.IsOpen;
        CancelMode();
        panel.SetOpen(open);
        if (open) panel.SetStatus("Choose a building or cable | B / ESC to close");
    }

    public void SelectTool(int index)
    {
        CancelMode();
        panel.SetOpen(false);
        selectionFrame = Time.frameCount;
        if (index == 2)
        {
            cables.SetBuildMode(true);
            panel.SetStatus(cables.Status + " | Right click / ESC to cancel");
            return;
        }
        var definition = index == 0 ? factoryA : index == 1 ? factoryB : null;
        if (definition == null || definition.Prefab == null ||
            definition.Prefab.GetComponent<BuildingRuntime>() == null || !grid.IsConfigured)
        {
            panel.SetStatus("Missing building prefab or board configuration");
            return;
        }
        SelectedDefinition = definition;
        ghost = new GameObject("BuildGhost").AddComponent<BuildGhost>();
        ghost.Initialize(definition.PlacementSize, validMaterial, invalidMaterial);
        ghost.gameObject.SetActive(false);
        panel.SetStatus(definition.name + " | Move onto board");
    }

    public BuildingRuntime PlaceAt(Vector3 position)
    {
        if (SelectedDefinition == null || panel.IsOpen) return null;
        position = grid.Snap(position);
        Physics.SyncTransforms();
        if (!grid.CanPlace(position, SelectedDefinition.PlacementSize, out _)) return null;
        GameObject instance = Instantiate(SelectedDefinition.Prefab, position, grid.Rotation, buildingRoot);
        instance.name = SelectedDefinition.name;
        BuildingRuntime runtime = instance.GetComponent<BuildingRuntime>();
        runtime.Initialize(SelectedDefinition);
        Physics.SyncTransforms();
        return runtime;
    }

    public void CancelMode()
    {
        SelectedDefinition = null;
        CanPlace = false;
        PlacementStatus = "";
        if (ghost != null)
        {
            ghost.gameObject.SetActive(false);
            Destroy(ghost.gameObject);
            ghost = null;
        }
        if (cables != null) cables.SetBuildMode(false);
        if (panel != null) panel.SetStatus("B | BUILD MENU");
    }

    private void OnCableStatus(string status)
    {
        if (panel != null && cables.IsBuildMode) panel.SetStatus(status);
    }
}
