using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;

public static class PulseAssetSetup {
 [Serializable] public class MatInfo {public string name; public float[] color,emission; public float metallic,smoothness,emissionStrength,alpha;}
 [Serializable] public class MatList {public MatInfo[] materials;}
 public static void Run(){
  string root="Assets/PulseFactory";
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  string[] names={"Core","Research_Factory","Amplifier_Factory","Capacitor_Factory","Hyperloop_Entrance","Hyperloop_Passage"};
  int index=0;
  foreach(string name in names){
   string dir=root+"/"+name, png=dir+"/"+name+"_Atlas.png", fbx=dir+"/"+name+".fbx";
   var ti=(TextureImporter)AssetImporter.GetAtPath(png);ti.sRGBTexture=false;ti.mipmapEnabled=false;ti.isReadable=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(png);
   var data=JsonUtility.FromJson<MatList>(File.ReadAllText(dir+"/Materials.json"));
   var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;
   foreach(var info in data.materials){
    string path=dir+"/"+Sanitize(info.name)+".mat";
    var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));
    Color target=new Color(info.color[0],info.color[1],info.color[2],1);
    float best=float.MaxValue;Vector2 uv=Vector2.zero;
    for(int y=16;y<tex.height;y+=32)for(int x=16;x<tex.width;x+=32){Color c=tex.GetPixel(x,y);float err=(new Vector3(c.r,c.g,c.b)-new Vector3(target.r,target.g,target.b)).sqrMagnitude;if(err<best){best=err;uv=new Vector2((x+.5f)/tex.width,(y+.5f)/tex.height);}}
    mat.SetTexture("_BaseMap",tex);mat.SetTextureScale("_BaseMap",Vector2.zero);mat.SetTextureOffset("_BaseMap",uv);mat.SetColor("_BaseColor",Color.white);
    mat.SetFloat("_Metallic",info.metallic);mat.SetFloat("_Smoothness",info.smoothness);
    if(info.emissionStrength>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(info.emission[0],info.emission[1],info.emission[2])*info.emissionStrength);}
    if(info.name.IndexOf("Transparent",StringComparison.OrdinalIgnoreCase)>=0 || info.alpha<.99f){mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetColor("_BaseColor",new Color(1,1,1,info.alpha<.99f?info.alpha:.2f));mat.renderQueue=3000;}
    AssetDatabase.CreateAsset(mat,path);
    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),info.name),mat);
   }
   importer.SaveAndReimport();
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
   var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name=name;go.transform.position=Vector3.zero;
   PrefabUtility.SaveAsPrefabAsset(go,dir+"/"+name+".prefab");go.transform.position=new Vector3((index%3)*5,0,(index/3)*6);index++;
  }
  string boardPath=root+"/Power_Board_V2/Power_Board_Static_RGBA.png";
  var boardTI=(TextureImporter)AssetImporter.GetAtPath(boardPath);boardTI.sRGBTexture=false;boardTI.alphaSource=TextureImporterAlphaSource.FromInput;boardTI.alphaIsTransparency=false;boardTI.wrapMode=TextureWrapMode.Repeat;boardTI.SaveAndReimport();
  var floorMat=new Material(Shader.Find("PulseFactory/StaticPowerBoard"));floorMat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(boardPath));AssetDatabase.CreateAsset(floorMat,root+"/Power_Board_V2/Power_Board_Static.mat");
  var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Power Board Static Preview";floor.transform.position=new Vector3(5,-.02f,3);floor.transform.localScale=new Vector3(1.8f,1,1.5f);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
  var light=new GameObject("Preview Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.4f,.45f);
  var camera=new GameObject("Preview Camera").AddComponent<Camera>();camera.transform.position=new Vector3(17,16,-16);camera.transform.LookAt(new Vector3(5,0,3));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.08f,.1f);camera.tag="MainCamera";
  EditorSceneManager.SaveScene(scene,"Assets/PulseFactory/Asset_Showcase.unity");AssetDatabase.SaveAssets();
  File.WriteAllText("Assets/PulseFactory/IMPORT_COMPLETE.txt","6 FBX models, 7 textures, 6 prefabs. Model transform animation imported. Procedural Blender material animation requires separate Unity implementation. Power board is static.");
  Debug.Log("PULSE_IMPORT_COMPLETE");
 }
 static string Sanitize(string s){foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');return s;}
}
