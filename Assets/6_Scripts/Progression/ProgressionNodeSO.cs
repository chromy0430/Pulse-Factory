using System;
using System.Collections.Generic;
using UnityEngine;

namespace PulseFactory.Progression
{
    public enum ProgressionKind { Research, Trait }
    public enum ProgressionEffect { None, UnlockBuilding, Capacity, PulseIntervalReduction, CreditBonus, PassiveCredit, CableLength, RefundRatio, ResearchTimeReduction, CoreTimeReduction, AmplifyBonus, StorageBonus }

    [CreateAssetMenu(menuName = "Pulse Factory/Progression/Node")]
    public sealed class ProgressionNodeSO : ScriptableObject
    {
        [HideInInspector] public string id;
        public ProgressionKind kind;
        public string title = "New node";
        [TextArea] public string description;
        [Tooltip("트리를 열 때 중앙에 놓을 시작 노드. Tools/PF Progression에서 지정할 수 있습니다.")]
        public bool entryNode;
        [Tooltip("비워 두면 Title을 사용합니다.")] public string tooltipTitle;
        [TextArea(3, 12), Tooltip("비워 두면 Description을 사용합니다.")] public string tooltipDescription;
        public Sprite icon;
        public Vector2 position;
        public List<ProgressionNodeSO> prerequisites = new();
        [Min(0)] public int creditCost = 100;
        [Min(0)] public int researchCost;
        [Min(1)] public int maxLevel = 1;
        [Min(1)] public float costMultiplier = 1;
        public ProgressionEffect effect;
        public float amount = 1;
        public BuildingDefinitionSO building;
        public int MaxLevel => kind == ProgressionKind.Research ? 1 : Mathf.Max(1, maxLevel);
        private static int revision;
        public static int Revision => System.Threading.Volatile.Read(ref revision);
        public static void NotifyChanged() => System.Threading.Interlocked.Increment(ref revision);
        private void OnValidate() => NotifyChanged();
        public long Cost(int level, bool points = false)
        {
            double value = Math.Max(0, points ? researchCost : creditCost) * Math.Pow(Math.Max(1, costMultiplier), Math.Max(0, level));
            return double.IsNaN(value) || value >= long.MaxValue ? long.MaxValue : (long)Math.Ceiling(value);
        }
    }
}
