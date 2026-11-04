using System;
using System.IO;
using EnhancedRendering.Atmosphere;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EnhancedRendering.Editor
{
    public static class AtmosphereLabSetup
    {
        public const string ScenePath = CinematicLabSetup.Root + "/Scenes/AtmosphereLab.unity";
        public const string ProfilePath = CinematicLabSetup.Root + "/Settings/AtmosphereLabVolume.asset";
        public const string ModulePath = CinematicLabSetup.Root + "/Settings/CinematicAtmosphere.asset";

        [MenuItem("Tools/Enhanced Rendering/Create Atmosphere Lab")]
        public static void CreateFromMenu() => Debug.Log(CreateLab());

        public static string CreateLab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene edits first.");
            if (File.Exists(ScenePath) || File.Exists(ProfilePath))
                throw new InvalidOperationException("Atmosphere lab already exists; existing work is preserved.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(CinematicLabSetup.Root + "/Shaders/CinematicAtmosphere.shader");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(CinematicLabSetup.RendererPath);
            if (shader == null || ShaderUtil.ShaderHasError(shader) || renderer == null)
                throw new InvalidOperationException("Compiled atmosphere shader and PC renderer are required.");
            EnhancedRenderingFeature feature = null;
            foreach (var item in renderer.rendererFeatures)
                if (item is EnhancedRenderingFeature found) feature = found;
            if (feature == null) throw new InvalidOperationException("Create the Phase 1 lab first.");
            string backup = "Documentation/EnhancedRendering/Backups/PC_Renderer.before-phase2.asset";
            if (!File.Exists(backup)) File.Copy(CinematicLabSetup.RendererPath, backup);
            if (!AssetDatabase.CopyAsset(CinematicLabSetup.ProfilePath, ProfilePath))
                throw new InvalidOperationException("Cannot copy the Phase 1 volume profile.");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            var atmosphere = profile.Add<CinematicAtmosphereVolume>(true);
            atmosphere.density.value = 0.045f;
            atmosphere.maxDistance.value = 50;
            atmosphere.heightFalloff.value = 0.18f;
            atmosphere.ambientColor.value = new Color(0.38f, 0.45f, 0.6f);
            atmosphere.ambientStrength.value = 0.35f;
            atmosphere.directionalStrength.value = 2;
            atmosphere.anisotropy.value = 0.35f;
            atmosphere.steps.value = 32;
            AssetDatabase.AddObjectToAsset(atmosphere, profile);
            EditorUtility.SetDirty(profile);
            var module = AssetDatabase.LoadAssetAtPath<CinematicAtmosphereModule>(ModulePath);
            if (module == null)
            {
                module = ScriptableObject.CreateInstance<CinematicAtmosphereModule>();
                module.atmosphereShader = shader;
                AssetDatabase.CreateAsset(module, ModulePath);
            }
            // Medium extinction and scattering happen before bloom and tone mapping.
            if (!feature.modules.Contains(module)) feature.modules.Insert(0, module);
            feature.Create();
            EditorUtility.SetDirty(feature);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            if (!AssetDatabase.CopyAsset(CinematicLabSetup.ScenePath, ScenePath))
                throw new InvalidOperationException("Cannot copy the Phase 1 lab scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var volume in root.GetComponentsInChildren<Volume>(true))
                    if (volume.sharedProfile != null && AssetDatabase.GetAssetPath(volume.sharedProfile) == CinematicLabSetup.ProfilePath)
                        volume.sharedProfile = profile;
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Atmosphere Lab created with an independent Volume profile. Original lab preserved.";
        }
    }
}
