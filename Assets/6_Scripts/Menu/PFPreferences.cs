using UnityEngine;
namespace PulseFactory.Menu
{
    [System.Serializable]
    public class PFPreferences
    {
        public float master=.8f, music=.6f, effects=.8f;
        public bool mute, fullscreen, invertWheel;
        public float zoom=1;
        private const string Key="PF.Settings.1";
        private static PFPreferences applied;
        private static bool defaultFullscreen;
        public static PFPreferences Defaults()
        {
            var defaults=new PFPreferences {fullscreen=defaultFullscreen};
            var config=Resources.Load<PulseFactory.Audio.PFAudioSettings>("PFAudioSettings");
            if (config)
            {
                defaults.master=config.MasterVolume;
                defaults.music=config.MusicVolume;
                defaults.effects=config.EffectsVolume;
            }
            return defaults;
        }
        public static PFPreferences Current => applied ??= Read();
        private static PFPreferences Read()
        {
            var value=Defaults();
            if (PlayerPrefs.HasKey(Key)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key),value);
            return value;
        }
        public PFPreferences Copy() => (PFPreferences)MemberwiseClone();
        public void Apply()
        {
            applied=Copy();
            if (Screen.fullScreen!=fullscreen) Screen.fullScreen=fullscreen;
            PlayerPrefs.SetString(Key,JsonUtility.ToJson(applied)); PlayerPrefs.Save();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            defaultFullscreen=Screen.fullScreen;
            applied=Read();
            if (PlayerPrefs.HasKey(Key) && Screen.fullScreen!=applied.fullscreen) Screen.fullScreen=applied.fullscreen;
        }
        public static float MusicGain => Current.mute ? 0 : Current.master*Current.music;
        public static float EffectsGain => Current.mute ? 0 : Current.master*Current.effects;
        public static float ZoomFactor => Current.zoom*(Current.invertWheel?-1:1);
    }
}
