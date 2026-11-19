using System;

[Serializable]
public struct PulseData
{
    public int Id;
    public float Power;

    private float? powerBeforeBoost;
    private float? nextReceiverPower;
    private bool held;

    // Power includes the bonus delivered to this receiver. Modifiers use the
    // original power so that a bonus cannot be compounded by another factory.
    public float BasePower => powerBeforeBoost ?? Power;

    public PulseData(int id, float power)
    {
        this = default;
        Id = id;
        Power = power;
    }

    // Delivery metadata applies only to this hop. TryGetOutgoing constructs a fresh pulse.
    public int ArrivalPop { get; private set; }

    public PulseData WithArrivalPop(int number)
    {
        var delivery = this;
        delivery.ArrivalPop = number;
        return delivery;
    }

    public void Hold() => held = true;

    public void BoostNextReceiver(float power)
    {
        nextReceiverPower = nextReceiverPower.HasValue ? Math.Max(nextReceiverPower.Value, power) : power;
    }

    public bool TryGetOutgoing(out PulseData outgoing)
    {
        outgoing = new PulseData(Id, BasePower);
        if (held) return false;
        if (nextReceiverPower.HasValue)
        {
            outgoing.powerBeforeBoost = BasePower;
            outgoing.Power = nextReceiverPower.Value;
        }
        return true;
    }
}
