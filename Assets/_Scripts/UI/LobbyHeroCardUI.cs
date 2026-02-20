using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyHeroCardUI : MonoBehaviour
{
    [SerializeField] private Image _iconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private GameObject _detailPopupPrefab;

    private HeroData _heroData;

    public void Setup(HeroData data)
    {
        _heroData = data;

        _nameTxt.text = data.heroName;

        //아이콘 이미지 어드레서블로연결 나중에
    }

    public void OnClickCard()
    {
        // UIManager를 통해 팝업 생성 및 데이터 전달
        var detailPopup = UIManager.Instance.ShowUI<HeroDetailPopup>(_detailPopupPrefab,true);
        if (detailPopup != null)
        {
            detailPopup.Setup(_heroData);
        }
    }
}
