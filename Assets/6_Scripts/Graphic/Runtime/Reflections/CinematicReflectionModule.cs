using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Reflections
{
    [CreateAssetMenu(menuName = "Enhanced Rendering/Cinematic Reflections")]
    public sealed class CinematicReflectionModule : RenderingModule
    {
        public Shader reflectionShader;
        public override ScriptableRenderPassInput RequiredInputs => ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
        public override IRenderingModuleRuntime CreateRuntime() =>
            reflectionShader != null && reflectionShader.isSupported
                ? new CinematicReflectionRuntime(reflectionShader) : null;
    }
}
