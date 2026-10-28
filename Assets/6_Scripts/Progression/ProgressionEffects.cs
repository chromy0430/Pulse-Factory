using UnityEngine;

namespace PulseFactory.Progression
{
    public sealed class ProgressionEffects : MonoBehaviour
    {
        public ProgressionState state;
        public PulseTimer timer;
        private float baseInterval;
        private double passiveRemainder;
        private bool initialized;
        private int revision;
        private void Start()
        {
            if (state == null) state = GetComponent<ProgressionState>();
            if (timer != null) baseInterval = timer.CurrentInterval;
            initialized = true;
            if (state != null) state.Changed += Apply;
            Apply();
        }
        private void OnDestroy()
        {
            if (state != null) state.Changed -= Apply;
            if (timer != null && baseInterval > 0) timer.SetInterval(baseInterval);
        }
        private void OnEnable() { if (initialized) Apply(); }
        private void OnDisable()
        {
            if (initialized && timer != null && baseInterval > 0) timer.SetInterval(baseInterval);
        }
        private void Apply()
        {
            if (!initialized || timer == null) return;
            bool active = isActiveAndEnabled && state != null && state.isActiveAndEnabled;
            timer.SetInterval(baseInterval - (active ? state.Total(ProgressionEffect.PulseIntervalReduction) : 0));
        }
        private void Update()
        {
            if (revision != ProgressionNodeSO.Revision) { revision = ProgressionNodeSO.Revision; Apply(); }
            if (state == null || !state.isActiveAndEnabled || CreditWallet.Instance == null) return;
            passiveRemainder += Mathf.Max(0, state.Total(ProgressionEffect.PassiveCredit)) * Time.deltaTime;
            if (passiveRemainder < 1) return;
            long credit = (long)passiveRemainder;
            passiveRemainder -= credit;
            CreditWallet.Instance.AddCredit(credit);
        }
    }
}
