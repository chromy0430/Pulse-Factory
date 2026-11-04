using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace EnhancedRendering.Bloom
{
    sealed class CinematicBloomRuntime : IRenderingModuleRuntime
    {
        static readonly int SourceId = Shader.PropertyToID("_BlitTexture");
        static readonly int LowId = Shader.PropertyToID("_BloomLow");
        static readonly int ScaleId = Shader.PropertyToID("_BlitScaleBias");
        static readonly int TexelId = Shader.PropertyToID("_SourceTexel");
        static readonly int ParamsId = Shader.PropertyToID("_BloomParams");
        static readonly int TintId = Shader.PropertyToID("_BloomTint");
        readonly Material material;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        readonly TextureHandle[] pyramid = new TextureHandle[BloomPyramid.MaxLevels];
        readonly string[] downNames = new string[BloomPyramid.MaxLevels];
        readonly string[] upNames = new string[BloomPyramid.MaxLevels];

        sealed class PassData
        {
            public TextureHandle source, low;
            public Material material;
            public MaterialPropertyBlock properties;
            public int shaderPass;
            public Vector4 texel, parameters;
            public Color tint;
        }

        public CinematicBloomRuntime(Shader shader)
        {
            material = CoreUtils.CreateEngineMaterial(shader);
            for (int i = 0; i < BloomPyramid.MaxLevels; i++)
            {
                downNames[i] = "ER Bloom / Down " + i;
                upNames[i] = "ER Bloom / Up " + i;
            }
        }

        public bool IsActive(VolumeStack stack)
        {
            var settings = stack?.GetComponent<CinematicBloomVolume>();
            return material != null && settings != null && settings.IsActive();
        }

        public TextureHandle Record(RenderGraph graph, ContextContainer frameData,
            TextureHandle source, VolumeStack stack)
        {
            var settings = stack.GetComponent<CinematicBloomVolume>();
            var original = graph.GetTextureDesc(source);
            int count = BloomPyramid.LevelCount(original.width, original.height, settings.levels.value);
            var parameters = new Vector4(settings.threshold.value, settings.softKnee.value,
                settings.scatter.value, settings.clamp.value);
            TextureHandle previous = source;
            for (int i = 0; i < count; i++)
            {
                var size = BloomPyramid.SizeAt(original.width, original.height, i);
                var desc = original;
                desc.width = size.x;
                desc.height = size.y;
                desc.name = downNames[i];
                desc.clearBuffer = false;
                desc.depthBufferBits = DepthBits.None;
                desc.msaaSamples = MSAASamples.None;
                desc.filterMode = FilterMode.Bilinear;
                pyramid[i] = graph.CreateTexture(desc);
                AddPass(graph, downNames[i], previous, TextureHandle.nullHandle,
                    pyramid[i], i == 0 ? 0 : 1, parameters, Color.white);
                previous = pyramid[i];
            }
            for (int i = count - 2; i >= 0; i--)
            {
                var desc = graph.GetTextureDesc(pyramid[i]);
                desc.name = upNames[i];
                var result = graph.CreateTexture(desc);
                AddPass(graph, upNames[i], pyramid[i], previous, result, 2, parameters, Color.white);
                previous = result;
            }
            var outputDesc = original;
            outputDesc.name = "ER Bloom / HDR Composite";
            outputDesc.clearBuffer = false;
            outputDesc.depthBufferBits = DepthBits.None;
            outputDesc.msaaSamples = MSAASamples.None;
            var output = graph.CreateTexture(outputDesc);
            var tint = settings.tint.value.linear;
            tint *= settings.intensity.value;
            AddPass(graph, outputDesc.name, source, previous, output, 3, parameters, tint);
            return output;
        }

        void AddPass(RenderGraph graph, string name, TextureHandle source, TextureHandle low,
            TextureHandle destination, int shaderPass, Vector4 parameters, Color tint)
        {
            using var builder = graph.AddRasterRenderPass<PassData>(name, out var data);
            var sampled = graph.GetTextureDesc(low.IsValid() ? low : source);
            data.source = source;
            data.low = low;
            data.material = material;
            data.properties = properties;
            data.shaderPass = shaderPass;
            data.texel = new Vector4(1f / sampled.width, 1f / sampled.height, sampled.width, sampled.height);
            data.parameters = parameters;
            data.tint = tint;
            builder.UseTexture(source, AccessFlags.Read);
            if (low.IsValid()) builder.UseTexture(low, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
            {
                // Populate only during execution: handles are resolved and each draw snapshots the block.
                d.properties.Clear();
                d.properties.SetTexture(SourceId, d.source);
                if (d.low.IsValid()) d.properties.SetTexture(LowId, d.low);
                d.properties.SetVector(ScaleId, new Vector4(1, 1, 0, 0));
                d.properties.SetVector(TexelId, d.texel);
                d.properties.SetVector(ParamsId, d.parameters);
                d.properties.SetColor(TintId, d.tint);
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.shaderPass,
                    MeshTopology.Triangles, 3, 1, d.properties);
            });
        }

        public void Dispose() => CoreUtils.Destroy(material);
    }
}
