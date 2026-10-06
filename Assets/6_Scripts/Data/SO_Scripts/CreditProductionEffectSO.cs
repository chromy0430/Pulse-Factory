using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CreditProduction",
    menuName = "Pulse Factory/Building Effects/Credit Production")]
public class CreditProductionEffectSO : BuildingEffectSO
{
    [Title("Credit Production")]

    [BoxGroup("Production")]
    [LabelText("Credit Per Pulse")]
    [MinValue(0)]
    [SerializeField]
    private int creditPerPulse = 10;

    public int CreditPerPulse =>
        creditPerPulse;

    public override void Apply(
        BuildingRuntime building,
        ref PulseData pulse)
    {
        int amount =
            Mathf.RoundToInt(
                creditPerPulse *
                pulse.Power);

        Debug.Log(
            $"[{building.name}] +{amount} Credit",
            building);

        // 추후:
        // CreditManager.Add(amount);
    }
}