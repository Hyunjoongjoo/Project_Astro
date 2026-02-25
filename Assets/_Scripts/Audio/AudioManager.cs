using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManager : Singleton<AudioManager>
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer _mixer;

    [Header("Sources")]
    [SerializeField] private AudioSource _bgmA;
    [SerializeField] private AudioSource _bgmB;

    [Header("BGM Table")]
    [SerializeField] private BgmTable _bgmTable;

    [Header("BGM Crossfade")]
    [SerializeField, Min(0f)] private float _bgmFadeSeconds = 1.5f;

    private Coroutine _fadeCo;
   
    protected override void OnSingletonAwake()
    {
        PrepareBgm(_bgmA);
        PrepareBgm(_bgmB);
    }

    void Start()
    {
        LoadAndApplySavedVolumes();
    }

    private static void PrepareBgm(AudioSource s)
    {
        if (s == null) return;
        s.playOnAwake = false;
        s.loop = true;
        s.spatialBlend = 0f;
        s.volume = 0f;
    }

    #region BGM

    public void PlayBgm(SceneState state)
    {
        if (_bgmTable == null) return;
        if (!_bgmTable.TryGetClip(state, out var clip) || clip == null) return;
        CrossFadeTo(clip);
    }

    private void CrossFadeTo(AudioClip clip)
    {
        if (_bgmA == null || _bgmB == null) return;

        var from = GetDominantSource();
        var to = (from == _bgmA) ? _bgmB : _bgmA;

        if (from.isPlaying && from.clip == clip) return;

        if (to.isPlaying) to.Stop();
        to.clip = clip;
        to.volume = 0f;
        to.Play();

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoCrossFade(from, to, 1f, _bgmFadeSeconds));
    }

    private AudioSource GetDominantSource()
    {
        if (!_bgmA.isPlaying && !_bgmB.isPlaying) return _bgmA;
        return (_bgmA.volume >= _bgmB.volume) ? _bgmA : _bgmB;
    }

    private IEnumerator CoCrossFade(AudioSource from, AudioSource to, float toTarget, float seconds)
    {
        if (seconds <= 0f)
        {
            if (from != null && from.isPlaying) from.Stop();
            if (from != null) from.volume = 0f;
            if (to != null) to.volume = toTarget;
            _fadeCo = null;
            yield break;
        }

        float t = 0f;
        float fromStart = (from != null) ? from.volume : 0f;

        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / seconds);

            if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, a);
            if (to != null) to.volume = Mathf.Lerp(0f, toTarget, a);

            yield return null;
        }

        if (from != null)
        {
            from.volume = 0f;
            if (from.isPlaying) from.Stop();
            from.clip = null;
        }

        if (to != null) to.volume = toTarget;
        _fadeCo = null;
    }

    #endregion

    #region Volume

    public void LoadAndApplySavedVolumes()
    {
        SetVolume(AudioBus.Master, PlayerPrefs.GetFloat(AudioParam.MASTER_KEY, 1f));
        SetVolume(AudioBus.BGM, PlayerPrefs.GetFloat(AudioParam.BGM_KEY, 1f));
        SetVolume(AudioBus.SFX, PlayerPrefs.GetFloat(AudioParam.SFX_KEY, 1f));
    }

    public void SetVolume(AudioBus bus, float normalized01)
    {
        if (_mixer == null) return;

        string muteKey = bus switch
        {
            AudioBus.BGM => AudioParam.BGM_MUTE_KEY,
            AudioBus.SFX => AudioParam.SFX_MUTE_KEY,
            _ => AudioParam.MASTER_MUTE_KEY
        };

        bool isMuted = PlayerPrefs.GetInt(muteKey, 0) == 1;
        float finalVol = isMuted ? 0.0001f : normalized01;

        float db = NormalizedToDb(finalVol);

        string param = bus switch
        {
            AudioBus.BGM => AudioParam.BGM_PARAM,
            AudioBus.SFX => AudioParam.SFX_PARAM,
            _ => AudioParam.MASTER_PARAM
        };

        _mixer.SetFloat(param, db);
    }

    private static float NormalizedToDb(float v01)
    {
        v01 = Mathf.Clamp(v01, 0.0001f, 1f);
        return Mathf.Log10(v01) * 20f;
    }

    #endregion
}
