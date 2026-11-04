using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
public static class PulseAnimationSetup {
 static string Clean(string s)=>Regex.Replace(s,@"(?:\.\d+)+$","");
 public static void Run(){
  string[] names={"Core","Research_Factory","Amplifier_Factory","Capacitor_Factory","Hyperloop_Entrance","Hyperloop_Passage"};
  foreach(string name in names){
   string dir="Assets/PulseFactory/"+name;var old=GameObject.Find(name);Vector3 pos=old?old.transform.position:Vector3.zero;if(old)Object.DestroyImmediate(old);
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>(dir+"/"+name+".fbx");var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name=name;
   var available=AssetDatabase.FindAssets("t:Material",new[]{dir}).Select(id=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
   foreach(var r in go.GetComponentsInChildren<Renderer>()){
    var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++){var match=available.FirstOrDefault(m=>Clean(m.name)==Clean(mats[j].name));if(match)mats[j]=match;}r.sharedMaterials=mats;
    if(r.name.StartsWith("Energy chamber")||r.name.StartsWith("Storage cell")||r.name.StartsWith("Energy conductor")){
     string path=dir+"/"+Regex.Replace(r.name,@"[^A-Za-z0-9_]","_")+"_Charge.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("PulseFactory/ChargePreview"));AssetDatabase.CreateAsset(mat,path);}r.sharedMaterial=mat;
    }
   }
   var playback=go.AddComponent<PulseModelPlayback>();playback.model=name;playback.clip=AssetDatabase.LoadAllAssetsAtPath(dir+"/"+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
   PrefabUtility.SaveAsPrefabAsset(go,dir+"/"+name+".prefab");go.transform.position=pos;
  }
  EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
 }
}
