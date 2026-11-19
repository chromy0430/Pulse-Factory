using Sirenix.OdinInspector;
using UnityEngine;
using PulseFactory.Audio;

public class BuildingRuntime : PowerNode
{
    [Title(
        "Building Runtime",
        "Building Definition에 설정된 효과를 실행합니다.")]

    // ============================================================
    // Definition
    // ============================================================

    [BoxGroup("Building")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private BuildingDefinitionSO definition;

    // Zero assigns POP by the factory's position, from left to right.
    private int popNumber;
    public int PopNumber
    {
        get => popNumber;
        set => popNumber = Mathf.Clamp(value, 0, 5);
    }

    public void Initialize(BuildingDefinitionSO value)
    {
        definition = value;
    }

    protected override BuildingDefinitionSO Definition =>
        definition;


    // ============================================================
    // Visual
    // ============================================================

    [BoxGroup("Visual")]
    [Required]
    [SerializeField]
    private BuildingPulseVisual pulseVisual;


    // ============================================================
    // Pulse
    // ============================================================

    protected override void OnEnable()
    {
        base.OnEnable();
        GameSounds.Register(this);
    }

    protected override void OnDisable()
    {
        GameSounds.Unregister(this);
        base.OnDisable();
    }

    protected override PulseData ProcessPulse(
        PulseData pulse)
    {
        // 1. 공장에 전력 도착
        // 2. 시각적인 POP 반응
        GameSounds.Pop(this);
        pulseVisual?.PlayPulse(pulse.Power);

        // 3. 공장의 실제 기능 실행
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

        // 4. 가공된 Pulse를 다음 공장으로 전달
        return pulse;
    }


    // ============================================================
    // Editor
    // ============================================================

    private void Reset()
    {
        pulseVisual =
            GetComponent<BuildingPulseVisual>();
    }
}
