using UnityEngine;

public class HeroDetailPresenter : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private HeroDetailView _view;
    [SerializeField] private GameObject _confirmPopupPrefab;
    [SerializeField] private GameObject _upgradeResultPrefab;

    [Header("스텟 아이콘 SO")]
    [SerializeField] private StatIconDataSO _statIcons;

    private HeroData _heroData;
    private HeroDbModel _userHeroData;

    // 외부(LobbyHeroCardUI)에서 호출하는 진입점
    public void Setup(HeroData data)
    {
        _heroData = data;

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

        //리프레시할 때마다 버튼 이벤트를 새로 연결합니다.
        _view.UpgradeBtn.onClick.RemoveAllListeners();
        _view.UpgradeBtn.onClick.AddListener(HandleUpgradeAction);

        // DB에 데이터가 아예 없는 경우(비정상) 예외 처리
        if (_userHeroData == null)
        {
            Debug.LogError($"HeroId {_heroData.id} 를 UserDataManager에서 찾을 수 없습니다.");
            return;
        }

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
        // 프리팹 자체가 null인지 먼저 체크 (인스펙터 할당 문제 확인용)
        if (_confirmPopupPrefab == null)
        {
            Debug.LogError("Presenter에 _confirmPopupPrefab이 할당되지 않았습니다!");
            return;
        }

        // 클릭 시점에 최신 데이터를 다시 확인 (NullReference 방지)
        _userHeroData = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);
        if (_userHeroData == null) return;

        if (!_userHeroData.isUnlock)
        {
            var popup = UIManager.Instance.ShowUI<ConfirmPopup>(_confirmPopupPrefab);
            popup.Setup($"{_heroData.heroName}을(를) 구매하시겠습니까?", async () =>
            {
                // 람다 실행 시점에 다시 한번 체크
                if (UserDataManager.Instance.WalletModel.gold >= _heroData.goldRequirement)
                {
                    await UserDataManager.Instance.UpdateWallet(-_heroData.goldRequirement);
                    await UserDataManager.Instance.UpdateHero(_heroData.id, 1, 0, true);

                    RefreshAll();
                }
            });
        }
        else
        {
            // 현재 레벨 데이터 가져오기 (Setup용)
            var currentLevelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());
            if (currentLevelData == null) return;

            var popup = UIManager.Instance.ShowUI<ConfirmPopup>(_confirmPopupPrefab);

            if (popup == null)
            {
                Debug.LogError("_confirmPopupPrefab이 제대로 생성되지 않았거나 ConfirmPopup 컴포넌트가 없습니다!");
                return;
            }
            popup.Setup("레벨업 하시겠습니까?", async () =>
            {
                //데이터 매니저나 히어로 리스트가 유효한지 먼저 확인
                if (UserDataManager.Instance == null || UserDataManager.Instance.HeroesModel == null) return;

                Debug.Log($"Searching for ID: {_heroData.id}");
                //실행 시점에 최신 데이터를 찾음
                var userHero = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);
                Debug.Log($"Found Hero: {userHero != null}");
                //찾은 데이터가 null인지 반드시 확인 후 다음으로 진행
                if (userHero == null)
                {
                    Debug.LogError($"[Upgrade] 영웅 데이터를 찾을 수 없습니다: {_heroData.id}");
                    return;
                }

                //테이블 데이터 확인
                var costData = TableManager.Instance.HeroLevelTable.Get(userHero.level.ToString());
                if (costData == null)
                {
                    Debug.LogWarning($"[Upgrade] 레벨 테이블에 데이터가 없습니다. 만렙일 가능성: {userHero.level}");
                    return;
                }

                //골드 체크 및 실행
                if (UserDataManager.Instance.WalletModel.gold >= costData.goldRequirement)
                {
                    HeroStatData oldStatus = HeroManager.Instance.GetStatus(_heroData.id);

                    int nextLevel = userHero.level + 1;
                    await UserDataManager.Instance.UpdateWallet(-costData.goldRequirement);
                    await UserDataManager.Instance.UpdateHero(_heroData.id, nextLevel, userHero.exp, true);

                    RefreshAll();
                    ShowUpgradeResult(oldStatus);
                }
            });
        }
    }

    private void UpdateStatPage()
    {
        _view.ClearStats();
        HeroStatData status = HeroManager.Instance.GetStatus(_heroData.id);
        if (status == null) return;

        _view.AddStatItem("체력", status.BaseHp.ToString(), _statIcons.GetIcon(StatType.Hp));
        _view.AddStatItem("공격력", status.baseAttackPower.ToString(), _statIcons.GetIcon(StatType.AttackPower));
        _view.AddStatItem("치유력", status.baseHealingPower.ToString(), _statIcons.GetIcon(StatType.HealingPower));
        _view.AddStatItem("공격 속도", status.attackSpeed.ToString("F2"), _statIcons.GetIcon(StatType.AttackSpeed));
        _view.AddStatItem("이동 속도", status.moveSpeed.ToString("F1"), _statIcons.GetIcon(StatType.MoveSpeed));
        _view.AddStatItem("공격 범위", status.detectionRange.ToString("F1"), _statIcons.GetIcon(StatType.DetectionRange));
    }

    private void ShowUpgradeResult(HeroStatData oldStat)
    {
        // 1. 갱신된(새로운) 런타임 스텟 가져오기
        HeroStatData newStat = HeroManager.Instance.GetStatus(_heroData.id);

        // 2. 테이블 원본 데이터 가져오기 (ipLv 수치를 확인하기 위함)
        // HeroData의 statId가 Stat_Hero_Knight 형태라면 그걸 사용
        HeroStatData tableBase = TableManager.Instance.HeroStatTable.Get(_heroData.heroStatId);

        // 3. 결과 팝업 띄우기
        var resultUI = UIManager.Instance.ShowUI<UpgradeResultPopup>(_upgradeResultPrefab);
        if (resultUI != null)
        {
            resultUI.Setup(oldStat, newStat, tableBase);
        }
    }
}
