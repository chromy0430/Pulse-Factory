using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PulseFactory.EditorTools
{
    public static class PFDataCatalog
    {
        public static readonly string[] Categories = { "건물", "전선", "건물 효과" };
        private static readonly Type[] Roots =
            { typeof(BuildingDefinitionSO), typeof(PowerCableDefinitionSO), typeof(BuildingEffectSO) };

        public static string TypeLabel(Type type)
        {
            if (type == typeof(BuildingDefinitionSO)) return "일반 건물";
            if (type == typeof(CoreDefinitionSO)) return "코어";
            if (type == typeof(PowerCableDefinitionSO)) return "전선";
            if (type == typeof(CreditProductionEffectSO)) return "크레딧 생산";
            if (type == typeof(AmplifyEffectSO)) return "펄스 증폭";
            return ObjectNames.NicifyVariableName(type.Name.Replace("SO", ""));
        }

        public static List<Type> Types(int category)
        {
            var root = Roots[Mathf.Clamp(category, 0, Roots.Length - 1)];
            var result = new List<Type>();
            if (!root.IsAbstract) result.Add(root);
            foreach (var type in TypeCache.GetTypesDerivedFrom(root))
                if (!type.IsAbstract && !type.ContainsGenericParameters) result.Add(type);
            result.Sort((a, b) => string.Compare(TypeLabel(a), TypeLabel(b), StringComparison.Ordinal));
            return result;
        }

        public static List<ScriptableObject> Find(int category, string search = "")
        {
            var root = Roots[Mathf.Clamp(category, 0, Roots.Length - 1)];
            var result = new List<ScriptableObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + root.Name, new[] { "Assets" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || !root.IsInstanceOfType(asset)) continue;
                string text = asset.name + " " + TypeLabel(asset.GetType());
                if (asset is BuildingDefinitionSO building) text += " " + building.Id + " " + building.DisplayName;
                if (string.IsNullOrWhiteSpace(search) || text.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(asset);
            }
            result.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static bool ValidFolder(string folder) => !string.IsNullOrEmpty(folder) &&
            (folder == "Assets" || folder.StartsWith("Assets/", StringComparison.Ordinal)) &&
            !folder.Contains("..") && !folder.Contains('\\') && AssetDatabase.IsValidFolder(folder);

        public static string Validate(Type type, string name, string folder)
        {
            if (type == null || type.IsAbstract || type.ContainsGenericParameters || !IsSupported(type))
                return "생성할 데이터 종류를 선택하세요.";
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name.EndsWith(".") ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\'))
                return "파일 이름을 확인하세요. 경로 구분자나 앞뒤 공백은 사용할 수 없습니다.";
            string stem = name.Split('.')[0].ToUpperInvariant();
            if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" ||
                (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) &&
                 stem[3] >= '0' && stem[3] <= '9')) return "운영체제 예약 이름은 사용할 수 없습니다.";
            if (!ValidFolder(folder)) return "Project 창의 Assets 아래 폴더를 지정하세요.";
            return null;
        }

        public static ScriptableObject Create(Type type, string name, string folder, ScriptableObject template = null)
        {
            string error = Validate(type, name, folder);
            if (error != null) throw new ArgumentException(error);
            if (template != null && template.GetType() != type) throw new ArgumentException("복제 원본의 종류가 다릅니다.");
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".asset");
            var asset = template != null ? UnityEngine.Object.Instantiate(template) : ScriptableObject.CreateInstance(type);
            try
            {
                asset.name = Path.GetFileNameWithoutExtension(path);
                if (asset is BuildingDefinitionSO)
                {
                    var serialized = new SerializedObject(asset);
                    serialized.FindProperty("id").stringValue = UniqueBuildingId(asset.name);
                    if (template == null) serialized.FindProperty("displayName").stringValue = asset.name;
                    if (template == null && asset is CoreDefinitionSO)
                    {
                        serialized.FindProperty("showInBuildMenu").boolValue = false;
                        serialized.FindProperty("maxInputs").intValue = 0;
                        serialized.FindProperty("maxOutputs").intValue = 2;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssetIfDirty(asset);
                return asset;
            }
            catch
            {
                if (!EditorUtility.IsPersistent(asset)) UnityEngine.Object.DestroyImmediate(asset);
                throw;
            }
        }

        private static bool IsSupported(Type type)
        {
            foreach (var root in Roots) if (root.IsAssignableFrom(type)) return true;
            return false;
        }

        private static string UniqueBuildingId(string basis)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var asset in Find(0)) ids.Add(((BuildingDefinitionSO)asset).Id ?? "");
            string id = basis;
            for (int suffix = 2; ids.Contains(id); suffix++) id = basis + "_" + suffix;
            return id;
        }
    }
}
