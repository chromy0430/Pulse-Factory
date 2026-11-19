using PulseFactory.Progression;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Amplify",
    menuName = "Pulse Factory/Building Effects/Amplify")]
public class AmplifyEffectSO : BuildingEffectSO
{
    [Title("Pulse Amplifier")]

    [BoxGroup("Amplification")]
    [LabelText("Multiplier")]
    [MinValue(1f)]
    [SerializeField]
    private float multiplier = 1.5f;

    public float Multiplier =>
        multiplier * (1f + (ProgressionState.Instance?.Total(ProgressionEffect.AmplifyBonus) ?? 0f));

    public override void Apply(
        BuildingRuntime building,
        ref PulseData pulse)
    {
        pulse.BoostNextReceiver(pulse.BasePower * Multiplier);
    }
}
