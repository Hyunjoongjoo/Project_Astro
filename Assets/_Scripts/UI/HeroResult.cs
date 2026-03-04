using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroResult : MonoBehaviour
{
    [SerializeField] private Image _heroIcon;
    [SerializeField] private TextMeshProUGUI _heroNameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Slider _expSlider;
    [SerializeField] private TextMeshProUGUI _expPercentText;

    [SerializeField] private Image _expSliderFillImage;
    [SerializeField] private HeroIconDataSO _heroIconDataSo;

    public void Setup(HeroResultData data)
    {
        _heroIcon.sprite   = _heroIconDataSo.GetIcon(data.HeroId);
        _heroNameText.text = TableManager.Instance.GetString(data.HeroId);
        _levelText.text    = $"LV {data.Level}";

        // 만약 직접 할당 안 했다면 코드에서 찾기
        if (_expSliderFillImage == null)
            _expSliderFillImage = _expSlider.fillRect.GetComponent<Image>();

        float startValue = (float)data.CurrentExp / data.MaxExp;
        float endValue = (float)(data.CurrentExp + data.AddedExp) / data.MaxExp;

        // 1.0 이상이면 1.0으로 고정
        float finalTargetValue = Mathf.Min(endValue, 1f);

        _expSlider.value = startValue;

        // 애니메이션 시작
        _expSlider.DOValue(finalTargetValue, 1.5f)
            .SetEase(Ease.OutCubic)
            .OnUpdate(() =>
            {
                int currentPercent = Mathf.RoundToInt(_expSlider.value * 100);
                _expPercentText.text = $"{currentPercent}%";

                if (_expSlider.value >= 1f)
                {
                    _expSliderFillImage.color = Color.green;
                }
            })
            .OnComplete(() =>
            {
                if (endValue >= 1f)
                {
                    _expSliderFillImage.color = Color.green;
                    _expSlider.transform.DOPunchScale(Vector3.one * 0.05f, 0.3f);
                }
            });
    }
}
