using System;
using System.IO;
using EnhancedRendering.Reflections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EnhancedRendering.Editor
{
    public static class ReflectionLabSetup
    {
        public const string ScenePath = CinematicLabSetup.Root + "/Scenes/ReflectionLab.unity";
        public const string ProfilePath = CinematicLabSetup.Root + "/Settings/ReflectionLabVolume.asset";
        public const string ModulePath = CinematicLabSetup.Root + "/Settings/CinematicReflections.asset";
        public const string RendererPath = CinematicLabSetup.Root + "/Settings/ReflectionLab_ForwardPlus.asset";
        const string OriginalScene = CinematicLabSetup.Root + "/Scenes/AtmosphereLab.unity";
        const string OriginalProfile = CinematicLabSetup.Root + "/Settings/AtmosphereLabVolume.asset";

        [MenuItem("Tools/Enhanced Rendering/Create Reflection Lab")]
        public static void CreateFromMenu() => Debug.Log(CreateLab());

        public static string CreateLab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene edits first.");
            if (File.Exists(ScenePath) || File.Exists(ProfilePath))
                throw new InvalidOperationException("Reflection lab already exists; open it instead.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(CinematicLabSetup.Root + "/Shaders/CinematicReflections.shader");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(CinematicLabSetup.RendererPath);
            if (shader == null || ShaderUtil.ShaderHasError(shader) || renderer == null)
                throw new InvalidOperationException("Compiled reflection shader and PC renderer are required.");
            if (File.Exists(RendererPath) || !AssetDatabase.CopyAsset(CinematicLabSetup.RendererPath, RendererPath))
                throw new InvalidOperationException("Cannot create independent Reflection Lab renderer.");
            renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            renderer.renderingMode = RenderingMode.ForwardPlus;
            RegisterRenderer(renderer);
            EnhancedRenderingFeature feature = null;
            foreach (var item in renderer.rendererFeatures)
                if (item is EnhancedRenderingFeature found) feature = found;
            if (feature == null) throw new InvalidOperationException("Enhanced Rendering feature is required.");
            if (!AssetDatabase.CopyAsset(OriginalProfile, ProfilePath))
                throw new InvalidOperationException("Cannot copy the atmosphere profile.");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            var reflection = profile.Add<CinematicReflectionVolume>(true);
            reflection.intensity.value = 0.8f;
            reflection.steps.value = 96;
            AssetDatabase.AddObjectToAsset(reflection, profile);
            EditorUtility.SetDirty(profile);
            var module = AssetDatabase.LoadAssetAtPath<CinematicReflectionModule>(ModulePath);
            if (module == null)
            {
                module = ScriptableObject.CreateInstance<CinematicReflectionModule>();
                module.reflectionShader = shader;
                AssetDatabase.CreateAsset(module, ModulePath);
            }
            // Reflection radiance precedes medium extinction, bloom and tone mapping.
            if (!feature.modules.Contains(module)) feature.modules.Insert(0, module);
            feature.Create();
            EditorUtility.SetDirty(feature);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            if (!AssetDatabase.CopyAsset(OriginalScene, ScenePath))
                throw new InvalidOperationException("Cannot copy the atmosphere lab scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    camera.GetUniversalAdditionalCameraData().SetRenderer(RendererIndex());
                foreach (var volume in root.GetComponentsInChildren<Volume>(true))
                    if (volume.sharedProfile != null && AssetDatabase.GetAssetPath(volume.sharedProfile) == OriginalProfile)
                        volume.sharedProfile = profile;
                if (root.name != "Gallery floor") continue;
                var floor = root.GetComponent<Renderer>();
                var polished = new Material(floor.sharedMaterial) { name = "Reflection Lab Slate" };
                polished.shader = Shader.Find("Enhanced Rendering/Reflective Lit");
                polished.SetFloat("_Metallic", 0.85f);
                polished.SetFloat("_Smoothness", 0.96f);
                AssetDatabase.CreateAsset(polished, CinematicLabSetup.Root + "/Materials/Reflection Lab Slate.mat");
                floor.sharedMaterial = polished;
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Reflection Lab created. Earlier labs and shared materials preserved.";
        }

        public static int ConfigureExistingLab()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != ScenePath)
                throw new InvalidOperationException("Open the saved ReflectionLab in Edit Mode first.");
            var shader = Shader.Find("Enhanced Rendering/Reflective Lit");
            var floor = AssetDatabase.LoadAssetAtPath<Material>(CinematicLabSetup.Root + "/Materials/Reflection Lab Slate.mat");
            var module = AssetDatabase.LoadAssetAtPath<CinematicReflectionModule>(ModulePath);
            if (!shader || ShaderUtil.ShaderHasError(shader) || !floor || !module || !Camera.main)
                throw new InvalidOperationException("Reflection lab assets or camera missing.");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (!renderer)
            {
                if (!AssetDatabase.CopyAsset(CinematicLabSetup.RendererPath, RendererPath))
                    throw new InvalidOperationException("Could not copy the PC renderer.");
                renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            }
            var feature = renderer.rendererFeatures.Find(item => item is EnhancedRenderingFeature) as EnhancedRenderingFeature;
            if (!feature) throw new InvalidOperationException("Lab renderer requires Enhanced Rendering.");
            renderer.renderingMode = RenderingMode.ForwardPlus;
            if (!feature.modules.Contains(module)) feature.modules.Insert(0, module);
            feature.Create();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            RegisterRenderer(renderer);
            int index = RendererIndex();
            Camera.main.GetUniversalAdditionalCameraData().SetRenderer(index);
            floor.shader = shader;
            EditorUtility.SetDirty(floor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssetIfDirty(floor);
            AssetDatabase.SaveAssetIfDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(GraphicsSettings.currentRenderPipeline);
            return index;
        }

        public static int RendererIndex()
        {
            var pipeline = new SerializedObject(GraphicsSettings.currentRenderPipeline);
            var list = pipeline.FindProperty("m_RendererDataList");
            for (int i = 0; i < list.arraySize; i++)
                if (AssetDatabase.GetAssetPath(list.GetArrayElementAtIndex(i).objectReferenceValue) == RendererPath) return i;
            throw new InvalidOperationException("Reflection Lab renderer is not registered.");
        }

        public static void RegisterRenderer(UniversalRendererData renderer)
        {
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null) throw new InvalidOperationException("URP pipeline required.");
            string backup = "Documentation/EnhancedRendering/Backups/PC_RPAsset.before-phase3.asset";
            if (!File.Exists(backup)) File.Copy(AssetDatabase.GetAssetPath(asset), backup);
            var pipeline = new SerializedObject(asset);
            var list = pipeline.FindProperty("m_RendererDataList");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) return;
            int index = list.arraySize;
            list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            pipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
    }
}
