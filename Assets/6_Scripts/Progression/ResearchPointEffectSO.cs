using UnityEngine;

namespace PulseFactory.Progression
{
    [CreateAssetMenu(menuName = "Pulse Factory/Building Effects/Research Points")]
    public sealed class ResearchPointEffectSO : BuildingEffectSO
    {
        [Min(1)] public int pointsPerPulse = 1;
        public override void Apply(BuildingRuntime building, ref PulseData pulse)
        {
            var state = ProgressionState.Instance;
            if (state != null)
                state.AddResearchPoints(pointsPerPulse + Mathf.RoundToInt(state.Total(ProgressionEffect.ResearchPointBonus)));
        }
    }
}
