using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Atmosphere
{
    sealed class CinematicAtmosphereRuntime : IRenderingModuleRuntime
    {
        static readonly int SourceId = Shader.PropertyToID("_BlitTexture");
        static readonly int DepthId = Shader.PropertyToID("_ERSceneDepth");
        static readonly int FogId = Shader.PropertyToID("_ERFog");
        static readonly int DistanceId = Shader.PropertyToID("_ERFogDistance");
        static readonly int ShadowId = Shader.PropertyToID("_MainLightShadowmapTexture");
        static readonly int MediumId = Shader.PropertyToID("_ERMedium");
        static readonly int LightingId = Shader.PropertyToID("_ERLighting");
        static readonly int AmbientId = Shader.PropertyToID("_ERAmbient");
        static readonly int AlbedoId = Shader.PropertyToID("_ERAlbedo");
        static readonly int LightDirId = Shader.PropertyToID("_ERLightDirection");
        static readonly int LightColorId = Shader.PropertyToID("_ERLightColor");
        static readonly int FogSizeId = Shader.PropertyToID("_ERFogSize");
        static readonly int ScaleId = Shader.PropertyToID("_BlitScaleBias");
        readonly Material material;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

        struct Settings
        {
            public Vector4 medium, lighting, lightDirection, fogSize;
            public Color ambient, albedo, lightColor;
        }

        sealed class PassData
        {
            public TextureHandle source, depth, shadow, fog, distance;
            public Material material;
            public MaterialPropertyBlock properties;
            public Settings settings;
            public int shaderPass;
        }

        public CinematicAtmosphereRuntime(Shader shader) => material = CoreUtils.CreateEngineMaterial(shader);

        public bool IsActive(VolumeStack stack)
        {
            var volume = stack?.GetComponent<CinematicAtmosphereVolume>();
            return material != null && volume != null && volume.IsActive();
        }

        public TextureHandle Record(RenderGraph graph, ContextContainer frameData,
            TextureHandle source, VolumeStack stack)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (!resources.cameraDepthTexture.IsValid()) return source;
            var volume = stack.GetComponent<CinematicAtmosphereVolume>();
            var original = graph.GetTextureDesc(source);
            // Phase 2 does not support dynamic-resolution or XR texture layouts.
            if (original.useDynamicScale || original.useDynamicScaleExplicit
                || original.dimension != TextureDimension.Tex2D) return source;
            int divisor = volume.halfResolution.value ? 2 : 1;
            var fogDesc = original;
            fogDesc.width = Mathf.Max(1, (original.width + divisor - 1) / divisor);
            fogDesc.height = Mathf.Max(1, (original.height + divisor - 1) / divisor);
            fogDesc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
            fogDesc.depthBufferBits = DepthBits.None;
            fogDesc.msaaSamples = MSAASamples.None;
            fogDesc.clearBuffer = false;
            fogDesc.filterMode = FilterMode.Point;
            fogDesc.name = "ER Atmosphere / Scattering and Transmittance";
            var fog = graph.CreateTexture(fogDesc);
            var distanceDesc = fogDesc;
            distanceDesc.colorFormat = GraphicsFormat.R32_SFloat;
            distanceDesc.name = "ER Atmosphere / Ray Distance";
            var distance = graph.CreateTexture(distanceDesc);

            var lightData = frameData.Get<UniversalLightData>();
            var shadows = frameData.Get<UniversalShadowData>();
            var lightColor = Color.black;
            Vector4 lightDirection = new Vector4(0, 1, 0, 0);
            TextureHandle shadow = TextureHandle.nullHandle;
            int index = lightData.mainLightIndex;
            if (index >= 0 && index < lightData.visibleLights.Length)
            {
                var light = lightData.visibleLights[index];
                if (light.lightType == LightType.Directional)
                {
                    lightColor = light.finalColor * volume.directionalStrength.value;
                    lightDirection = -light.localToWorldMatrix.GetColumn(2);
                    bool receive = volume.receiveShadows.value && shadows.supportsMainLightShadows
                        && light.light != null && light.light.shadows != LightShadows.None
                        && resources.mainShadowsTexture.IsValid();
                    if (receive) shadow = resources.mainShadowsTexture;
                }
            }
            var settings = new Settings
            {
                medium = new Vector4(volume.density.value, volume.maxDistance.value,
                    volume.baseHeight.value, volume.heightFalloff.value),
                lighting = new Vector4(volume.anisotropy.value, volume.steps.value,
                    shadow.IsValid() ? 1 : 0, shadows.mainLightShadowCascadesCount),
                ambient = volume.ambientColor.value.linear * volume.ambientStrength.value,
                albedo = volume.albedo.value.linear,
                lightDirection = lightDirection,
                lightColor = lightColor,
                fogSize = new Vector4(fogDesc.width, fogDesc.height, 1f / fogDesc.width, 1f / fogDesc.height)
            };
            AddPass(graph, "ER Atmosphere / Integrate", source, resources.cameraDepthTexture,
                shadow, TextureHandle.nullHandle, TextureHandle.nullHandle, fog, distance, settings, 0);
            var outputDesc = original;
            outputDesc.depthBufferBits = DepthBits.None;
            outputDesc.msaaSamples = MSAASamples.None;
            outputDesc.clearBuffer = false;
            outputDesc.name = "ER Atmosphere / Depth Aware Composite";
            var output = graph.CreateTexture(outputDesc);
            AddPass(graph, outputDesc.name, source, resources.cameraDepthTexture, shadow,
                fog, distance, output, TextureHandle.nullHandle, settings, 1);
            return output;
        }

        void AddPass(RenderGraph graph, string name, TextureHandle source, TextureHandle depth,
            TextureHandle shadow, TextureHandle fog, TextureHandle distance, TextureHandle output,
            TextureHandle distanceOutput, Settings settings, int shaderPass)
        {
            using var builder = graph.AddRasterRenderPass<PassData>(name, out var data);
            data.source = source;
            data.depth = depth;
            data.shadow = shadow;
            data.fog = fog;
            data.distance = distance;
            data.material = material;
            data.properties = properties;
            data.settings = settings;
            data.shaderPass = shaderPass;
            builder.UseTexture(depth, AccessFlags.Read);
            if (shaderPass == 1) builder.UseTexture(source, AccessFlags.Read);
            if (shadow.IsValid()) builder.UseTexture(shadow, AccessFlags.Read);
            if (fog.IsValid()) builder.UseTexture(fog, AccessFlags.Read);
            if (distance.IsValid()) builder.UseTexture(distance, AccessFlags.Read);
            builder.SetRenderAttachment(output, 0, AccessFlags.Write);
            if (distanceOutput.IsValid()) builder.SetRenderAttachment(distanceOutput, 1, AccessFlags.Write);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
            {
                var block = d.properties;
                block.Clear();
                if (d.shaderPass == 1) block.SetTexture(SourceId, d.source);
                block.SetTexture(DepthId, d.depth);
                if (d.shadow.IsValid()) block.SetTexture(ShadowId, d.shadow);
                if (d.fog.IsValid()) block.SetTexture(FogId, d.fog);
                if (d.distance.IsValid()) block.SetTexture(DistanceId, d.distance);
                block.SetVector(ScaleId, new Vector4(1, 1, 0, 0));
                block.SetVector(MediumId, d.settings.medium);
                block.SetVector(LightingId, d.settings.lighting);
                block.SetVector(FogSizeId, d.settings.fogSize);
                block.SetVector(LightDirId, d.settings.lightDirection);
                block.SetColor(LightColorId, d.settings.lightColor);
                block.SetColor(AmbientId, d.settings.ambient);
                block.SetColor(AlbedoId, d.settings.albedo);
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.shaderPass,
                    MeshTopology.Triangles, 3, 1, block);
            });
        }

        public void Dispose() => CoreUtils.Destroy(material);
    }
}
