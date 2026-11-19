using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PulseFactory.Audio
{
    internal static class GameSounds
    {
        public const string Charge = "01_Board_Charge";
        public const string Release = "02_Core_Release";
        public const string Travel = "03_Wire_Travel";
        public const string Credit = "05_Credit";
        public const string Place = "06_Build_Place";
        public const string Connect = "07_Cable_Connect";
        public const string Demolish = "08_Demolish";
        public const string Capacity = "09_Capacity_Denied";
        public const string Research = "10_Research_Unlock";
        public const string Trait = "11_Trait_Upgrade";
        public const string Select = "12_UI_Select";
        public const string Disconnect = "13_Cable_Disconnect";

        private static readonly string[] pops = { "04_POP_1", "04_POP_2", "04_POP_3", "04_POP_4", "04_POP_5" };
        private static readonly List<BuildingRuntime> factories = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFactories() => factories.Clear();

        public static void Register(BuildingRuntime factory) => factories.Add(factory);
        public static void Unregister(BuildingRuntime factory) => factories.Remove(factory);

        public static void Pop(BuildingRuntime factory)
        {
            int overrideNumber = factory.PopNumber;
            if (overrideNumber >= 1 && overrideNumber <= pops.Length)
            {
                Play(pops[overrideNumber - 1]);
                return;
            }
            var position = factory.transform.position;
            int rank = 0;
            foreach (var other in factories)
            {
                if (other == null || other == factory) continue;
                var p = other.transform.position;
                if (p.x < position.x || (p.x == position.x && p.z < position.z))
                    rank++;
            }
            Play(pops[rank % pops.Length]);
        }

        public static void Bind(Button button)
        {
            button.onClick.RemoveListener(Click);
            button.onClick.AddListener(Click);
        }

        private static void Click() => Play(Select);
        public static void Play(string id) => PFAudioSystem.TryPlaySfx(id);
    }
}
