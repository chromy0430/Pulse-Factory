using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
public static class PulseMaterialMerge {
 public static void Run(){
  foreach(var playback in Object.FindObjectsByType<PulseModelPlayback>(FindObjectsSortMode.None)){
   var go=playback.gameObject;string dir="Assets/PulseFactory/"+playback.model;
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/"+playback.model+"_Atlas.png");
   var opaque=new Material(Shader.Find("PulseFactory/UnifiedModel"));opaque.SetTexture("_BaseMap",tex);AssetDatabase.CreateAsset(opaque,dir+"/Unified_Opaque.mat");
   Material transparent=null;if(playback.model.StartsWith("Hyperloop")){transparent=new Material(opaque);transparent.SetFloat("_SrcBlend",5);transparent.SetFloat("_DstBlend",10);transparent.SetFloat("_ZWrite",0);transparent.renderQueue=3000;transparent.SetOverrideTag("RenderType","Transparent");AssetDatabase.CreateAsset(transparent,dir+"/Unified_Transparent.mat");}
   Directory.CreateDirectory(dir+"/UnifiedMeshes");int serial=0;
   foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){
    var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var src=mf.sharedMesh;
    var vertices=src.vertices;var normals=src.normals;var positions=new List<Vector3>();var norms=new List<Vector3>();var uv=new List<Vector2>();var surface=new List<Vector2>();var effects=new List<Vector2>();var progress=new List<Vector2>();var colors=new List<Color>();var solidTris=new List<int>();var glassTris=new List<int>();
    var bounds=r.bounds;var mats=r.sharedMaterials;
    int mode=0;Color glow=Color.black;
    if(r.name.StartsWith("Central energy heart")){mode=1;glow=new Color(.03f,.55f,1);}
    if(r.name.StartsWith("Energy chamber")){mode=2;glow=new Color(.02f,.7f,1);}
    if(r.name.StartsWith("Storage cell")){mode=3+int.Parse(Regex.Match(r.name,@"cell (\d)").Groups[1].Value);glow=new Color(.03f,1,.22f);}
    if(r.name.StartsWith("Charge band")){var match=Regex.Match(r.name,@"band (\d) (\d)");mode=10+5*int.Parse(match.Groups[1].Value)+int.Parse(match.Groups[2].Value);glow=new Color(.03f,1,.22f);}
    if(r.name.StartsWith("Energy conductor")){mode=30;glow=new Color(.1f,.7f,1);}
    for(int sm=0;sm<src.subMeshCount;sm++){
     var mat=mats[Mathf.Min(sm,mats.Length-1)];bool glass=transparent&&(mat.renderQueue>=3000);float alpha=glass?(mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor").a:.2f):1;
     Vector2 coord=mat.HasProperty("_BaseMap")?mat.GetTextureOffset("_BaseMap"):FindColor(tex,new Color(.025f,.06f,.07f));
     float metallic=mat.HasProperty("_Metallic")?mat.GetFloat("_Metallic"):.15f,smooth=mat.HasProperty("_Smoothness")?mat.GetFloat("_Smoothness"):.5f;
     Color emission=mode>0?glow:(mat.HasProperty("_EmissionColor")?mat.GetColor("_EmissionColor"):Color.black);float strength=mode>0?1:Mathf.Max(emission.r,Mathf.Max(emission.g,emission.b));if(mode==0&&strength>0)emission/=strength;emission.a=alpha;
     foreach(int vi in src.GetTriangles(sm)){var world=r.transform.TransformPoint(vertices[vi]);int idx=positions.Count;positions.Add(vertices[vi]);norms.Add(normals.Length==vertices.Length?normals[vi]:Vector3.up);uv.Add(coord);surface.Add(new Vector2(metallic,smooth));effects.Add(new Vector2(strength,mode));progress.Add(new Vector2(Mathf.InverseLerp(bounds.min.y,bounds.max.y,world.y),Mathf.InverseLerp(bounds.min.z,bounds.max.z,world.z)));colors.Add(emission);(glass?glassTris:solidTris).Add(idx);}
    }
    var mesh=new Mesh();mesh.name=r.name+" Unified";if(positions.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(positions);mesh.SetNormals(norms);mesh.SetUVs(0,uv);mesh.SetUVs(1,surface);mesh.SetUVs(2,effects);mesh.SetUVs(3,progress);mesh.SetColors(colors);
    var assigned=new List<Material>();mesh.subMeshCount=(solidTris.Count>0?1:0)+(glassTris.Count>0?1:0);int sub=0;if(solidTris.Count>0){mesh.SetTriangles(solidTris,sub++);assigned.Add(opaque);}if(glassTris.Count>0){mesh.SetTriangles(glassTris,sub++);assigned.Add(transparent);}mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,dir+"/UnifiedMeshes/Mesh_"+(serial++)+".asset");mf.sharedMesh=mesh;r.sharedMaterials=assigned.ToArray();r.SetPropertyBlock(null);
   }
   playback.unifiedMaterial=true;var pos=go.transform.position;go.transform.position=Vector3.zero;PrefabUtility.SaveAsPrefabAsset(go,dir+"/"+playback.model+".prefab");go.transform.position=pos;
  }
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
 }
 static Vector2 FindColor(Texture2D tex,Color target){float best=float.MaxValue;Vector2 uv=Vector2.zero;for(int y=16;y<tex.height;y+=32)for(int x=16;x<tex.width;x+=32){var c=tex.GetPixel(x,y);float e=(new Vector3(c.r,c.g,c.b)-new Vector3(target.r,target.g,target.b)).sqrMagnitude;if(e<best){best=e;uv=new Vector2((x+.5f)/tex.width,(y+.5f)/tex.height);}}return uv;}
}
