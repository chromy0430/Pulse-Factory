using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Reflections
{
    [Serializable, VolumeComponentMenu("Enhanced Rendering/Cinematic Reflections")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class CinematicReflectionVolume : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Additional screen-space specular contribution. Zero bypasses both passes.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 2);
        public ClampedFloatParameter maxDistance = new ClampedFloatParameter(20, 0, 100);
        public ClampedIntParameter steps = new ClampedIntParameter(64, 16, 128);
        [Tooltip("View-space depth tolerance for a refined ray hit, in world units.")]
        public ClampedFloatParameter thickness = new ClampedFloatParameter(0.15f, 0.01f, 1);
        public ClampedFloatParameter normalBias = new ClampedFloatParameter(0.03f, 0.005f, 0.2f);
        [Tooltip("Only polished opaque URP Lit surfaces are intended for this first SSR module.")]
        public ClampedFloatParameter minimumSmoothness = new ClampedFloatParameter(0.65f, 0, 0.99f);
        public ClampedFloatParameter edgeFade = new ClampedFloatParameter(0.08f, 0.01f, 0.3f);
        public ClampedFloatParameter radianceClamp = new ClampedFloatParameter(32, 0.1f, 256);
        [Tooltip("Shows valid hit confidence in grayscale, before downstream modules.")]
        public BoolParameter debugConfidence = new BoolParameter(false);
        public bool IsActive() => active && intensity.value > 0 && maxDistance.value > 0;
    }
}
