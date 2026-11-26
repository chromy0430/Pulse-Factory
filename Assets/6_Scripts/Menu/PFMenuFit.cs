using UnityEngine;
namespace PulseFactory.Menu
{
    [ExecuteAlways]
    public sealed class PFMenuFit : MonoBehaviour
    {
        private void OnEnable()=>Fit();
        private void LateUpdate()=>Fit();
        private void OnRectTransformDimensionsChange()=>Fit();
        private void Fit()
        {
            if (!(transform.parent is RectTransform parent)) return;
            float scale=Mathf.Min(parent.rect.width/1280f,parent.rect.height/720f);
            if(scale>0 && !Mathf.Approximately(transform.localScale.x,scale)) transform.localScale=Vector3.one*scale;
        }
    }
}
