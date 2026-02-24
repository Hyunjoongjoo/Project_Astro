using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyHeroCardUI : MonoBehaviour
{
    [SerializeField] private Image _iconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _levelTxt;
    [SerializeField] private Image _heroExpBar;
    [SerializeField] private TMP_Text _heroExpTxt;
    [SerializeField] private GameObject _detailPopupPrefab;

    private HeroData _heroData;
    private HeroDbModel _userHeroData;

    public void Setup(HeroData data)
    {
        _heroData = data;

        _userHeroData = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);
        var levelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());


        _nameTxt.text = data.heroName;
        _levelTxt.text = _userHeroData.level.ToString();

        _heroExpBar.fillAmount = (float)_userHeroData.exp / levelData.expRequirement;
        _heroExpTxt.text = $"{_userHeroData.exp} / {levelData.expRequirement}";

        //아이콘 이미지 어드레서블로연결 나중에
    }

    public void OnClickCard()
    {
        // UIManager를 통해 팝업 생성 및 데이터 전달
        var detailObj = UIManager.Instance.ShowUI<HeroDetailView>(_detailPopupPrefab, true);
        if (detailObj != null && detailObj.TryGetComponent(out HeroDetailPresenter presenter))
        {
            presenter.Setup(_heroData);
        }
    }
}
