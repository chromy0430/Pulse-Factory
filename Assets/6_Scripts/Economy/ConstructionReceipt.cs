using System.Collections.Generic;
using PulseFactory.Progression;
using UnityEngine;

// Only player purchases receive a receipt. Store the price at purchase time.
[DisallowMultipleComponent]
public sealed class ConstructionReceipt : MonoBehaviour
{
    private static readonly List<ConstructionReceipt> cables = new();
    public static IReadOnlyList<ConstructionReceipt> Cables => cables;
    public int PaidCost { get; private set; }
    public bool Available { get; private set; }
    public PowerCable Cable { get; private set; }
    private PowerNode building;

    public static bool CanPay(int cost, out string reason)
    {
        reason = null;
        if (cost == 0) return true;
        var wallet = CreditWallet.Instance;
        if (wallet && wallet.CanSpend(cost)) return true;
        reason = wallet ? $"크레딧 부족 · 필요 {cost}, 보유 {wallet.CurrentCredit}" : "지갑을 찾을 수 없습니다.";
        return false;
    }

    public static bool Pay(GameObject item, int cost)
    {
        if (item.GetComponent<ConstructionReceipt>() || !CanPay(cost, out _)) return false;
        var receipt = item.AddComponent<ConstructionReceipt>();
        receipt.Cable = item.GetComponent<PowerCable>();
        receipt.building = item.GetComponent<PowerNode>();
        if (cost > 0 && !CreditWallet.Instance.TrySpend(cost)) return false;
        receipt.PaidCost = cost;
        receipt.Available = true;
        if (receipt.Cable) cables.Add(receipt);
        return true;
    }

    private void OnDestroy() => cables.Remove(this);

    private static float RefundRate => Mathf.Clamp01(.5f + (ProgressionState.Instance?.Total(ProgressionEffect.RefundRatio) ?? 0));
    private long Refund(float rate) => Available ? (long)(PaidCost * (double)rate) : 0;

    public long Quote()
    {
        float rate = RefundRate;
        long total = Refund(rate);
        if (Available && building)
            foreach (var port in building.Ports)
                if (port.ConnectedCable && port.ConnectedCable.TryGetComponent(out ConstructionReceipt wire))
                    total += wire.Refund(rate);
        return total;
    }

    public bool Demolish()
    {
        var wallet = CreditWallet.Instance;
        if (!Available || !wallet || (!Cable && !(building is BuildingRuntime))) return false;
        long refund = Quote();
        Available = false;
        if (building)
            foreach (var port in building.Ports)
                if (port.ConnectedCable) RemoveCable(port.ConnectedCable);
        if (Cable) RemoveCable(Cable);
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        wallet.AddCredit(refund);
        return true;
    }

    public static void RemoveCable(PowerCable cable)
    {
        if (cable.TryGetComponent(out ConstructionReceipt receipt))
        {
            receipt.Available = false;
            cables.Remove(receipt);
        }
        cable.SourcePort?.Disconnect(cable);
        cable.TargetPort?.Disconnect(cable);
        if (cable.SourceNode) cable.SourceNode.UnregisterOutgoingCable(cable);
        if (cable.TargetNode) cable.TargetNode.UnregisterIncomingCable(cable);
        cable.gameObject.SetActive(false);
        Destroy(cable.gameObject);
    }
}
