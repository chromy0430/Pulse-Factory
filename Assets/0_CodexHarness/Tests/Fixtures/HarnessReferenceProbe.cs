using UnityEngine;

namespace CodexHarness.Tests
{
    public sealed class HarnessReferenceProbe : MonoBehaviour
    {
        public Object optionalReference;
        [HideInInspector] public Object hiddenReference;
    }
}
