using PulseFactory.Progression;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PowerCable",
    menuName = "Pulse Factory/Power/Power Cable Definition")]
public class PowerCableDefinitionSO : ScriptableObject
{
    [Title("Power Cable Definition")]

    [BoxGroup("Construction")]
    [LabelText("Base Cost")]
    [MinValue(0)]
    [SerializeField]
    private int baseCost = 10;

    [BoxGroup("Construction")]
    [LabelText("Cost Per Meter")]
    [MinValue(0)]
    [SerializeField]
    private float costPerMeter;

    [BoxGroup("Limits")]
    [LabelText("Max Length")]
    [SuffixLabel("m", Overlay = true)]
    [MinValue(0.1f)]
    [SerializeField]
    private float maxLength = 2f;

    [BoxGroup("Pulse")]
    [LabelText("Pulse Speed")]
    [SuffixLabel("m/s", Overlay = true)]
    [MinValue(0.1f)]
    [SerializeField]
    private float pulseSpeed = 5f;

    [BoxGroup("Visual")]
    [LabelText("Cable Width")]
    [MinValue(0.001f)]
    [SerializeField]
    private float cableWidth = 0.05f;

    [BoxGroup("Visual")]
    [AssetsOnly]
    [SerializeField]
    private Material cableMaterial;

    public int BaseCost => baseCost;
    public float CostPerMeter => costPerMeter;

    public float MaxLength => maxLength + (PulseFactory.Progression.ProgressionState.Instance?.Total(PulseFactory.Progression.ProgressionEffect.CableLength) ?? 0f);
    public float PulseSpeed => pulseSpeed * (1f + (ProgressionState.Instance?.Total(ProgressionEffect.CableSpeedBonus) ?? 0f));

    public float CableWidth => cableWidth;
    public Material CableMaterial => cableMaterial;

    public int GetBuildCost(float length)
    {
        float discount = Mathf.Clamp01(ProgressionState.Instance?.Total(ProgressionEffect.CableCostDiscount) ?? 0f);
        return Mathf.CeilToInt((baseCost + length * costPerMeter) * (1f - discount));
    }
}
