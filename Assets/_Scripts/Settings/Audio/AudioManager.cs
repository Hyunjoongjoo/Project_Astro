using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManager : Singleton<AudioManager>
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer _mixer;

    [Header("Sources")]
    [SerializeField] private AudioSource _bgmA;
    [SerializeField] private AudioSource _bgmB;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Sound Table")]
    [SerializeField] private BgmTable _bgmTable;
    [SerializeField] private SFXTable _sfxTable;

    [Header("BGM Crossfade")]
    [SerializeField, Min(0f)] private float _bgmFadeSeconds = 1.5f;

    [Header("Sound Stacking")]
    [SerializeField] private float soundCooldown = 0.1f;

    private Coroutine _playListCo;

    // 오디오 스태킹 (같은 소리가 다수 호출되어 사운드가 매우 커지는 현상) 방지를 위한 딕셔너리
    private Dictionary<AudioClip, float> clipLastPlayTimes = new Dictionary<AudioClip, float>();

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
        if (_playListCo != null)
        {
            StopCoroutine(_playListCo);
            _playListCo = null;
        }

        if (_bgmTable == null) return;
        if (!_bgmTable.TryGetClip(state, out var clip) || clip == null) return;

        CrossFadeTo(clip);
        _playListCo = StartCoroutine(CoBgmLoop(state));
    }

    private void CrossFadeTo(AudioClip clip)
    {
        if (_bgmA == null || _bgmB == null) return;

        var from = GetDominantSource();
        var to = (from == _bgmA) ? _bgmB : _bgmA;

        if (from.isPlaying && from.clip == clip) return;
        Debug.Log($"[Audio] : {clip.name}");
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

    private IEnumerator CoBgmLoop(SceneState state)
    {
        while (true)
        {
            var currentSource = GetDominantSource();

            if (currentSource.clip != null)
            {
                float waitTime = currentSource.clip.length - _bgmFadeSeconds;
                yield return new WaitForSecondsRealtime(Mathf.Max(0, waitTime));
            }
            else
            {
                yield return new WaitForSeconds(1f); // 클립이 없으면 잠시 대기
            }

            if (_bgmTable.TryGetClip(state, out var nextClip) && nextClip != null)
            {
                CrossFadeTo(nextClip);
            }
        }
    }
    #endregion

    #region SFX
    // 공통 사운드 용도(SfxList.SO 관리)
    public void PlaySfx(SfxList sfx)
    {
        if (_sfxTable == null || _sfxSource == null) return;

        if (_sfxTable.TryGetClip(sfx, out var clip) && clip != null)
        {
            PlayClipWithCooldown(clip);
        }
    }

    // UI용 SFX
    public void PlayUISfx(UISfxList uiSfx)
    {
        if (_sfxTable == null || _sfxSource == null) return;

        // SFXTable에서 새로 만드신 TryGetUIClip을 사용합니다.
        if (_sfxTable.TryGetUIClip(uiSfx, out var clip) && clip != null)
        {
            PlayClipWithCooldown(clip);
        }
    }

    // 개별 사운드 용도(개별 Clip 관리)
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        PlayClipWithCooldown(clip);
    }

    private void PlayClipWithCooldown(AudioClip clip)
    {
        if (clipLastPlayTimes.TryGetValue(clip, out float lastPlayTime))
        {
            if (Time.time < lastPlayTime + soundCooldown)
            {
                return;
            }
        }

        _sfxSource.PlayOneShot(clip);
        clipLastPlayTimes[clip] = Time.time;
    }
    #endregion

    #region Volume

    public void LoadAndApplySavedVolumes()
    {
        SetVolume(AudioBus.Master, PlayerPrefs.GetFloat(AudioParam.MASTER_KEY, 1f));
        SetVolume(AudioBus.BGM,    PlayerPrefs.GetFloat(AudioParam.BGM_KEY, 1f));
        SetVolume(AudioBus.SFX,    PlayerPrefs.GetFloat(AudioParam.SFX_KEY, 1f));
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
