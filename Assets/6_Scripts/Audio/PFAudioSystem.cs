using UnityEngine;

namespace PulseFactory.Audio
{
    [DisallowMultipleComponent]
    public sealed class PFAudioSystem : MonoBehaviour
    {
        private static PFAudioSystem instance;
        private PFAudioSettings settings;
        private AudioSource music;
        private AudioSource effects;
        private AudioSource credit;
        private readonly System.Collections.Generic.Dictionary<AudioClip, int> lastFrame = new();
        private int nextIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            var config = Resources.Load<PFAudioSettings>("PFAudioSettings");
            if (config == null) return;

            var root = new GameObject("PF Audio");
            var system = root.AddComponent<PFAudioSystem>();
            system.Initialize(config);
            DontDestroyOnLoad(root);
        }

        private void Initialize(PFAudioSettings config)
        {
            instance = this;
            settings = config;
            music = CreateChannel("BGM");
            effects = CreateChannel("SFX");
            credit = CreateChannel("Credit");
            ApplyVolumes();
            PlayNextTrack();
        }

        private AudioSource CreateChannel(string channelName)
        {
            var channel = new GameObject(channelName);
            channel.transform.SetParent(transform, false);
            var source = channel.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void Update()
        {
            if (settings == null) return;
            ApplyVolumes();
            if (!AudioListener.pause && music.clip != null && !music.isPlaying)
                PlayNextTrack();
        }

        private void ApplyVolumes()
        {
            if (music.volume != settings.MusicGain) music.volume = settings.MusicGain;
            if (credit.volume != settings.EffectsGain) credit.volume = settings.EffectsGain;
            if (effects.volume != settings.EffectsGain) effects.volume = settings.EffectsGain;
        }

        private void PlayNextTrack()
        {
            music.clip = settings.NextTrack(ref nextIndex);
            if (music.clip != null) music.Play();
        }

        public static bool TryPlaySfx(string id, float volumeScale = 1f)
        {
            if (instance == null || !instance.isActiveAndEnabled) return false;
            AudioClip clip = instance.settings.FindEffect(id);
            if (clip == null) return false;
            if (id == GameSounds.Credit)
            {
                // Multiple rewards can arrive on the same pulse. Keep one credit voice.
                if (!instance.credit.isPlaying)
                {
                    instance.credit.clip = clip;
                    instance.credit.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
                }
                return true;
            }

            if (instance.lastFrame.TryGetValue(clip, out int frame) && frame == Time.frameCount)
                return true;
            instance.lastFrame[clip] = Time.frameCount;
            instance.effects.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            return true;
        }

        private void OnDisable()
        {
            if (music != null) music.Stop();
            if (effects != null) effects.Stop();
            if (credit != null) credit.Stop();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
