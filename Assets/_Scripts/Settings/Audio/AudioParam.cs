public enum AudioBus { Master, BGM, UI, SFX }

public static class AudioParam
{
    // AudioMixer Exposed Parameter Names
    public const string MASTER_PARAM = "MasterVol";
    public const string BGM_PARAM    = "BgmVol";
    public const string SFX_PARAM    = "SFXVol";

    // PlayerPrefs Keys
    public const string MASTER_KEY = "audio.master";
    public const string BGM_KEY    = "audio.bgm";
    public const string SFX_KEY    = "audio.sfx";

    // Mute Keys
    public const string MASTER_MUTE_KEY = "audio.master_mute";
    public const string BGM_MUTE_KEY = "audio.bgm_mute";
    public const string SFX_MUTE_KEY = "audio.sfx_mute";
}
