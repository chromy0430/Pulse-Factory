using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Reflections
{
    static class ReflectionSurfacePass
    {
        static readonly ShaderTagId SurfaceTag = new ShaderTagId("ERReflectionSurface");
        sealed class PassData { public RendererListHandle surfaces; }

        public static TextureHandle Record(RenderGraph graph, ContextContainer frame, TextureDesc descriptor,
            TextureHandle visibleDepth)
        {
            descriptor.name = "ER Reflections / Surface Properties";
            descriptor.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
            descriptor.filterMode = FilterMode.Point;
            descriptor.clearBuffer = true;
            descriptor.clearColor = Color.clear;
            var output = graph.CreateTexture(descriptor);
            var rendering = frame.Get<UniversalRenderingData>();
            var camera = frame.Get<UniversalCameraData>();
            var drawing = RenderingUtils.CreateDrawingSettings(SurfaceTag, rendering, camera,
                frame.Get<UniversalLightData>(), camera.defaultOpaqueSortFlags);
            var filtering = new FilteringSettings(RenderQueueRange.opaque);
            using var builder = graph.AddRasterRenderPass<PassData>(descriptor.name, out var data);
            data.surfaces = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
            builder.UseRendererList(data.surfaces);
            builder.SetRenderAttachment(output, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(visibleDepth, AccessFlags.Read);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
                context.cmd.DrawRendererList(pass.surfaces));
            return output;
        }
    }
}
