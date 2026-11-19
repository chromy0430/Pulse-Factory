using System;
using TMPro;
using UnityEngine;
using PulseFactory.Progression;

namespace PulseFactory.UI
{
    [CreateAssetMenu(menuName = "Pulse Factory/UI Theme")]
    public sealed class PFUITheme : ScriptableObject
    {
        [Serializable] public struct SpriteEntry { public string key; public Sprite sprite; }
        [Serializable] public struct BuildingIconEntry { public BuildingDefinitionSO definition; public Sprite originalIcon; public Sprite themedIcon; }
        public TMP_FontAsset font;
        public SpriteEntry[] sprites;
        public BuildingIconEntry[] buildingIcons;
        public Color text = new(0.94f, 0.96f, 0.96f);
        public Color mint = new(0.51f, 0.95f, 0.73f);
        public Color cyan = new(0.34f, 0.90f, 0.93f);
        public Color gold = new(1f, 0.81f, 0.39f);
        public Color muted = new(0.55f, 0.62f, 0.65f);
        public Color danger = new(1f, 0.45f, 0.45f);
        private static PFUITheme current;
        public static PFUITheme Current => current != null ? current : current = Resources.Load<PFUITheme>("PFUITheme");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => current = null;
        public Sprite Get(string key)
        {
            if (sprites != null) foreach (var entry in sprites) if (entry.key == key) return entry.sprite;
            return null;
        }
        public Sprite BuildingIcon(BuildingDefinitionSO definition)
        {
            if (definition == null) return Get("Cable");
            if (buildingIcons != null) foreach (var entry in buildingIcons)
                if (entry.definition == definition)
                    return definition.Icon != entry.originalIcon && definition.Icon != null ? definition.Icon : entry.themedIcon;
            return definition.Icon != null ? definition.Icon : DefaultBuildingIcon(definition);
        }
        public Sprite DefaultBuildingIcon(BuildingDefinitionSO definition)
        {
            if (definition == null) return Get("Cable");
            string n = definition.name;
            if (n.Contains("Amplifier")) return Get("Amplifier");
            if (n.Contains("Capacitor")) return Get("Capacitor");
            if (n.Contains("Assembly")) return Get("Assembly");
            if (n.Contains("Research")) return Get("Research");
            if (n.Contains("Core")) return Get("Core");
            if (n.Contains("Production")) return Get("Factory");
            return definition.Icon != null ? definition.Icon : Get("Factory");
        }
        public Sprite NodeIcon(ProgressionNodeSO node)
        {
            if (node.icon != null) return node.icon;
            if (node.building != null) return BuildingIcon(node.building);
            return Get(node.effect switch
            {
                ProgressionEffect.Capacity => "Factory", ProgressionEffect.CableLength => "Cable",
                ProgressionEffect.CreditBonus => "Profit", ProgressionEffect.PassiveCredit => "Credit",
                ProgressionEffect.RefundRatio => "Recycle", ProgressionEffect.ResearchTimeReduction => "People",
                ProgressionEffect.AmplifyBonus => "Amplifier", ProgressionEffect.StorageBonus => "Capacitor",
                ProgressionEffect.PulseIntervalReduction => "Core", ProgressionEffect.CoreTimeReduction => "Clock",
                _ => node.kind == ProgressionKind.Research ? "Research" : "Star"
            });
        }
    }
}
