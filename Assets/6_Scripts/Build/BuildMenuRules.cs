using UnityEngine;

public static class BuildMenuRules
{
    public static string Label(BuildingDefinitionSO definition) => definition == null ? "전선" :
        string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.name : definition.DisplayName;

    public static bool CanPlace(BuildingDefinitionSO definition, out string reason)
    {
        reason = "";
        if (definition == null) reason = "건물 데이터가 없습니다.";
        else if (definition.Prefab == null) reason = "데이터 툴에서 Prefab을 지정하세요.";
        else if (definition.Prefab.GetComponent<BuildingRuntime>() == null)
            reason = "건설 Prefab에 BuildingRuntime이 필요합니다.";
        else if (definition.PlacementSize.x <= 0f || definition.PlacementSize.y <= 0f)
            reason = "Placement Size는 0보다 커야 합니다.";
        else if (PulseFactory.Progression.ProgressionState.Instance != null &&
                 !PulseFactory.Progression.ProgressionState.Instance.IsUnlocked(definition))
            reason = "선행 연구를 완료해야 건설할 수 있습니다.";
        return reason.Length == 0;
    }
}
