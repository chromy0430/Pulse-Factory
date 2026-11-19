using PulseFactory.Progression;
using UnityEngine;

[CreateAssetMenu(menuName = "Pulse Factory/Building Effects/Capacitor")]
public sealed class CapacitorEffectSO : BuildingEffectSO
{
    [SerializeField, Min(1)] private int pulsesPerRelease = 2;
    [SerializeField, Min(1f)] private float releaseMultiplier = 2f;

    public override void Apply(BuildingRuntime building, ref PulseData pulse)
    {
        if (!building.TryGetComponent<CapacitorCharge>(out var charge) || !charge.isActiveAndEnabled) return;
        if (charge.TryRelease(pulse.BasePower, Mathf.Max(1, pulsesPerRelease), out float total))
        {
            float bonus = ProgressionState.Instance?.Total(ProgressionEffect.StorageBonus) ?? 0f;
            pulse.BoostNextReceiver(total * Mathf.Max(1f, releaseMultiplier + bonus));
        }
        else
            pulse.Hold();
    }
}
