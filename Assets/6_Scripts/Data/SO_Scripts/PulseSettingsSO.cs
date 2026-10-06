using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PulseSettings",
    menuName = "Pulse Factory/Pulse/Pulse Settings")]
public class PulseSettingsSO : ScriptableObject
{
    [Title("Pulse Settings")]

    [BoxGroup("Timing")]
    [LabelText("Base Interval")]
    [SuffixLabel("sec", Overlay = true)]
    [MinValue(0.1f)]
    [SerializeField]
    private float baseInterval = 10f;

    [BoxGroup("Timing")]
    [LabelText("Minimum Interval")]
    [SuffixLabel("sec", Overlay = true)]
    [MinValue(0.1f)]
    [SerializeField]
    private float minimumInterval = 2f;

    [BoxGroup("Pulse")]
    [LabelText("Base Power")]
    [MinValue(0f)]
    [SerializeField]
    private float basePower = 1f;

    public float BaseInterval => baseInterval;
    public float MinimumInterval => minimumInterval;
    public float BasePower => basePower;
}