using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DescriptionType
{
    Skill,
    Augment,
    LevelReward
}
public class HeroDetailView : BaseUI
{
    [Header("메인 정보")]
    [SerializeField] private Image _heroPilotImg;
    [SerializeField] private TMP_Text _heroNameTxt;
    [SerializeField] private TMP_Text _heroTypeTxt;
    [SerializeField] private TMP_Text _heroRoleTxt;
    [SerializeField] private TMP_Text _descriptionTxt;

    [Header("레벨 정보")]
    [SerializeField] private TMP_Text _heroLevelTxt;
    [SerializeField] private TMP_Text _heroExpTxt;
    [SerializeField] private Image _heroExpBar;

    [Header("상세 설명 설정")]
    [SerializeField] private Transform _skillDesContainer;
    [SerializeField] private Transform _augmentDesContainer;
    [SerializeField] private Transform _levelRewardDesContainer;
    [SerializeField] private Transform _skillIconContainer;
    [SerializeField] private Transform _augmentIconContainer;
    [SerializeField] private Transform _levelRewardIconContainer;
    [SerializeField] private GameObject _skillIconPrefab;
    [SerializeField] private GameObject _desPanelPrefab;

    [Header("스텟 페이지 설정")]
    [SerializeField] private Transform _statContainer;
    [SerializeField] private GameObject _statPanelPrefab;

    [Header("업그레이드 버튼 설정")]
    [SerializeField] private Button _upgradeBtn;
    [SerializeField] private TMP_Text _upgradeBtnTxt;
    [SerializeField] private Color _buyColor = Color.gray;
    [SerializeField] private Color _upgradeColor = Color.green;

    // Presenter에서 버튼 클릭 이벤트를 연결하기 위한 프로퍼티
    public Button UpgradeBtn => _upgradeBtn;

    // 타입에 맞는 컨테이너를 반환하는 헬퍼 프로퍼티/메서드
    private Transform GetDesContainer(DescriptionType type) => type switch
    {
        DescriptionType.Skill => _skillDesContainer,
        DescriptionType.Augment => _augmentDesContainer,
        DescriptionType.LevelReward => _levelRewardDesContainer,
        _ => null
    };
    private Transform GetIconContainer(DescriptionType type) => type switch
    {
        DescriptionType.Skill => _skillIconContainer,
        DescriptionType.Augment => _augmentIconContainer,
        DescriptionType.LevelReward => _levelRewardIconContainer,
        _ => null
    };

    // 기본 정보 세팅
    public void SetHeroBaseInfo(string name, string desc, string type, string role)
    {
        _heroNameTxt.text = name;
        _descriptionTxt.text = desc;
        _heroTypeTxt.text = type;
        _heroRoleTxt.text = role;
    }
    public void SetHeroImage(Sprite icon)
    {
        _heroPilotImg.sprite = icon;
    }

    // 레벨 및 경험치 세팅
    public void SetLevelInfo(int level, int currentExp, float maxExp)
    {
        _heroLevelTxt.text = $"Lv. {level}";
        _heroExpBar.fillAmount = currentExp / maxExp;
        _heroExpTxt.text = $"{currentExp} / {maxExp}";
    }

    // 버튼 상태 세팅
    public void SetUpgradeButton(bool isUnlock, string priceText, bool isMaxLevel)
    {
        _upgradeBtn.interactable = !isMaxLevel;
        _upgradeBtnTxt.text = priceText;

        if (isMaxLevel)
        {
            _upgradeBtn.image.color = Color.black;
        }
        else
        {
            _upgradeBtn.image.color = isUnlock ? _upgradeColor : _buyColor;
        }
    }

    // 스텟 항목 생성 및 초기화
    public void ClearStats()
    {
        foreach (Transform child in _statContainer) Destroy(child.gameObject);
    }

    //상세설명 판넬 초기화
    public void ClearDescription(DescriptionType type)
    {
        Transform container = GetDesContainer(type);
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
    }
    public void ClearIcons(DescriptionType type)
    {
        Transform container = GetIconContainer(type);
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
    }
    //스텟 판넬 설정후 소환
    public void AddStatItem(string name, string value, Sprite icon)
    {
        GameObject obj = Instantiate(_statPanelPrefab, _statContainer);
        if (obj.TryGetComponent(out StatPanelUI statItem))
        {
            statItem.SetStat(name, value, icon);
        }
    }

    public void AddSkillIcon(DescriptionType type, Sprite skillIcon)
    {
        Transform container = GetIconContainer(type);
        if (container == null) return;

        GameObject obj = Instantiate(_skillIconPrefab, container);

        if (obj.TryGetComponent(out Image iconImage))
        {
            iconImage.sprite = skillIcon;
        }
    }

    public void AddDescriptionItem(DescriptionType type, string name, string des)
    {
        Transform container = GetDesContainer(type);
        if (container == null) return;

        GameObject obj = Instantiate(_desPanelPrefab, container);
        if (obj.TryGetComponent(out DescriptionPanelUI desItem))
        {
            desItem.SetDes(name, des);
        }
    }
}
