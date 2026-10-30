using System.Collections.Generic;
using UnityEngine;

namespace PulseFactory.Progression
{
    public sealed class ProgressionCatalogSO : ScriptableObject
    {
        public List<ProgressionNodeSO> nodes = new();
        public bool Contains(ProgressionNodeSO node) => node != null && nodes.Contains(node);
        public ProgressionNodeSO Entry(ProgressionKind kind)
        {
            ProgressionNodeSO best = null;
            foreach (var node in nodes)
            {
                if (node == null || node.kind != kind) continue;
                if (best == null || EntryRank(node) < EntryRank(best) ||
                    EntryRank(node) == EntryRank(best) && (node.position.sqrMagnitude < best.position.sqrMagnitude ||
                    node.position.sqrMagnitude == best.position.sqrMagnitude && string.CompareOrdinal(node.id, best.id) < 0)) best = node;
            }
            return best;
        }
        private static int EntryRank(ProgressionNodeSO node) => node.entryNode ? 0 : node.prerequisites.Count == 0 ? 1 : 2;
        public string Validate(ProgressionNodeSO node)
        {
            if (!Contains(node)) return "목록에 없는 데이터입니다.";
            var visited = new HashSet<ProgressionNodeSO>();
            var active = new HashSet<ProgressionNodeSO>();
            return Visit(node, visited, active);
        }
        private string Visit(ProgressionNodeSO node, HashSet<ProgressionNodeSO> visited, HashSet<ProgressionNodeSO> active)
        {
            if (!Contains(node)) return "삭제되었거나 누락된 선행 데이터가 있습니다.";
            if (active.Contains(node)) return "선행 조건에 순환 연결이 있습니다.";
            if (!visited.Add(node)) return null;
            if (string.IsNullOrEmpty(node.id)) return "데이터 ID가 없습니다.";
            foreach (var other in nodes)
                if (other != null && other != node && other.id == node.id) return "중복 데이터 ID입니다.";
            active.Add(node);
            foreach (var prerequisite in node.prerequisites)
            {
                string error = Visit(prerequisite, visited, active);
                if (error != null) return error;
            }
            active.Remove(node);
            return null;
        }
    }
}
