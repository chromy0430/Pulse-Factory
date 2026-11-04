using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EnhancedRendering.Editor
{
    static class CinematicLabScene
    {
        public static void Populate(VolumeProfile profile)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.025f, 0.03f, 0.05f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            var stone = Material("Graphite", new Color(0.09f, 0.105f, 0.14f), 0.25f, 0.65f);
            var floor = Material("Slate", new Color(0.055f, 0.065f, 0.085f), 0.4f, 0.78f);
            var metal = Material("Titanium", new Color(0.55f, 0.60f, 0.67f), 0.85f, 0.87f);
            var ceramic = Material("Ivory", new Color(0.75f, 0.71f, 0.63f), 0.05f, 0.45f);
            var gold = Material("Copper", new Color(0.64f, 0.28f, 0.09f), 0.8f, 0.7f);
            var amber = Material("Amber Emission", Color.black, 0, 0.5f, new Color(1f, 0.32f, 0.065f) * 12f);
            var blue = Material("Blue Emission", Color.black, 0, 0.5f, new Color(0.08f, 0.4f, 1f) * 9f);

            Box("Gallery floor", new Vector3(0,-0.3f,4), new Vector3(14,0.5f,25), floor);
            Box("Back wall", new Vector3(0,3,15), new Vector3(14,7,0.5f), stone);
            for (int i = 0; i < 5; i++)
            {
                float z = i * 3f;
                Box("Left pier " + i, new Vector3(-4.7f,2.7f,z), new Vector3(0.7f,5.5f,0.8f), stone);
                Box("Right pier " + i, new Vector3(4.7f,2.7f,z), new Vector3(0.7f,5.5f,0.8f), stone);
                Box("Lintel " + i, new Vector3(0,5.3f,z), new Vector3(10,0.5f,0.8f), stone);
                Box("Amber strip " + i, new Vector3(-4.28f,2.5f,z-0.42f), new Vector3(0.09f,4.4f,0.05f), amber);
                Box("Blue strip " + i, new Vector3(4.28f,2.5f,z-0.42f), new Vector3(0.09f,4.4f,0.05f), blue);
            }
            Box("Far light", new Vector3(0,2.9f,14.65f), new Vector3(2.2f,3.8f,0.08f), amber);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 2.5f;
                Box("Plinth " + i, new Vector3(x,0.55f,3.5f+i*0.3f), new Vector3(1.8f,1.1f,1.8f), stone);
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Material study " + i;
                sphere.transform.position = new Vector3(x,1.85f,3.5f+i*0.3f);
                sphere.transform.localScale = Vector3.one * 1.5f;
                sphere.GetComponent<Renderer>().sharedMaterial = i == 0 ? ceramic : i == 1 ? metal : gold;
            }
            var key = Light("Soft key", LightType.Directional, new Color(0.72f,0.82f,1f), 1.8f, Vector3.zero);
            key.transform.rotation = Quaternion.Euler(42,-28,0);
            key.shadows = LightShadows.Soft;
            RenderSettings.sun = key;
            for (int i = 0; i < 3; i++)
            {
                Light("Warm bounce " + i, LightType.Point, new Color(1,0.36f,0.12f), 10f,
                    new Vector3(-3.7f,2.5f,i*4f+1));
                Light("Cool bounce " + i, LightType.Point, new Color(0.12f,0.4f,1), 12f,
                    new Vector3(3.7f,2.5f,i*4f+1));
            }
            var camera = new GameObject("Cinematic Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0.35f,2.4f,-9.8f);
            camera.transform.LookAt(new Vector3(0,2.15f,5.8f));
            camera.fieldOfView = 48;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 70f;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f,0.008f,0.018f);
            var extra = camera.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = true;
            extra.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var volume = new GameObject("Cinematic Look", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            volume.sharedProfile = profile;
        }

        static Material Material(string name, Color color, float metallic, float smoothness, Color emission = default)
        {
            string path = CinematicLabSetup.Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetPositionAndRotation(position, Quaternion.identity);
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Light Light(string name, LightType type, Color color, float intensity, Vector3 position)
        {
            var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = 10;
            light.transform.position = position;
            return light;
        }
    }
}
