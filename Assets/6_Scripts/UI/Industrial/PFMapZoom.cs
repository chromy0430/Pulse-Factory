using UnityEngine;
using UnityEngine.EventSystems;
using PulseFactory.Progression;

namespace PulseFactory.UI
{
    public sealed class PFMapZoom : MonoBehaviour, IScrollHandler
    {
        public ProgressionUI owner;
        public void OnScroll(PointerEventData data){if(owner!=null)owner.SetZoom(owner.Zoom*Mathf.Pow(1.12f,data.scrollDelta.y));}
    }
}
