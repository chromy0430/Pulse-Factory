using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CreditProduction",
    menuName = "Pulse Factory/Building Effects/Credit Production")]
public class CreditProductionEffectSO : BuildingEffectSO
{
    [Title("Credit Production")]

    [BoxGroup("Production")]
    [LabelText("Base Credit Per Pulse")]
    [MinValue(0)]
    [SerializeField]
    private int baseCreditPerPulse = 10;


    public int BaseCreditPerPulse =>
        baseCreditPerPulse;


    public override void Apply(
        BuildingRuntime building,
        ref PulseData pulse)
    {
        float rawCredit =
            baseCreditPerPulse *
            pulse.Power * (1f + (PulseFactory.Progression.ProgressionState.Instance?.Total(PulseFactory.Progression.ProgressionEffect.CreditBonus) ?? 0f));

        int finalCredit =
            Mathf.RoundToInt(
                rawCredit);


        if (CreditWallet.Instance == null)
        {
            Debug.LogError(
                "[CreditProduction] CreditWallet이 없습니다.",
                building);

            return;
        }


        CreditWallet.Instance.AddCredit(
            finalCredit);


        Debug.Log(
            $"[{building.name}] +{finalCredit} Credit",
            building);
    }
}
