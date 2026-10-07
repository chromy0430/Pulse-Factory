using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class CreditWallet : MonoBehaviour
{
    // ============================================================
    // Singleton
    // ============================================================

    public static CreditWallet Instance { get; private set; }


    // ============================================================
    // Settings
    // ============================================================

    [Title("Credit Wallet")]

    [BoxGroup("Settings")]
    [LabelText("Starting Credit")]
    [MinValue(0)]
    [SerializeField]
    private long startingCredit = 100;


    // ============================================================
    // Runtime
    // ============================================================

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Current Credit")]
    public long CurrentCredit { get; private set; }


    public event Action<long> OnCreditChanged;


    // ============================================================
    // Unity
    // ============================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "[CreditWallet] CreditWallet이 두 개 이상 존재합니다.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;

        CurrentCredit =
            startingCredit;
    }


    private void Start()
    {
        NotifyChanged();
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    // ============================================================
    // Credit
    // ============================================================

    public void AddCredit(
        long amount)
    {
        if (amount <= 0)
            return;

        CurrentCredit += amount;

        NotifyChanged();
    }


    public bool CanSpend(
        long amount)
    {
        return amount >= 0 &&
               CurrentCredit >= amount;
    }


    public bool TrySpend(
        long amount)
    {
        if (!CanSpend(amount))
            return false;

        CurrentCredit -= amount;

        NotifyChanged();

        return true;
    }


    private void NotifyChanged()
    {
        OnCreditChanged?.Invoke(
            CurrentCredit);
    }


    // ============================================================
    // Debug
    // ============================================================

    [BoxGroup("Debug")]
    [Button("Add 10 Credit")]
    private void DebugAdd10()
    {
        AddCredit(10);
    }

    [BoxGroup("Debug")]
    [Button("Spend 10 Credit")]
    private void DebugSpend10()
    {
        TrySpend(10);
    }
}