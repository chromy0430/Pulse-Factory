using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering
{
    /// <summary>Optional asset configuration. Each renderer owns a separate runtime instance.</summary>
    public abstract class RenderingModule : ScriptableObject
    {
        public bool moduleEnabled = true;
        public virtual ScriptableRenderPassInput RequiredInputs => ScriptableRenderPassInput.None;
        public abstract IRenderingModuleRuntime CreateRuntime();
    }

    public interface IRenderingModuleRuntime : IDisposable
    {
        bool IsActive(VolumeStack stack);
        TextureHandle Record(RenderGraph graph, ContextContainer frameData,
            TextureHandle source, VolumeStack stack);
    }
}
