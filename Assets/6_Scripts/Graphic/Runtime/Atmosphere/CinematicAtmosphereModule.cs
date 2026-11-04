using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Atmosphere
{
    [CreateAssetMenu(menuName = "Enhanced Rendering/Cinematic Atmosphere Module")]
    public sealed class CinematicAtmosphereModule : RenderingModule
    {
        public Shader atmosphereShader;
        public override ScriptableRenderPassInput RequiredInputs => ScriptableRenderPassInput.Depth;

        public override IRenderingModuleRuntime CreateRuntime()
        {
            if (atmosphereShader == null || !atmosphereShader.isSupported) return null;
            return new CinematicAtmosphereRuntime(atmosphereShader);
        }
    }
}
