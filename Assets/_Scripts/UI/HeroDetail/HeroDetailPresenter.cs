using UnityEngine;

public class HeroDetailPresenter : MonoBehaviour
{
    [SerializeField] private HeroDetailView _view;

    private HeroData _heroData;
    private HeroDbModel _userHeroData;

    // 외부(LobbyHeroCardUI)에서 호출하는 진입점
    public void Setup(HeroData data)
    {
        _heroData = data;

        // 버튼 클릭 이벤트 연결 (기존 OnClickAction 대체)
        _view.UpgradeBtn.onClick.RemoveAllListeners();
        _view.UpgradeBtn.onClick.AddListener(HandleUpgradeAction);

        RefreshAll();
    }

    private void OnEnable()
    {
        // 옵저버 패턴: 골드가 바뀌면 팝업 UI도 다시 계산해서 갱신
        if (UserDataManager.Instance != null)
            UserDataManager.Instance.OnGoldChanged += OnGoldChanged;
    }

    private void OnDisable()
    {
        if (UserDataManager.Instance != null)
            UserDataManager.Instance.OnGoldChanged -= OnGoldChanged;
    }

    private void OnGoldChanged(int newGold) => RefreshAll();

    private void RefreshAll()
    {
        _userHeroData = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);
        if (_userHeroData == null) return;

        // 1. 기본 정보 전달
        _view.SetHeroBaseInfo(_heroData.heroName, _heroData.heroDesc,
                             _heroData.heroType.ToString(), _heroData.heroRole.ToString());

        // 2. 레벨/경험치 로직 처리
        var levelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());
        if (levelData != null)
        {
            _view.SetLevelInfo(_userHeroData.level, _userHeroData.exp, levelData.expRequirement);

            // 버튼 텍스트 및 상태 결정 로직
            string priceText = _userHeroData.isUnlock ?
                $"업그레이드\n{levelData.goldRequirement}골드" :
                $"구매\n{_heroData.goldRequirement}골드";

            _view.SetUpgradeButton(_userHeroData.isUnlock, priceText, false);
        }
        else
        {
            _view.SetUpgradeButton(true, "최고 레벨", true);
        }

        // 3. 스텟 페이지 갱신
        UpdateStatPage();
    }

    private async void HandleUpgradeAction()
    {
        int currentGold = UserDataManager.Instance.WalletModel.gold;

        if (!_userHeroData.isUnlock)
        {
            if (currentGold >= _heroData.goldRequirement)
            {
                await UserDataManager.Instance.UpdateWallet(-_heroData.goldRequirement);
                await UserDataManager.Instance.UpdateHero(_heroData.id, 1, 0, true);
            }
        }
        else
        {
            var levelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());
            if (levelData != null && currentGold >= levelData.goldRequirement)
            {
                await UserDataManager.Instance.UpdateWallet(-levelData.goldRequirement);
                await UserDataManager.Instance.UpdateHero(_heroData.id, _userHeroData.level + 1, _userHeroData.exp, true);
            }
        }
    }

    private void UpdateStatPage()
    {
        _view.ClearStats();
        HeroStatData status = HeroManager.Instance.GetStatus(_heroData.id);
        if (status == null) return;

        _view.AddStatItem("체력", status.BaseHp.ToString());
        _view.AddStatItem("공격력", status.baseAttackPower.ToString());
        _view.AddStatItem("치유력", status.baseHealingPower.ToString());
        _view.AddStatItem("공격 속도", status.attackSpeed.ToString("F2"));
        _view.AddStatItem("이동 속도", status.moveSpeed.ToString("F1"));
    }
}
