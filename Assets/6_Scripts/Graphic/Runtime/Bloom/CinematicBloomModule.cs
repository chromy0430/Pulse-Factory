using UnityEngine;

namespace EnhancedRendering.Bloom
{
    [CreateAssetMenu(menuName = "Enhanced Rendering/Cinematic Bloom Module")]
    public sealed class CinematicBloomModule : RenderingModule
    {
        [Tooltip("Explicit reference keeps the shader available in player builds.")]
        public Shader bloomShader;

        public override IRenderingModuleRuntime CreateRuntime()
        {
            if (bloomShader == null || !bloomShader.isSupported) return null;
            return new CinematicBloomRuntime(bloomShader);
        }
    }
}
