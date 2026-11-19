using Sirenix.OdinInspector;
using UnityEngine;
using PulseFactory.Audio;

public class Core : PowerNode
{
    [Title("Core")]

    [BoxGroup("Building")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private BuildingDefinitionSO definition;

    [BoxGroup("Event")]
    [Required]
    [AssetsOnly]
    [SerializeField]
    private PulseEventChannelSO pulseEvent;

    protected override BuildingDefinitionSO Definition =>
        definition;
    
    [BoxGroup("Visual")]
    [Required]
    [SerializeField]
    private BuildingPulseVisual pulseVisual;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Received Global Pulses")]
    private int globalPulseCount;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (pulseEvent != null)
            pulseEvent.Subscribe(OnGlobalPulse);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (pulseEvent != null)
            pulseEvent.Unsubscribe(OnGlobalPulse);
    }

    private void OnGlobalPulse(
        PulseData pulse)
    {
        globalPulseCount++;

        ReceivePulse(pulse);
    }

    protected override PulseData ProcessPulse(
        PulseData pulse)
    {
        GameSounds.Play(GameSounds.Release);
        pulseVisual?.PlayPulse(pulse.Power);
        
        Debug.Log(
            $"[Core] Pulse #{pulse.Id} / Power {pulse.Power:F2}",
            gameObject);

        return pulse;
    }
    
    
}
