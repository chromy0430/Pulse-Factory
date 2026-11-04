using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Bloom
{
    [Serializable, VolumeComponentMenu("Enhanced Rendering/Cinematic Bloom")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class CinematicBloomVolume : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Zero bypasses the entire bloom graph. Bloom is added in linear HDR before tone mapping.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 3f);
        [Tooltip("Linear HDR threshold. Values above 1 isolate emissive highlights.")]
        public MinFloatParameter threshold = new MinFloatParameter(1f, 0f);
        public ClampedFloatParameter softKnee = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0.05f, 0.95f);
        public ClampedIntParameter levels = new ClampedIntParameter(5, 1, 8);
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);
        [Tooltip("Clamp input highlights to avoid extreme values spreading over the frame.")]
        public MinFloatParameter clamp = new MinFloatParameter(64f, 1f);

        public bool IsActive() => active && intensity.value > 0f;
    }
}
