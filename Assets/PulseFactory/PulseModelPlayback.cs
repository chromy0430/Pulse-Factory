using UnityEngine;
using System.Text.RegularExpressions;
public class PulseModelPlayback:MonoBehaviour {
 public AnimationClip clip;
 public string model;
 public float elapsed;
 public bool unifiedMaterial;
 Renderer[] parts;
 MaterialPropertyBlock block;
 void Awake(){parts=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();}
 void Update(){
  elapsed=Time.timeSinceLevelLoad;
  if(clip){var pos=transform.localPosition;var rot=transform.localRotation;var scale=transform.localScale;clip.SampleAnimation(gameObject,Mathf.Repeat(elapsed,clip.length+1f/30f));transform.localPosition=pos;transform.localRotation=rot;transform.localScale=scale;}
  if(unifiedMaterial)return;
  foreach(var r in parts){
   string n=r.name;block.Clear();
   if(n.StartsWith("Central energy heart")){float t=elapsed%3f;float pulse=.6f+5f*Mathf.Pow(.5f-.5f*Mathf.Cos(t/3f*Mathf.PI*2),2);block.SetColor("_EmissionColor",new Color(.03f,.55f,1)*pulse);}
   else if(n.StartsWith("Energy chamber")){Fill(r,block,Mathf.Clamp01((elapsed%4)/3.3f),new Color(.02f,.7f,1),0);}
   else if(n.StartsWith("Storage cell")){int cell=int.Parse(Regex.Match(n,@"cell (\d)").Groups[1].Value);float[] duration={2.25f,3.75f,3f};Fill(r,block,Mathf.Clamp01((elapsed%5)/duration[cell]),new Color(.03f,1,.22f),0);}
   else if(n.StartsWith("Charge band")){var m=Regex.Match(n,@"band (\d) (\d)");int cell=int.Parse(m.Groups[1].Value),level=int.Parse(m.Groups[2].Value);float[] duration={2.25f,3.75f,3f};float fill=Mathf.Clamp01((elapsed%5)/duration[cell]);float v=Mathf.SmoothStep(0,1,Mathf.InverseLerp(level*.2f,level*.2f+.16f,fill));block.SetColor("_EmissionColor",new Color(.03f,1,.22f)*v*2);}
   else if(n.StartsWith("Energy conductor")){Fill(r,block,(elapsed%2.4f)/2.4f,new Color(.1f,.7f,1),1);}
   else continue;
   r.SetPropertyBlock(block);
  }
 }
 void Fill(Renderer r,MaterialPropertyBlock b,float f,Color c,int mode){var bounds=r.bounds;b.SetFloat("_Fill",f);b.SetFloat("_YMin",mode==0?bounds.min.y:bounds.min.z);b.SetFloat("_YMax",mode==0?bounds.max.y:bounds.max.z);b.SetFloat("_Mode",mode);b.SetColor("_GlowColor",c);}
}
