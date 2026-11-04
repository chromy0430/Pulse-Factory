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
        demolitionMode ? "Demolition" : panel != null && panel.IsOpen ? "Menu" : SelectedDefinition != null ? "Factory" :
        cables != null && cables.IsBuildMode ? "Cable" : "Idle";
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public Vector3 PreviewPosition { get; private set; }
    [BoxGroup("Runtime"), ShowInInspector, ReadOnly] public string PlacementStatus { get; private set; }
    private BuildGhost ghost;
    private int selectionFrame;
    private bool demolitionMode;

    private bool ValidateDefinition(BuildingDefinitionSO value) => value != null &&
        value.Prefab != null && value.Prefab.GetComponent<BuildingRuntime>() != null &&
        value.PlacementSize.x > 0f && value.PlacementSize.y > 0f;

    private void OnEnable()
    {
        if (panel != null)
        {
            panel.Selected += SelectTool;
            panel.BuildingSelected += SelectBuilding;
            panel.CableSelected += SelectCable;
        }
        if (cables != null) cables.StatusChanged += OnCableStatus;
    }

    private void OnDisable()
    {
        if (panel != null)
        {
            panel.Selected -= SelectTool;
            panel.BuildingSelected -= SelectBuilding;
            panel.CableSelected -= SelectCable;
        }
        if (cables != null) cables.StatusChanged -= OnCableStatus;
        CancelMode();
        if (panel != null) panel.SetOpen(false);
    }

    private void Update()
    {
        if (PulseFactory.Progression.ProgressionUI.IsOpen) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
        {
            ToggleMenu();
            return;
        }
        if (keyboard != null && keyboard.xKey.wasPressedThisFrame)
        {
            bool enter = !demolitionMode;
            CancelMode();
            panel.SetOpen(false);
            demolitionMode = enter;
            return;
        }
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
        {
            CancelMode();
            panel.SetOpen(false);
            return;
        }
        if (demolitionMode)
        {
            UpdateDemolition();
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
        var progressionPolicy = GetComponent<PulseFactory.Progression.ProgressionBuildPolicy>();
        if (CanPlace && progressionPolicy != null) CanPlace = progressionPolicy.CanBuild(out reason);
        if (CanPlace && !ConstructionReceipt.CanPay(SelectedDefinition.BuildCost, out var paymentReason))
        {
            CanPlace = false;
            reason = paymentReason;
        }
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
        if (open) panel.SetStatus("건물 또는 전선 선택 · X 철거 · B / ESC 닫기");
    }

    // Compatibility for existing callers; the dynamic menu sends the SO itself.
    public void SelectTool(int index)
    {
        if (index == 2) SelectCable();
        else SelectBuilding(index == 0 ? factoryA : index == 1 ? factoryB : null);
    }

    public void SelectCable()
    {
        CancelMode();
        panel.SetOpen(false);
        selectionFrame = Time.frameCount;
        if (cables == null) return;
        cables.SetBuildMode(true);
        panel.SetStatus(cables.Status + " | Right click / ESC to cancel");
    }

    public void SelectBuilding(BuildingDefinitionSO definition)
    {
        CancelMode();
        panel.SetOpen(false);
        selectionFrame = Time.frameCount;
        if (!BuildMenuRules.CanPlace(definition, out string reason) || !grid.IsConfigured)
        {
            panel.SetStatus(string.IsNullOrEmpty(reason) ? "Missing board configuration" : reason);
            return;
        }
        SelectedDefinition = definition;
        ghost = new GameObject("BuildGhost").AddComponent<BuildGhost>();
        ghost.Initialize(definition.PlacementSize, validMaterial, invalidMaterial, grid.CellSize);
        ghost.gameObject.SetActive(false);
        panel.SetStatus(BuildMenuRules.Label(definition) + " | Move onto board");
    }

    public BuildingRuntime PlaceAt(Vector3 position)
    {
        if (SelectedDefinition == null || panel.IsOpen) return null;
        if (!BuildMenuRules.CanPlace(SelectedDefinition, out _)) return null;
        var progressionPolicy = GetComponent<PulseFactory.Progression.ProgressionBuildPolicy>();
        if (progressionPolicy != null && !progressionPolicy.CanBuild(out _)) return null;
        position = grid.Snap(position);
        Physics.SyncTransforms();
        if (!grid.CanPlace(position, SelectedDefinition.PlacementSize, out _)) return null;
        if (!ConstructionReceipt.CanPay(SelectedDefinition.BuildCost, out var reason))
        {
            PlacementStatus = reason;
            panel.SetStatus(reason);
            return null;
        }
        var definition = SelectedDefinition;
        GameObject instance = Instantiate(definition.Prefab, position, grid.Rotation, buildingRoot);
        instance.name = definition.name;
        BuildingRuntime runtime = instance.GetComponent<BuildingRuntime>();
        runtime.Initialize(definition);
        Physics.SyncTransforms();
        if (!ConstructionReceipt.Pay(instance, definition.BuildCost))
        {
            instance.SetActive(false);
            Destroy(instance);
            return null;
        }
        return runtime;
    }

    private void UpdateDemolition()
    {
        var mouse = Mouse.current;
        var target = mouse != null && !BuildPointer.IsOverUI()
            ? DemolitionPicker.Pick(mainCamera, mouse.position.ReadValue()) : null;
        if (!target || !target.Available)
        {
            panel.SetStatus("철거 · 건물 또는 전선을 가리키세요 · ESC 취소");
            return;
        }
        string label = target.Cable ? "전선" : target.GetComponent<PowerNode>().DisplayName;
        panel.SetStatus($"철거 · {label} · 환급 {target.Quote()} 크레딧 · 좌클릭 확정");
        if (mouse.leftButton.wasPressedThisFrame) target.Demolish();
    }

    public void CancelMode()
    {
        demolitionMode = false;
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
        if (panel != null) panel.SetStatus("B 건설 · X 철거");
    }

    private void OnCableStatus(string status)
    {
        if (panel != null && cables.IsBuildMode) panel.SetStatus(status);
    }
}
