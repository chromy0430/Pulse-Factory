using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class CreditUI : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Title("Credit UI")]

    [BoxGroup("References")]
    [Required]
    [SerializeField]
    private TMP_Text creditText;


    // ============================================================
    // Unity
    // ============================================================

    private void Start()
    {
        if (CreditWallet.Instance == null)
        {
            Debug.LogError(
                "[CreditUI] CreditWallet이 없습니다.",
                this);

            return;
        }

        CreditWallet.Instance.OnCreditChanged +=
            UpdateCreditText;

        UpdateCreditText(
            CreditWallet.Instance.CurrentCredit);
    }


    private void OnDestroy()
    {
        if (CreditWallet.Instance != null)
        {
            CreditWallet.Instance.OnCreditChanged -=
                UpdateCreditText;
        }
    }


    // ============================================================
    // UI
    // ============================================================

    private void UpdateCreditText(
        long credit)
    {
        if (creditText == null)
            return;

        creditText.text =
            $"{credit:N0}";
    }
}