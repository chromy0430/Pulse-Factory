using System;
using System.IO;
using EnhancedRendering.Bloom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EnhancedRendering.Editor
{
    public static class CinematicLabSetup
    {
        public const string Root = "Assets/EnhancedRendering";
        public const string ScenePath = Root + "/Scenes/CinematicLab.unity";
        public const string ModulePath = Root + "/Settings/CinematicBloom.asset";
        public const string ProfilePath = Root + "/Settings/CinematicLabVolume.asset";
        public const string RendererPath = "Assets/Settings/PC_Renderer.asset";

        [MenuItem("Tools/Enhanced Rendering/Create Cinematic Lab")]
        public static void CreateFromMenu() => Debug.Log(CreateLab());

        public static string CreateLab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before creating the lab.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save existing scene edits before creating the lab.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("The lab already exists. Open it instead; existing work is preserved.");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/CinematicBloom.shader");
            if (renderer == null || shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("PC renderer or compiled bloom shader is unavailable.");
            if (GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode)
                throw new InvalidOperationException("Render Graph must be enabled.");
            Directory.CreateDirectory(Root + "/Settings");
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory("Documentation/EnhancedRendering/Backups");
            string backup = "Documentation/EnhancedRendering/Backups/PC_Renderer.before-phase1.asset";
            if (!File.Exists(backup)) File.Copy(RendererPath, backup);
            AssetDatabase.Refresh();

            var module = AssetDatabase.LoadAssetAtPath<CinematicBloomModule>(ModulePath);
            if (module == null)
            {
                module = ScriptableObject.CreateInstance<CinematicBloomModule>();
                module.bloomShader = shader;
                AssetDatabase.CreateAsset(module, ModulePath);
            }
            EnhancedRenderingFeature feature = null;
            foreach (var existing in renderer.rendererFeatures)
                if (existing is EnhancedRenderingFeature found) feature = found;
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<EnhancedRenderingFeature>();
                feature.name = "Enhanced Rendering";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
            }
            if (!feature.modules.Contains(module)) feature.modules.Add(module);
            feature.Create();
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);

            var profile = CreateProfile();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CinematicLabScene.Populate(profile);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return "Cinematic Lab created. PC renderer extended; SampleScene and default Volume preserved.";
        }

        static VolumeProfile CreateProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (existing != null) return existing;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            var bloom = profile.Add<CinematicBloomVolume>(true);
            bloom.intensity.value = 0.55f;
            bloom.threshold.value = 1.1f;
            bloom.scatter.value = 0.78f;
            bloom.levels.value = 6;
            var builtIn = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            builtIn.intensity.value = 0f;
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.15f;
            color.contrast.value = 8f;
            color.saturation.value = -8f;
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.19f;
            vignette.smoothness.value = 0.5f;
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }
    }
}
