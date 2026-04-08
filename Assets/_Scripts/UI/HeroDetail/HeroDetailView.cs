using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroDetailView : BaseUI
{
    [Header("메인 정보")]
    [SerializeField] private Image _heroImg;
    [SerializeField] private Image _pilotImg;
    [SerializeField] private TMP_Text _heroNameTxt;
    [SerializeField] private TMP_Text _heroTypeTxt;
    [SerializeField] private TMP_Text _heroRoleTxt;
    [SerializeField] private TMP_Text _descriptionTxt;

    [Header("레벨 정보")]
    [SerializeField] private TMP_Text _heroLevelTxt;
    [SerializeField] private TMP_Text _heroExpTxt;
    [SerializeField] private Image _heroExpBar;

    [Header("스킬 페이지 설정")]
    [SerializeField] private Transform _skillContainer;
    [SerializeField] private GameObject _skillPanelPrefab;

    [Header("증강 페이지 설정")]
    [SerializeField] private Transform _augmentContainer;
    [SerializeField] private GameObject _augmentPanelPrefab;

    //[Header("레벨 보상 페이지 설정")]
    //[SerializeField] private Button[] _rewardBtns;
    //[SerializeField] private GameObject[] _lockImgs;
    //[SerializeField] private TMP_Text _rewardNameTxt;
    //[SerializeField] private TMP_Text _rewardDesTxt;
    //[SerializeField] private GameObject _lockPanel;
    //[SerializeField] private TMP_Text _locktxt;

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
    //public Button[] RewardButtons => _rewardBtns;

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
        _heroImg.sprite = icon;
    }

    public void SetPilotImage(Sprite icon)
    {
        _pilotImg.sprite = icon;
    }

    // 레벨 및 경험치 세팅
    public void SetLevelInfo(string level, int currentExp, float maxExp)
    {
        _heroLevelTxt.text = level;
        if (maxExp > 0)
        {
            _heroExpBar.fillAmount = (float)currentExp / maxExp;
            _heroExpTxt.text = $"{currentExp} / {maxExp}";
        }
        else
        {
            // 만렙일 경우 MAX 표시
            _heroExpBar.fillAmount = 1f;
            _heroExpTxt.text = "MAX";
        }
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
    public void ClearSkillPage()
    {
        foreach (Transform child in _skillContainer) Destroy(child.gameObject);
    }
    public void ClearAugmentPage()
    {
        foreach (Transform child in _augmentContainer) Destroy(child.gameObject);
    }


    //스텟 판넬 설정후 소환
    public StatPanelUI AddStatItem(string name, string value, Sprite icon, Color color = default)
    {
        GameObject obj = Instantiate(_statPanelPrefab, _statContainer);
        if (obj.TryGetComponent(out StatPanelUI statItem))
        {
            statItem.SetStat(name, value, icon, color);
            return statItem;
        }
        return null;
    }
    // 스킬 생성
    public void AddSkillItem(string name, string des,string cooltime, Sprite icon = null)
    {
        GameObject obj = Instantiate(_skillPanelPrefab, _skillContainer);
        if (obj.TryGetComponent(out SkillDesPanelUI skillItem))
        {
            skillItem.Setup(name, des,cooltime, icon);
        }
    }

    // 증강 생성
    public void AddAugmentItem(string name, string des, Sprite icon = null)
    {
        GameObject obj = Instantiate(_augmentPanelPrefab, _augmentContainer);
        if (obj.TryGetComponent(out AugmentDesPanelUI augmentItem))
        {
            augmentItem.Setup(name, des, icon);
        }
    }

    //public void UpdateLevelRewardUI(int currentLevel, string rewardName,string rewardDes, int selectedIndex)
    //{
    //    int[] unlockLevels = { 3, 6, 6, 9 };
    //
    //    for(int i = 0; i < _lockImgs.Length; i++)
    //    {
    //        //현재 레벨이 해금레벨 보다 크거나 같으면 자물쇠 비활성화
    //        _lockImgs[i].SetActive(currentLevel < unlockLevels[i]); 
    //    }
    //
    //    bool isCurrentSelectedLocked = currentLevel < unlockLevels[selectedIndex];
    //    _lockPanel.SetActive(isCurrentSelectedLocked);
    //
    //    _locktxt.text = $"레벨 {unlockLevels[selectedIndex]} 달성 시 해금";
    //
    //    _rewardNameTxt.text = rewardName;
    //    _rewardDesTxt.text = rewardDes;
    //}
    //
    //// 아이콘 설정 (버튼 자체의 이미지를 바꾸고 싶을 때)
    //public void SetLevelRewardIcons(Sprite[] icons)
    //{
    //    for (int i = 0; i < _rewardBtns.Length; i++)
    //    {
    //        if (i < icons.Length)
    //            _rewardBtns[i].image.sprite = icons[i];
    //    }
    //}

}
