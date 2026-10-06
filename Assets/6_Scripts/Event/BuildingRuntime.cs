using Sirenix.OdinInspector;
using UnityEngine;

public class BuildingRuntime : PowerNode
{
    [Title(
        "Building Runtime",
        "Building Definition에 설정된 효과를 실행합니다.")]

    [BoxGroup("Building")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private BuildingDefinitionSO definition;

    protected override BuildingDefinitionSO Definition =>
        definition;


    protected override PulseData ProcessPulse(
        PulseData pulse)
    {
        if (definition == null)
            return pulse;

        foreach (BuildingEffectSO effect
                 in definition.Effects)
        {
            if (effect == null)
                continue;

            effect.Apply(
                this,
                ref pulse);
        }

        return pulse;
    }
}