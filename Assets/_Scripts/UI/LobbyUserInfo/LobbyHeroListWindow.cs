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


    private void OnEnable()
    {
        if (UserDataManager.Instance != null)
        {
            // 골드가 변하거나 영웅 데이터가 변하면 리스트 리프레시
            UserDataManager.Instance.OnGoldChanged += HandleGoldChanged;
            UserDataManager.Instance.OnHeroDataChanged += RefreshHeroList;
        }
    }

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
    
    private void OnDisable()
    {
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnGoldChanged -= HandleGoldChanged;
            UserDataManager.Instance.OnHeroDataChanged -= RefreshHeroList;
        }
    }

    private void HandleGoldChanged(int newGold) => RefreshHeroList();

    public override void Open(bool playSound = true)
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
        var displayList = heroTable.Join(userHeroes,
            h => h.id,
            u => u.heroId,
            (h, u) => new { 
                Data = h, 
                User = u,
                // 정렬용 번역 데이터 추가
                TranslatedName = TableManager.Instance.GetString(h.heroName),
                TranslatedRole = TableManager.Instance.GetString($"hero_role_{h.heroRole.ToString().ToLower()}")
            });

        // 번역된 텍스트를 기준으로 정렬 수행
        var sortType = (HeroSortType)_sortDropdown.value;
        bool isDescending = _descendingToggle.isOn;

        // 정렬 로직
        var query = displayList.AsEnumerable();

        switch (sortType)
        {
            case HeroSortType.Name:
                query = isDescending ? query.OrderByDescending(x => x.TranslatedName) : query.OrderBy(x => x.TranslatedName);
                break;
            case HeroSortType.Level:
                query = isDescending ? query.OrderByDescending(x => x.User.level).ThenBy(x => x.TranslatedName) : query.OrderBy(x => x.User.level).ThenBy(x => x.TranslatedName);
                break;
            case HeroSortType.Role:
                query = isDescending ? query.OrderByDescending(x => x.TranslatedRole).ThenBy(x => x.TranslatedName) : query.OrderBy(x => x.TranslatedRole).ThenBy(x => x.TranslatedName);
                break;
            default:
                query = query.OrderBy(x => x.Data.id);
                break;
        }

        //생성
        foreach (var item in query)
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
