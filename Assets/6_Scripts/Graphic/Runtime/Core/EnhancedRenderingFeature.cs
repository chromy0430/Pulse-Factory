using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering
{
    [DisallowMultipleRendererFeature("Enhanced Rendering")]
    public sealed class EnhancedRenderingFeature : ScriptableRendererFeature
    {
        [Tooltip("Ordered HDR modules, executed before URP post-processing. Null entries are skipped.")]
        public List<RenderingModule> modules = new List<RenderingModule>();
        public bool showInSceneView = true;
        ModulePass pass;

        public override void Create()
        {
            pass?.Dispose();
            pass = new ModulePass(modules);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            // Phase 1 is intentionally limited to mono base cameras, without MSAA.
            if (pass == null || !camera.postProcessEnabled || camera.renderType != CameraRenderType.Base
                || camera.camera.stereoEnabled || camera.cameraTargetDescriptor.msaaSamples > 1)
                return;
            if (camera.cameraType != CameraType.Game
                && !(showInSceneView && camera.cameraType == CameraType.SceneView))
                return;
            if (pass.Prepare(VolumeManager.instance.stack))
                renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
            pass = null;
        }

        sealed class ModulePass : ScriptableRenderPass
        {
            readonly List<Entry> entries = new List<Entry>();
            struct Entry
            {
                public RenderingModule asset;
                public IRenderingModuleRuntime runtime;
            }

            public ModulePass(List<RenderingModule> modules)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                requiresIntermediateTexture = true;
                if (modules == null) return;
                var seen = new HashSet<RenderingModule>();
                foreach (var module in modules)
                {
                    if (module == null || !seen.Add(module)) continue;
                    var runtime = module.CreateRuntime();
                    if (runtime != null) entries.Add(new Entry { asset = module, runtime = runtime });
                }
            }

            public bool Prepare(VolumeStack stack)
            {
                bool active = false;
                var inputs = ScriptableRenderPassInput.None;
                foreach (var entry in entries)
                {
                    if (entry.asset == null || !entry.asset.moduleEnabled || !entry.runtime.IsActive(stack))
                        continue;
                    active = true;
                    inputs |= entry.asset.RequiredInputs;
                }
                ConfigureInput(inputs);
                return active;
            }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                var color = resources.activeColorTexture;
                if (!color.IsValid()) return;
                var stack = VolumeManager.instance.stack;
                foreach (var entry in entries)
                {
                    if (entry.asset == null || !entry.asset.moduleEnabled || !entry.runtime.IsActive(stack))
                        continue;
                    var output = entry.runtime.Record(graph, frameData, color, stack);
                    if (output.IsValid()) color = output;
                }
                resources.cameraColor = color;
            }

            public void Dispose()
            {
                foreach (var entry in entries) entry.runtime.Dispose();
                entries.Clear();
            }
        }
    }
}
