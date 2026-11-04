using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Reflections
{
    sealed class CinematicReflectionRuntime : IRenderingModuleRuntime
    {
        static readonly int SourceId = Shader.PropertyToID("_BlitTexture");
        static readonly int DepthId = Shader.PropertyToID("_CameraDepthTexture");
        static readonly int NormalsId = Shader.PropertyToID("_CameraNormalsTexture");
        static readonly int PropertiesId = Shader.PropertyToID("_ERReflectiveProperties");
        static readonly int ReflectionId = Shader.PropertyToID("_ERReflection");
        static readonly int RayId = Shader.PropertyToID("_ERRay");
        static readonly int SurfaceId = Shader.PropertyToID("_ERSurface");
        static readonly int OutputId = Shader.PropertyToID("_EROutput");
        static readonly int SizeId = Shader.PropertyToID("_ERSize");
        static readonly int ScaleId = Shader.PropertyToID("_BlitScaleBias");
        readonly Material material;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

        sealed class PassData
        {
            public TextureHandle source, depth, normals, surfaceProperties, reflection;
            public Vector4 ray, surface, output, size;
            public Material material;
            public MaterialPropertyBlock properties;
            public int shaderPass;
        }

        public CinematicReflectionRuntime(Shader shader) => material = CoreUtils.CreateEngineMaterial(shader);
        public bool IsActive(VolumeStack stack)
        {
            var volume = stack?.GetComponent<CinematicReflectionVolume>();
            return material != null && volume != null && volume.IsActive();
        }

        public TextureHandle Record(RenderGraph graph, ContextContainer frameData,
            TextureHandle source, VolumeStack stack)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (!resources.cameraDepthTexture.IsValid() || !resources.cameraNormalsTexture.IsValid()
                || !resources.activeDepthTexture.IsValid()) return source;
            var descriptor = graph.GetTextureDesc(source);
            if (descriptor.useDynamicScale || descriptor.useDynamicScaleExplicit
                || descriptor.dimension != TextureDimension.Tex2D) return source;
            var volume = stack.GetComponent<CinematicReflectionVolume>();
            descriptor.depthBufferBits = DepthBits.None;
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.clearBuffer = false;
            var surfaceProperties = ReflectionSurfacePass.Record(graph, frameData, descriptor, resources.activeDepthTexture);
            var traceDesc = descriptor;
            traceDesc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
            traceDesc.filterMode = FilterMode.Point;
            traceDesc.name = "ER Reflections / Trace";
            var reflection = graph.CreateTexture(traceDesc);
            AddPass(graph, traceDesc.name, source, resources.cameraDepthTexture, resources.cameraNormalsTexture, surfaceProperties,
                TextureHandle.nullHandle, reflection, volume, descriptor.width, descriptor.height, 0);
            descriptor.name = "ER Reflections / Composite";
            var output = graph.CreateTexture(descriptor);
            AddPass(graph, descriptor.name, source, resources.cameraDepthTexture, resources.cameraNormalsTexture, surfaceProperties,
                reflection, output, volume, descriptor.width, descriptor.height, 1);
            return output;
        }

        void AddPass(RenderGraph graph, string name, TextureHandle source, TextureHandle depth,
            TextureHandle normals, TextureHandle surfaceProperties, TextureHandle reflection, TextureHandle destination,
            CinematicReflectionVolume volume, int width, int height, int shaderPass)
        {
            using var builder = graph.AddRasterRenderPass<PassData>(name, out var data);
            data.source = source;
            data.depth = depth;
            data.normals = normals;
            data.surfaceProperties = surfaceProperties;
            data.reflection = reflection;
            data.material = material;
            data.properties = properties;
            data.shaderPass = shaderPass;
            data.ray = new Vector4(volume.maxDistance.value, volume.steps.value,
                volume.thickness.value, volume.normalBias.value);
            data.surface = new Vector4(volume.minimumSmoothness.value, volume.edgeFade.value,
                volume.radianceClamp.value, 0);
            data.output = new Vector4(volume.intensity.value, volume.debugConfidence.value ? 1 : 0, 0, 0);
            data.size = new Vector4(width, height, 1f / width, 1f / height);
            builder.UseTexture(source, AccessFlags.Read);
            if (shaderPass == 0)
            {
                builder.UseTexture(depth, AccessFlags.Read);
                builder.UseTexture(normals, AccessFlags.Read);
                builder.UseTexture(surfaceProperties, AccessFlags.Read);
            }
            else builder.UseTexture(reflection, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
            {
                var block = d.properties;
                block.Clear();
                block.SetTexture(SourceId, d.source);
                if (d.shaderPass == 0)
                {
                    block.SetTexture(DepthId, d.depth);
                    block.SetTexture(NormalsId, d.normals);
                    block.SetTexture(PropertiesId, d.surfaceProperties);
                }
                else block.SetTexture(ReflectionId, d.reflection);
                block.SetVector(RayId, d.ray);
                block.SetVector(SurfaceId, d.surface);
                block.SetVector(OutputId, d.output);
                block.SetVector(SizeId, d.size);
                block.SetVector(ScaleId, new Vector4(1, 1, 0, 0));
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.shaderPass,
                    MeshTopology.Triangles, 3, 1, block);
            });
        }

        public void Dispose() => CoreUtils.Destroy(material);
    }
}
