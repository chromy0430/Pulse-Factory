using UnityEngine;
using UnityEngine.EventSystems;
using PulseFactory.Progression;

namespace PulseFactory.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PFMapOverview : UnityEngine.UI.MaskableGraphic, IPointerClickHandler
    {
        public ProgressionUI owner;
        private Vector2 min,max;
        private float nextRefresh;
        private void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(owner==null||owner.state?.catalog==null)return;
            min=Vector2.one*float.MaxValue;max=Vector2.one*float.MinValue;
            bool found=false;
            foreach(var n in owner.state.catalog.nodes)if(n!=null&&n.kind==owner.CurrentKind){min=Vector2.Min(min,n.position);max=Vector2.Max(max,n.position+ProgressionMapViewport.NodeSize);found=true;}
            if(!found)return;min-=Vector2.one*100;max+=Vector2.one*100;
            Quad(vh,rectTransform.rect,new Color(.04f,.06f,.07f,.96f));
            foreach(var n in owner.state.catalog.nodes)
            {
                if(n==null||n.kind!=owner.CurrentKind)continue;
                Vector2 a=Point(n.position+ProgressionMapViewport.NodeSize*.5f);
                foreach(var parent in n.prerequisites)
                    if(parent!=null&&parent.kind==n.kind)Line(vh,a,Point(parent.position+ProgressionMapViewport.NodeSize*.5f),new Color(.6f,.67f,.69f));
                Quad(vh,new Rect(a-Vector2.one*2,Vector2.one*4),owner.state.Level(n)>0?PFUI.Theme.mint:Color.white);
            }
            Vector2 half=owner.MapViewportSize/(2*owner.Zoom);
            Vector2 lo=Point(owner.MapCenter-half),hi=Point(owner.MapCenter+half);
            lo=Clamp(lo);hi=Clamp(hi);
            Line(vh,lo,new Vector2(hi.x,lo.y),PFUI.Theme.mint);Line(vh,new Vector2(hi.x,lo.y),hi,PFUI.Theme.mint);
            Line(vh,hi,new Vector2(lo.x,hi.y),PFUI.Theme.mint);Line(vh,new Vector2(lo.x,hi.y),lo,PFUI.Theme.mint);
        }
        private Vector2 Point(Vector2 p){var r=rectTransform.rect;return new Vector2(r.xMin+8+(p.x-min.x)/(max.x-min.x)*(r.width-16),r.yMax-8-(p.y-min.y)/(max.y-min.y)*(r.height-16));}
        private Vector2 Clamp(Vector2 p){var r=rectTransform.rect;return new Vector2(Mathf.Clamp(p.x,r.xMin+2,r.xMax-2),Mathf.Clamp(p.y,r.yMin+2,r.yMax-2));}
        private static void Quad(UnityEngine.UI.VertexHelper vh,Rect r,Color c)
        {
            int i=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        private static void Line(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Color c)
        {
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*.6f;int i=vh.currentVertCount;
            vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        public void OnPointerClick(PointerEventData e)
        {
            if(owner==null||max.x<=min.x)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);
            var r=rectTransform.rect;owner.CenterMap(new Vector2(Mathf.Lerp(min.x,max.x,Mathf.InverseLerp(r.xMin+8,r.xMax-8,p.x)),Mathf.Lerp(min.y,max.y,Mathf.InverseLerp(r.yMax-8,r.yMin+8,p.y))));
        }
    }
}
