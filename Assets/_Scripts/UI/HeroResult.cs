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

    public void Setup(HeroResultData data)
    {
        // 1. 기본 정보 세팅
        _heroNameText.text = data.Name;
        _levelText.text = $"LV {data.Level}";

        // 아이콘 로직 (Addressables나 Resources에 맞춰 경로 사용)
        // _heroIcon.sprite = Managers.Resource.Load<Sprite>(data.IconPath);

        // 원래 경험치
        float startValue = (float)data.CurrentExp / data.MaxExp;

        // 습득한 경험치 적용
        float endValue = (float)(data.CurrentExp + data.AddedExp) / data.MaxExp;

        _expSlider.value = startValue;

        // 1.5초 동안 부드럽게 차오르는 애니메이션
        _expSlider.DOValue(endValue, 1.5f).SetEase(Ease.OutCubic).OnUpdate(() =>
        {
            // 차오르는 동안 퍼센트 텍스트 갱신
            int currentPercent = Mathf.RoundToInt(_expSlider.value * 100);
            _expPercentText.text = $"{currentPercent}%";
        });
    }
}
