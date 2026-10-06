using UnityEngine;

public abstract class BuildingEffectSO : ScriptableObject
{
    public abstract void Apply(
        BuildingRuntime building,
        ref PulseData pulse);
}