using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroDetailPopup : BaseUI
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

    [Header("하단 스와이프 패널")]
    [SerializeField] private SwipeUI _detailSwipeUI;
    [SerializeField] private GameObject[] _swipePages;

    [Header("스텟 페이지 설정")]
    [SerializeField] private Transform _statContainer;
    [SerializeField] private GameObject _statPanelPrefab;

    [Header("업그레이드 버튼 설정")]
    [SerializeField] private Button _upgradeBtn;
    [SerializeField] private TMP_Text _upgradeBtnTxt;
    [SerializeField] private Color _buyColor = Color.gray;
    [SerializeField] private Color _upgradeColor = Color.green;

    private HeroData _currentHeroData;
    private HeroDbModel _userHeroData;

    public void Setup(HeroData heroData)
    {
        _currentHeroData = heroData;
        _userHeroData = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == heroData.id);

        //기본정보 매핑
        _heroNameTxt.text = heroData.heroName;
        _descriptionTxt.text = heroData.heroDesc;
        _heroTypeTxt.text = heroData.heroType.ToString();
        _heroRoleTxt.text = heroData.heroRole.ToString();

        //이미지는 어드레서블로  나중에 연결시킬예정 like 어드레서블 매니저

        RefreshUI(); //레벨, 경험치, 해금여부등 
        RefreshDetailPages(); // 스와이프 패널에 연결된 설정들
    }
    //레벨, 경험치, 업그레이드 버튼 상태 설정
    private void RefreshUI()
    {
        if (_userHeroData == null) return;

        //레벨, 경험치 표시
        _heroLevelTxt.text = $"Lv. {_userHeroData.level}";

        var levelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());
        if (levelData != null)
        {
            float maxExp = levelData.expRequirement;
            _heroExpBar.fillAmount = (float)_userHeroData.exp / maxExp;
            _heroExpTxt.text = $"{_userHeroData.exp} / {maxExp}";
        }

        //업그레이드 버튼 설정
        UpdateUpgradeButton();
    }

    private void UpdateUpgradeButton()
    {
        if (!_userHeroData.isUnlock) //미해금 상태일 때
        {
            _upgradeBtn.image.color = _buyColor;
            _upgradeBtnTxt.text = $"구매\n{_currentHeroData.goldRequirement}골드";
        }
        else //해금상태일 때
        {
            _upgradeBtn.image.color = _upgradeColor;
            // 테이블에서 현재 레벨에 필요한 업그레이드 골드 가져오기
            var levelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());
            if (levelData != null)
            {
                _upgradeBtnTxt.text = $"업그레이드\n{levelData.goldRequirement}골드";
            }
            else
            {
                _upgradeBtnTxt.text = "최고 레벨";
                _upgradeBtn.interactable = false; // 테이블 데이터가 없으면 만렙 처리
            }
        }
    }

    public async void OnClickAction()
    {
        int currentGold = UserDataManager.Instance.WalletModel.gold;

        if (!_userHeroData.isUnlock)
        {
            //구매 로직
            if(currentGold >= _currentHeroData.goldRequirement)
            {
                await UserDataManager.Instance.UpdateWallet(-_currentHeroData.goldRequirement);
                await UserDataManager.Instance.UpdateHero(_userHeroData.heroId, 1, 0, true);

                _userHeroData.isUnlock = true; //로컬 즉시 반영
                _userHeroData.level = 1; // 로컬 즉시 반영

                RefreshAfterUpdate();
            }
            else
            {
                Debug.Log("골드 부족");
            }
        }
        else
        {
            //업그레이드 로직
            var levelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());
            if (levelData == null)
            {
                Debug.LogError($"레벨 {_userHeroData.level}에 대한 테이블 데이터가 없습니다.");
                return;
            }

            int upgradeCost = levelData.goldRequirement;

            if (currentGold >= upgradeCost)
            {
                await UserDataManager.Instance.UpdateWallet(-upgradeCost);
                int nextLevel = _userHeroData.level + 1;
                await UserDataManager.Instance.UpdateHero(_userHeroData.heroId, nextLevel, _userHeroData.exp, true);

                _userHeroData.level = nextLevel; // 로컬 즉시 반영

                RefreshAfterUpdate();
            }
            else
            {
                Debug.Log("골드 부족");
            }
        }
    }
    // 업데이트 후 모든 UI를 갱신하는 헬퍼 함수
    private void RefreshAfterUpdate()
    {
        RefreshUI();           // 레벨, 경험치바, 버튼 상태 갱신
        RefreshDetailPages();  // [중요] 강화된 수치가 반영된 스텟 페이지 갱신

        // 로비 골드 정보 갱신 (싱글톤 UI 접근)
        var lobbyUI = Object.FindFirstObjectByType<LobbyUserInfoUI>();
        if (lobbyUI != null) lobbyUI.RefreshUI();
    }


    //스와이프 패널 안쪽 설정
    private void RefreshDetailPages()
    {
        // 정보(스텟) 페이지 셋업
        SetupStatPage(_currentHeroData.id);

        // 프리뷰 영상 페이지 셋업
        SetupVideoPage(_currentHeroData.heroPreviewVideo);

        // 스킬 정보 셋업
        SetupSkillPage(_currentHeroData.skill);
    }

    private void SetupStatPage(string statId)
    {
        // 기존에 생성된 스텟 항목들 제거
        foreach (Transform child in _statContainer)
            Destroy(child.gameObject);

        Debug.Log($"요청하는 StatId: {statId}");
        HeroStatData status = HeroManager.Instance.GetStatus(statId);

        if (status == null)
        {
            Debug.LogError($"[HeroDetailPopup] {statId}에 해당하는 데이터를 HeroManager에서 찾을 수 없습니다! 딕셔너리를 확인하세요.");
            return;
        }

        AddStatItem("체력", status.BaseHp.ToString());
        AddStatItem("공격력", status.baseAttackPower.ToString());
        AddStatItem("치유력", status.baseHealingPower.ToString());
        AddStatItem("공격 속도", status.attackSpeed.ToString("F2"));
        AddStatItem("이동 속도", status.moveSpeed.ToString("F1"));
        AddStatItem("공격 범위", status.detectionRange.ToString("F1"));
        AddStatItem("재소환 대기시간", $"{status.spawnCooldown}초");
    }
    private void AddStatItem(string name, string value)
    {
        GameObject obj = Instantiate(_statPanelPrefab, _statContainer);
        var statItem = obj.GetComponent<StatPanelUI>();
        if (statItem != null)
        {
            statItem.SetStat(name, value);
        }
    }

    private void SetupVideoPage(string videoKey)
    {
        // VideoPlayer 컴포넌트에 videoKey(주소)를 할당하여 재생 준비
    }

    private void SetupSkillPage(string skillKey)
    {
        // 스킬 아이콘 및 설명 텍스트 세팅
    }
}
