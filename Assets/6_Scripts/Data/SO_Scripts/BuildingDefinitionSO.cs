using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Building",
    menuName = "Pulse Factory/Building/Building Definition")]
public class BuildingDefinitionSO : ScriptableObject
{
    [Title("Building Definition")]

    // ============================================================
    // Identity
    // ============================================================

    [BoxGroup("Identity")]
    [LabelText("ID")]
    [SerializeField]
    private string id;

    [BoxGroup("Identity")]
    [LabelText("Display Name")]
    [SerializeField]
    private string displayName;

    [BoxGroup("Identity")]
    [PreviewField(70)]
    [SerializeField]
    private Sprite icon;


    // ============================================================
    // Construction
    // ============================================================

    [BoxGroup("Construction")]
    [LabelText("Build Cost")]
    [MinValue(0)]
    [SerializeField]
    private int buildCost;

    [BoxGroup("Construction")]
    [AssetsOnly]
    [Required]
    [SerializeField]
    private GameObject prefab;


    // ============================================================
    // Power
    // ============================================================

    [BoxGroup("Power")]
    [LabelText("Max Inputs")]
    [MinValue(0)]
    [SerializeField]
    private int maxInputs = 1;

    [BoxGroup("Power")]
    [LabelText("Max Outputs")]
    [MinValue(0)]
    [SerializeField]
    private int maxOutputs = 2;


    // ============================================================
    // Effects
    // ============================================================

    [BoxGroup("Effects")]
    [AssetsOnly]
    [SerializeField]
    [ListDrawerSettings(
        ShowFoldout = true,
        DefaultExpandedState = true)]
    private List<BuildingEffectSO> effects = new();


    // ============================================================
    // Properties
    // ============================================================

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;

[BoxGroup("Construction"), SerializeField, MinValue(0.02f)]
    private Vector2 placementSize = Vector2.one;
    public Vector2 PlacementSize => placementSize;

    public int BuildCost => buildCost;
    public GameObject Prefab => prefab;

    public int MaxInputs => maxInputs;
    public int MaxOutputs => maxOutputs;

    public IReadOnlyList<BuildingEffectSO> Effects =>
        effects;
}