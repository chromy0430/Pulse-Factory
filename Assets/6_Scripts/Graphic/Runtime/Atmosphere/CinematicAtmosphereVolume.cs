using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Atmosphere
{
    [Serializable, VolumeComponentMenu("Enhanced Rendering/Cinematic Atmosphere")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class CinematicAtmosphereVolume : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Extinction per world unit at base height. Zero skips the entire module.")]
        public ClampedFloatParameter density = new ClampedFloatParameter(0f, 0f, 1f);
        public MinFloatParameter maxDistance = new MinFloatParameter(50f, 0f);
        public FloatParameter baseHeight = new FloatParameter(0f);
        [Tooltip("Density decreases exponentially above base height; below it density stays constant.")]
        public ClampedFloatParameter heightFalloff = new ClampedFloatParameter(0.15f, 0f, 4f);
        public ColorParameter albedo = new ColorParameter(Color.white, false, false, true);
        [Tooltip("An authored ambient source, not sampled GI or sky lighting.")]
        public ColorParameter ambientColor = new ColorParameter(new Color(0.35f, 0.4f, 0.5f), true, false, true);
        public ClampedFloatParameter ambientStrength = new ClampedFloatParameter(0.5f, 0f, 4f);
        public ClampedFloatParameter directionalStrength = new ClampedFloatParameter(1f, 0f, 8f);
        [Tooltip("Henyey-Greenstein anisotropy. Positive values scatter toward the main light.")]
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.35f, -0.8f, 0.8f);
        public ClampedIntParameter steps = new ClampedIntParameter(32, 8, 64);
        public BoolParameter halfResolution = new BoolParameter(true);
        public BoolParameter receiveShadows = new BoolParameter(true);

        public bool IsActive() => active && density.value > 0f && maxDistance.value > 0f;
    }
}
