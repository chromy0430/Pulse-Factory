using UnityEngine;

namespace PulseFactory.Progression
{
    public sealed class ProgressionBuildPolicy : MonoBehaviour
    {
        public ProgressionState state;
        public Transform buildings;
        [Min(0)] public int baseCapacity = 3;
        public bool CanBuild(out string reason)
        {
            reason = null;
            if (state == null || !state.isActiveAndEnabled || buildings == null || !isActiveAndEnabled) return true;
            int count = 0;
            foreach (Transform child in buildings)
                if (child.gameObject.activeSelf && child.GetComponent<BuildingRuntime>() != null) count++;
            int capacity = baseCapacity + Mathf.FloorToInt(state.Total(ProgressionEffect.Capacity));
            if (count >= capacity) reason = $"공장 수용량 {count}/{capacity}";
            return reason == null;
        }
    }
}
