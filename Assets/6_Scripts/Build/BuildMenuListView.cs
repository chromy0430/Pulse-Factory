using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class BuildMenuListView : MonoBehaviour
{
    [AssetsOnly, SerializeField] private BuildMenuCatalogSO catalog;
    [Required, SerializeField] private RectTransform content;
    [Required, SerializeField] private BuildMenuItemView buildingTemplate;
    [Required, SerializeField] private BuildMenuItemView cableItem;
    [SerializeField] private BuildMenuScroller scroller;
    private readonly List<BuildMenuItemView> instances = new();
    public IReadOnlyList<BuildMenuItemView> Items => instances;
    public event Action<BuildingDefinitionSO> BuildingSelected;
    public event Action CableSelected;
    public event Action<string> Hovered;
    private bool initialized;

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (catalog == null) catalog = Resources.Load<BuildMenuCatalogSO>(BuildMenuCatalogSO.ResourceName);
        Populate(catalog != null ? catalog.Buildings : Array.Empty<BuildingDefinitionSO>());
    }

    public void Populate(IReadOnlyList<BuildingDefinitionSO> definitions)
    {
        foreach (var item in instances)
        {
            if (item == cableItem) continue;
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }
        instances.Clear();
        buildingTemplate.gameObject.SetActive(false);
        foreach (var definition in definitions)
        {
            if (definition == null || !definition.ShowInBuildMenu) continue;
            var item = Instantiate(buildingTemplate, content);
            item.name = "Build_" + definition.name;
            item.Bind(definition, false, SelectBuilding, SelectCable, ShowStatus);
            item.gameObject.SetActive(true);
            instances.Add(item);
        }
        cableItem.transform.SetAsLastSibling();
        cableItem.Bind(null, true, SelectBuilding, SelectCable, ShowStatus);
        cableItem.gameObject.SetActive(true);
        instances.Add(cableItem);
        scroller?.Configure(instances.Count);
    }

    private void SelectBuilding(BuildingDefinitionSO definition) => BuildingSelected?.Invoke(definition);
    private void SelectCable() => CableSelected?.Invoke();
    private void ShowStatus(string message) => Hovered?.Invoke(message);
}
