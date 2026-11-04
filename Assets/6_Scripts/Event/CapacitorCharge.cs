using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CapacitorCharge : MonoBehaviour
{
    [ShowInInspector, ReadOnly] public int StoredCount { get; private set; }
    [ShowInInspector, ReadOnly] public float StoredPower { get; private set; }

    public bool TryRelease(float incoming, int required, out float total)
    {
        StoredPower += incoming;
        StoredCount++;
        total = 0;
        if (StoredCount < required) return false;

        total = StoredPower;
        StoredCount = 0;
        StoredPower = 0;
        return true;
    }
}
