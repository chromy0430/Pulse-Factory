using UnityEngine;
namespace PulseFactory.Menu
{
    [ExecuteAlways]
    public sealed class PFMenuCoreGlow : MonoBehaviour
    {
        public Transform core;
        public Light glow;
        private void Update()
        {
            float wave=.5f+.5f*Mathf.Sin(Time.realtimeSinceStartup*1.4f);
            if(core) core.localScale=Vector3.one*(1f+.025f*wave);
            if(glow) glow.intensity=2f+wave*3f;
        }
    }
}
