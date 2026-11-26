using System;
using System.Collections.Generic;
using UnityEngine;
using PulseFactory.Audio;

namespace PulseFactory.Progression
{
    public sealed class ProgressionState : MonoBehaviour
    {
        public ProgressionCatalogSO catalog;
        [SerializeField] private CreditWallet wallet;
        [SerializeField, Min(0)] private long startingResearchPoints;
        public static ProgressionState Instance { get; private set; }
        public long ResearchPoints { get; private set; }
        public event Action Changed;
        private readonly Dictionary<string, int> levels = new();
        private bool purchasing;
        private void OnEnable() { if (Instance == null) Instance = this; Changed?.Invoke(); }
        private void OnDisable() { if (Instance == this) Instance = null; Changed?.Invoke(); }
        private void Awake()
        {
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;
            ResearchPoints = Math.Max(0, startingResearchPoints);
            if (catalog == null) catalog = Resources.Load<ProgressionCatalogSO>("PFProgressionCatalog");
        }
        private void Start()
        {
            if (wallet == null) wallet = CreditWallet.Instance;
            if (wallet != null) wallet.OnCreditChanged += CreditChanged;
            Changed?.Invoke();
        }
        private void OnDestroy()
        {
            if (wallet != null) wallet.OnCreditChanged -= CreditChanged;
            if (Instance == this) Instance = null;
        }
        private void CreditChanged(long _) { if (!purchasing) Changed?.Invoke(); }
        public int Level(ProgressionNodeSO node) => node != null && !string.IsNullOrEmpty(node.id) && levels.TryGetValue(node.id, out int level) ? level : 0;
        public void AddResearchPoints(long amount)
        {
            if (amount <= 0) return;
            ResearchPoints = amount > long.MaxValue - ResearchPoints ? long.MaxValue : ResearchPoints + amount;
            Changed?.Invoke();
        }
        public bool CanPurchase(ProgressionNodeSO node, out string reason)
        {
            if (!isActiveAndEnabled) { reason = "진행 시스템이 비활성화되어 있습니다."; return false; }
            if (!CanAccess(node, out reason)) return false;
            if (purchasing) reason = "구매 처리 중입니다.";
            else if (Level(node) >= node.MaxLevel) reason = "최대 단계입니다.";
            else if (node.effect == ProgressionEffect.ResearchTimeReduction || node.effect == ProgressionEffect.CoreTimeReduction || !Enum.IsDefined(typeof(ProgressionEffect), node.effect)) reason = "이 효과를 처리할 게임 모듈이 아직 연결되지 않았습니다.";
            if (reason == null && (wallet == null || !wallet.CanSpend(node.Cost(Level(node))))) reason = "크레딧이 부족합니다.";
            if (reason == null && ResearchPoints < node.Cost(Level(node), true)) reason = "연구 포인트가 부족합니다.";
            return reason == null;
        }
        public bool CanAccess(ProgressionNodeSO node, out string reason)
        {
            reason = catalog == null ? "트리 데이터가 없습니다." : catalog.Validate(node);
            if (reason != null) return false;
            foreach (var prerequisite in node.prerequisites)
                if (Level(prerequisite) < prerequisite.MaxLevel)
                { reason = "선행 완료 필요: " + prerequisite.title; return false; }
            return true;
        }
        public bool TryPurchase(ProgressionNodeSO node, out string reason)
        {
            if (!CanPurchase(node, out reason)) return false;
            purchasing = true;
            try
            {
                int level = Level(node);
                if (!wallet.TrySpend(node.Cost(level))) { reason = "크레딧이 부족합니다."; return false; }
                ResearchPoints -= node.Cost(level, true);
                levels[node.id] = level + 1;
                GameSounds.Play(node.kind == ProgressionKind.Research ? GameSounds.Research : GameSounds.Trait);
                Changed?.Invoke();
            }
            finally { purchasing = false; }
            return true;
        }
        public void Restore(long points, Dictionary<string, int> saved)
        {
            levels.Clear();
            foreach (var pair in saved) levels.Add(pair.Key, pair.Value);
            ResearchPoints = points;
            Changed?.Invoke();
        }
        public float Total(ProgressionEffect effect)
        {
            float total = 0;
            if (catalog != null) foreach (var node in catalog.nodes)
                if (node != null && node.effect == effect) total += node.amount * Level(node);
            return total;
        }
        public bool IsUnlocked(BuildingDefinitionSO building)
        {
            bool gated = false;
            if (catalog != null) foreach (var node in catalog.nodes)
            {
                if (node == null || node.effect != ProgressionEffect.UnlockBuilding || node.building != building) continue;
                gated = true;
                if (Level(node) > 0) return true;
            }
            return !gated;
        }
    }
}
