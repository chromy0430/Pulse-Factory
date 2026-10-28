using PulseFactory.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PulseFactory.EditorTools
{
    public static class ProgressionSetup
    {
        public static string Install()
        {
            if (EditorApplication.isPlaying) return "Stop Play Mode before setup.";
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/1_Scenes/Main.unity") return "Expected Main scene.";
            var state = Object.FindFirstObjectByType<ProgressionState>();
            if (state == null)
            {
                var root = new GameObject("ProgressionSystem");
                Undo.RegisterCreatedObjectUndo(root, "Install progression system");
                state = root.AddComponent<ProgressionState>();
                var effects = root.AddComponent<ProgressionEffects>(); effects.state = state;
                effects.timer = Object.FindFirstObjectByType<PulseTimer>();
                var ui = root.AddComponent<ProgressionUI>(); ui.state = state;
                ui.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/7_UI/BuildMenuFonts/PFBuildMenu Korean SDF.asset");
            }
            state.catalog = ProgressionCatalogSync.Synchronize();
            if (state.catalog.nodes.Count == 0)
            {
                var cable = Node(ProgressionKind.Research, "전력 공급망 강화", new Vector2(0, 0), 200, 10, ProgressionEffect.CableLength, 3, 1);
                cable.description = "전선 최대 길이를 2m에서 5m로 늘립니다.";
                var income = Node(ProgressionKind.Trait, "수익 극대화", new Vector2(300, 220), 100, 0, ProgressionEffect.CreditBonus, .1f, 5);
                var speed = Node(ProgressionKind.Trait, "파워보드 과부하", new Vector2(0, 0), 100, 0, ProgressionEffect.PulseIntervalReduction, 1, 8);
                var passive = Node(ProgressionKind.Trait, "수익 자동화", new Vector2(600, 0), 100, 0, ProgressionEffect.PassiveCredit, 10, 5);
                var capacity = Node(ProgressionKind.Trait, "공장 증축", new Vector2(300, 0), 100, 0, ProgressionEffect.Capacity, 1, 10);
                income.prerequisites.Add(speed);
                passive.prerequisites.Add(income);
                capacity.description = "공장 수용량 +1. 특성 비용·최대 단계·연결은 편집 가능한 테스트 기본값입니다.";
                foreach (var n in new[] { cable, income, speed, passive, capacity }) ProgressionCatalogSync.Save(n);
            }
            var build = Object.FindFirstObjectByType<BuildController>();
            if (build != null)
            {
                var policy = build.GetComponent<ProgressionBuildPolicy>() ?? build.gameObject.AddComponent<ProgressionBuildPolicy>();
                policy.state = state;
                policy.buildings = new SerializedObject(build).FindProperty("buildingRoot").objectReferenceValue as Transform;
            }
            AddResearchFactory();
            BuildMenuCatalogSync.Synchronize();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "Installed progression UI/state/effects, catalog, research factory; saved Main.";
        }
        private static ProgressionNodeSO Node(ProgressionKind kind, string title, Vector2 pos, int credits, int points, ProgressionEffect effect, float amount, int levels)
        {
            var n = ProgressionCatalogSync.Create(kind, pos);
            n.title = title; n.creditCost = credits; n.researchCost = points; n.effect = effect; n.amount = amount; n.maxLevel = levels;
            n.description = "특성 비용·단계·연결은 테스트 기본값입니다. 오른쪽 Inspector에서 변경할 수 있습니다.";
            return n;
        }
        private static void AddResearchFactory()
        {
            const string folder = "Assets/6_Scripts/Data/SO/Progression";
            const string definitionPath = folder + "/ResearchFactory.asset";
            if (AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(definitionPath) != null) return;
            var source = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>("Assets/6_Scripts/Data/SO/Building/ProductionFactory.asset");
            if (source == null || source.Prefab == null) throw new System.InvalidOperationException("Production factory prefab missing.");
            var effect = ScriptableObject.CreateInstance<ResearchPointEffectSO>();
            AssetDatabase.CreateAsset(effect, folder + "/ResearchPoints.asset");
            var definition = Object.Instantiate(source);
            AssetDatabase.CreateAsset(definition, definitionPath);
            var data = new SerializedObject(definition);
            data.FindProperty("id").stringValue = "ResearchFactory";
            data.FindProperty("displayName").stringValue = "연구 공장";
            data.FindProperty("buildCost").intValue = 300;
            var effects = data.FindProperty("effects"); effects.arraySize = 1; effects.GetArrayElementAtIndex(0).objectReferenceValue = effect;
            data.ApplyModifiedPropertiesWithoutUndo();
            var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source.Prefab));
            try
            {
                var runtime = new SerializedObject(root.GetComponent<BuildingRuntime>());
                runtime.FindProperty("definition").objectReferenceValue = definition; runtime.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/8_Prefabs/Build/Prefab_ResearchFactory.prefab");
                data.Update(); data.FindProperty("prefab").objectReferenceValue = prefab; data.ApplyModifiedPropertiesWithoutUndo();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssetIfDirty(definition);
        }
    }
}
