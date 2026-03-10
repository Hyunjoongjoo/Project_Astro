using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;


public enum HeroSortType { Name, Level, Role }


public class LobbyHeroListWindow : BaseUI
{
    [Header("연결 설정")]
    [SerializeField] private GameObject _heroCardPrefab;
    [SerializeField] private Transform _content;

    [Header("정렬버튼")]
    [SerializeField] private TMP_Dropdown _sortDropdown;
    [SerializeField] private Toggle _descendingToggle; //내림차순용 토글

    [Header("정렬 UI 아이콘")]
    [SerializeField] private Image _sortToggleImage; 
    [SerializeField] private Sprite _ascendingSprite;  // 오름차순용
    [SerializeField] private Sprite _descendingSprite; // 내림차순용


    private void Start()
    {
        _sortDropdown.onValueChanged.AddListener((val) => RefreshHeroList());
        _descendingToggle.onValueChanged.AddListener((isOn) =>
        {
            UpdateToggleVisual(isOn);
            RefreshHeroList();
        });

        UpdateToggleVisual(_descendingToggle.isOn);
    }

    public override void Open()
    {
        if (!gameObject.activeSelf)
        {
            RefreshHeroList();
        }
        base.Open();
    }

    private void RefreshHeroList()
    {

        foreach (Transform child in _content) Destroy(child.gameObject);

        var heroTable = TableManager.Instance.HeroTable.GetAll();
        var userHeroes = UserDataManager.Instance.HeroesModel;

        //링큐 조인 이용해서 영웅정보랑, 플레이어 레벨/경험치를 섞어 사용가능하게
        var sortedList = heroTable.Join(userHeroes,
            h => h.id,
            u => u.heroId,
            (h, u) => new { Data = h, User = u });

        switch ((HeroSortType)_sortDropdown.value)
        {
            case HeroSortType.Name:
                sortedList = _descendingToggle.isOn ? sortedList.OrderByDescending(x => x.Data.heroName) : sortedList.OrderBy(x => x.Data.heroName);
                break;
            case HeroSortType.Level:
                sortedList = _descendingToggle.isOn ? sortedList.OrderByDescending(x => x.User.level) : sortedList.OrderBy(x => x.User.level);
                break;
            case HeroSortType.Role:
                sortedList = _descendingToggle.isOn ? sortedList.OrderByDescending(x => x.Data.heroRole) : sortedList.OrderBy(x => x.Data.heroRole);
                break;
        }

        foreach (var item in sortedList)
        {
            GameObject cardObj = Instantiate(_heroCardPrefab, _content);
            if (cardObj.TryGetComponent(out LobbyHeroCardUI cardUI))
            {
                cardUI.Setup(item.Data);
            }
        }
    }

    private void UpdateToggleVisual(bool isOn)
    {
        if (_sortToggleImage != null)
        {
            _sortToggleImage.sprite = isOn ? _descendingSprite : _ascendingSprite;
        }
    }
}
