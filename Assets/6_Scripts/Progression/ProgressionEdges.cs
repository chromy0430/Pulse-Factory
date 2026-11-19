using UnityEngine;
using PulseFactory.UI;

namespace PulseFactory.Progression
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ProgressionEdges : UnityEngine.UI.MaskableGraphic
    {
        public ProgressionCatalogSO catalog;
        public ProgressionKind kind;
        public Vector2 origin;
        public ProgressionState state;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(catalog==null)return;
            foreach(var node in catalog.nodes)
            {
                if(node==null||node.kind!=kind)continue;
                foreach(var parent in node.prerequisites)
                {
                    if(parent==null||parent.kind!=kind||!catalog.Contains(parent))continue;
                    Vector2 a=parent.position-origin+ProgressionMapViewport.NodeSize*.5f;
                    Vector2 b=node.position-origin+ProgressionMapViewport.NodeSize*.5f;
                    a.y=-a.y;b.y=-b.y;
                    bool locked=state!=null&&!state.CanAccess(node,out _);
                    bool complete=state!=null&&state.Level(parent)>=parent.MaxLevel;
                    Color tint=PFUI.Theme==null?color:complete?PFUI.Theme.mint:locked?PFUI.Theme.muted:PFUI.Theme.text;
                    if(locked)
                    {
                        float length=Vector2.Distance(a,b);
                        for(float d=0;d<length;d+=18)Line(vh,Vector2.Lerp(a,b,d/length),Vector2.Lerp(a,b,Mathf.Min(d+10,length)/length),tint);
                    }
                    else Line(vh,a,b,tint);
                }
            }
        }
        private static void Line(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Color tint)
        {
            var direction=(b-a).normalized;var normal=new Vector2(-direction.y,direction.x)*1.3f;int start=vh.currentVertCount;
            vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
