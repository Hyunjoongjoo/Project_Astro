using UnityEngine;
using UnityEngine.UI;

public sealed class AudioOptionsView : MonoBehaviour
{
    [Header("Sliders (0~1)")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("Mute Toggle")]
    [SerializeField] private Toggle _masterMuteToggle;
    [SerializeField] private Toggle _bgmMuteToggle;
    [SerializeField] private Toggle _sfxMuteToggle;

    private const float DEFAULT_MASTER = 1f;
    private const float DEFAULT_BGM    = 1f;
    private const float DEFAULT_SFX    = 1f;

    private bool _suppress;

    void OnEnable()
    {
        SyncSlidersFromSaved();
        Bind();
    }

    void OnDisable()
    {
        Unbind();
        PlayerPrefs.Save(); // 패널 닫을 때 1회만 저장
    }

    private void SyncSlidersFromSaved()
    {
        _suppress = true;

        float master    = PlayerPrefs.GetFloat(AudioParam.MASTER_KEY, DEFAULT_MASTER);
        float bgm       = PlayerPrefs.GetFloat(AudioParam.BGM_KEY, DEFAULT_BGM);
        float sfx       = PlayerPrefs.GetFloat(AudioParam.SFX_KEY, DEFAULT_SFX);

        bool m_mute = PlayerPrefs.GetInt(AudioParam.MASTER_MUTE_KEY, 0) == 1;
        bool b_mute = PlayerPrefs.GetInt(AudioParam.BGM_MUTE_KEY, 0) == 1;
        bool s_mute = PlayerPrefs.GetInt(AudioParam.SFX_MUTE_KEY, 0) == 1;

        if (_masterSlider != null)    _masterSlider.SetValueWithoutNotify(master);
        if (_bgmSlider != null)       _bgmSlider.SetValueWithoutNotify(bgm);
        if (_sfxSlider != null)       _sfxSlider.SetValueWithoutNotify(sfx);

        if (_masterMuteToggle != null) _masterMuteToggle.SetIsOnWithoutNotify(m_mute);
        if (_bgmMuteToggle != null) _bgmMuteToggle.SetIsOnWithoutNotify(b_mute);
        if (_sfxMuteToggle != null) _sfxMuteToggle.SetIsOnWithoutNotify(s_mute);

        if (AudioManager.Instance != null)
            AudioManager.Instance.LoadAndApplySavedVolumes();

        _suppress = false;
    }

    private void Bind()
    {
        if (_masterSlider != null)    _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_bgmSlider != null)       _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (_sfxSlider != null)       _sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        if (_masterMuteToggle != null) _masterMuteToggle.onValueChanged.AddListener(OnMasterMuteChanged);
        if (_bgmMuteToggle != null)    _bgmMuteToggle.onValueChanged.AddListener(OnBgmMuteChanged);
        if (_sfxMuteToggle != null)    _sfxMuteToggle.onValueChanged.AddListener(OnSfxMuteChanged);
    }

    private void Unbind()
    {
        if (_masterSlider != null)    _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (_bgmSlider != null)       _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        if (_sfxSlider != null)       _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

        if (_masterMuteToggle != null) _masterMuteToggle.onValueChanged.RemoveListener(OnMasterMuteChanged);
        if (_bgmMuteToggle != null)    _bgmMuteToggle.onValueChanged.RemoveListener(OnBgmMuteChanged);
        if (_sfxMuteToggle != null)    _sfxMuteToggle.onValueChanged.RemoveListener(OnSfxMuteChanged);
    }

    private void OnMasterChanged(float v)
    {
        if (_suppress) return;

        v = Mathf.Clamp01(v);
        AudioManager.Instance.SetVolume(AudioBus.Master, v);
        PlayerPrefs.SetFloat(AudioParam.MASTER_KEY, v);
    }

    private void OnBgmChanged(float v)
    {
        if (_suppress) return;

        v = Mathf.Clamp01(v);
        AudioManager.Instance.SetVolume(AudioBus.BGM, v);
        PlayerPrefs.SetFloat(AudioParam.BGM_KEY, v);
    }

    private void OnSfxChanged(float v)
    {
        if (_suppress) return;
        v = Mathf.Clamp01(v);
        AudioManager.Instance.SetVolume(AudioBus.SFX, v);
        PlayerPrefs.SetFloat(AudioParam.SFX_KEY, v);
    }

    private void OnMasterMuteChanged(bool isOn) 
    { 
        if (!_suppress) 
        { 
            PlayerPrefs.SetInt(AudioParam.MASTER_MUTE_KEY, isOn ? 1 : 0); 
            AudioManager.Instance.SetVolume(AudioBus.Master, _masterSlider.value);
        } 
    }

    private void OnBgmMuteChanged(bool isOn) 
    { 
        if (!_suppress) 
        { 
            PlayerPrefs.SetInt(AudioParam.BGM_MUTE_KEY, isOn ? 1 : 0); 
            AudioManager.Instance.SetVolume(AudioBus.BGM, _bgmSlider.value); 
        } 
    }

    private void OnSfxMuteChanged(bool isOn) 
    { 
        if (!_suppress) 
        { 
            PlayerPrefs.SetInt(AudioParam.SFX_MUTE_KEY, isOn ? 1 : 0);
            AudioManager.Instance.SetVolume(AudioBus.SFX, _sfxSlider.value); 
        } 
    }

    // 초기화 버튼용
    public void ResetToDefault()
    {
        PlayerPrefs.SetFloat(AudioParam.MASTER_KEY, DEFAULT_MASTER);
        PlayerPrefs.SetFloat(AudioParam.BGM_KEY, DEFAULT_BGM);
        PlayerPrefs.SetFloat(AudioParam.SFX_KEY, DEFAULT_SFX);
        
        PlayerPrefs.SetInt(AudioParam.MASTER_MUTE_KEY, 0);
        PlayerPrefs.SetInt(AudioParam.BGM_MUTE_KEY, 0);
        PlayerPrefs.SetInt(AudioParam.SFX_MUTE_KEY, 0);

        PlayerPrefs.Save();

        SyncSlidersFromSaved();
    }
}
