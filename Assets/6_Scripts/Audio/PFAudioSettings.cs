using UnityEngine;

namespace PulseFactory.Audio
{
    [CreateAssetMenu(menuName = "Pulse Factory/Audio Settings")]
    public sealed class PFAudioSettings : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.8f;
        [Tooltip("Tracks play from top to bottom, then repeat.")]
        [SerializeField] private AudioClip[] playlist;
        [Tooltip("Use the clip name (without extension) as the sound ID.")]
        [SerializeField] private AudioClip[] effects;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float EffectsVolume => effectsVolume;
        public float MusicGain => masterVolume * musicVolume;
        public float EffectsGain => masterVolume * effectsVolume;

        internal AudioClip NextTrack(ref int index)
        {
            int count = playlist?.Length ?? 0;
            for (int i = 0; i < count; i++)
            {
                index %= count;
                AudioClip clip = playlist[index++];
                if (clip != null) return clip;
            }
            return null;
        }

        internal AudioClip FindEffect(string id)
        {
            if (effects == null || string.IsNullOrEmpty(id)) return null;
            foreach (AudioClip clip in effects)
                if (clip != null && clip.name == id) return clip;
            return null;
        }
    }
}
