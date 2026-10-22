using System.Collections.Generic;
using UnityEngine;

public sealed class BuildMenuCatalogSO : ScriptableObject
{
    public const string ResourceName = "PFBuildMenuCatalog";
    [SerializeField] private List<BuildingDefinitionSO> buildings = new();
    public IReadOnlyList<BuildingDefinitionSO> Buildings => buildings;
}
