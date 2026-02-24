using UnityEngine;
using UnityEngine.UI;

public sealed class AudioOptionsView : MonoBehaviour
{
    [Header("Sliders (0~1)")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

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
        float sfx        = PlayerPrefs.GetFloat(AudioParam.SFX_KEY, DEFAULT_SFX);

        if (_masterSlider != null)    _masterSlider.SetValueWithoutNotify(master);
        if (_bgmSlider != null)       _bgmSlider.SetValueWithoutNotify(bgm);
        if (_sfxSlider != null)       _sfxSlider.SetValueWithoutNotify(sfx);

        if (AudioManager.Instance != null)
            AudioManager.Instance.LoadAndApplySavedVolumes();

        _suppress = false;
    }

    private void Bind()
    {
        if (_masterSlider != null)    _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_bgmSlider != null)       _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (_sfxSlider != null)       _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    private void Unbind()
    {
        if (_masterSlider != null)    _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (_bgmSlider != null)       _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        if (_sfxSlider != null)       _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
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

    // 초기화 버튼용
    public void ResetToDefault()
    {
        PlayerPrefs.SetFloat(AudioParam.MASTER_KEY, DEFAULT_MASTER);
        PlayerPrefs.SetFloat(AudioParam.BGM_KEY, DEFAULT_BGM);
        PlayerPrefs.SetFloat(AudioParam.SFX_KEY, DEFAULT_SFX);

        PlayerPrefs.Save();

        SyncSlidersFromSaved();
    }
}
