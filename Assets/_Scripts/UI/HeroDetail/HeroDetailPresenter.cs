using System.Collections.Generic;
using UnityEngine;

public class HeroDetailPresenter : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private HeroDetailView _view;
    [SerializeField] private GameObject _confirmPopupPrefab;
    [SerializeField] private GameObject _upgradeResultPrefab;

    [Header("리소스 SO")]
    [SerializeField] private StatIconDataSO _statIcons;
    [SerializeField] private HeroIconDataSO _heroIcons;

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

        //리프레시할 때마다 버튼 이벤트를 새로 연결
        _view.UpgradeBtn.onClick.RemoveAllListeners();
        _view.UpgradeBtn.onClick.AddListener(HandleUpgradeAction);

        // DB에 데이터가 아예 없는 경우(비정상) 예외 처리
        if (_userHeroData == null)
        {
            Debug.LogError($"HeroId {_heroData.id} 를 UserDataManager에서 찾을 수 없습니다.");
            return;
        }

        //스트링 테이블 참조
        string translatedName = TableManager.Instance.GetString(_heroData.heroName);
        string translatedDesc = TableManager.Instance.GetString(_heroData.heroDesc);
        string typeKey = $"hero_type_{_heroData.heroType.ToString().ToLower()}";
        string roleKey = $"hero_role_{_heroData.heroRole.ToString().ToLower()}";

        //기본 정보 전달
        _view.SetHeroBaseInfo(
            translatedName,
            translatedDesc,
            TableManager.Instance.GetString(typeKey),
            TableManager.Instance.GetString(roleKey)
        );

        //아이콘 이미지 설정
        if (_heroIcons != null)
        {
            _view.SetHeroImage(_heroIcons.GetIcon(_heroData.id));
        }

        //레벨/경험치 로직 처리
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

        //스와이프 페이지 갱신
        UpdateSkillDesPage();
        UpdateAugmentDesPage();
        UpdateLevelRewardDesPage();
        UpdateStatPage();
    }

    private async void HandleUpgradeAction()
    {
        // 프리팹 자체가 null인지 먼저 체크
        if (_confirmPopupPrefab == null)
        {
            Debug.LogError("Presenter에 _confirmPopupPrefab이 할당되지 않았습니다!");
            return;
        }

        // 클릭 시점에 최신 데이터를 다시 확인
        _userHeroData = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);
        if (_userHeroData == null) return;

        var levelData = TableManager.Instance.HeroLevelTable.Get(_userHeroData.level.ToString());
        if (levelData == null) return;

        if (!_userHeroData.isUnlock)
        {
            var popup = UIManager.Instance.ShowUI<ConfirmPopup>(_confirmPopupPrefab);
            string translatedName = TableManager.Instance.GetString(_heroData.heroName);
            // 골드 체크
            bool isBuy = UserDataManager.Instance.WalletModel.gold >= _heroData.goldRequirement;

            popup.Setup(
                $"{translatedName}을(를) 구매하시겠습니까?",
                async () => {
                    await UserDataManager.Instance.UpdateWallet(-_heroData.goldRequirement);
                    await UserDataManager.Instance.UpdateHero(_heroData.id, _userHeroData.level, _userHeroData.exp, true);
                    RefreshAll();
                },
                isBuy,
                "골드가 부족하여 구매할 수 없습니다."
            );
        }
        else
        {
            //경험치랑 골드 조건 확인
            bool isExpFull = _userHeroData.exp >= levelData.expRequirement;
            bool isGoldEnough = UserDataManager.Instance.WalletModel.gold >= levelData.goldRequirement;
            string alertMsg = !isExpFull ? "경험치가 부족하여 레벨업할 수 없습니다." : "골드가 부족하여 레벨업할 수 없습니다.";

            var popup = UIManager.Instance.ShowUI<ConfirmPopup>(_confirmPopupPrefab);

            popup.Setup(
                "레벨업 하시겠습니까?", 
                async () =>
                {
                    // 다시 한번 최종 데이터 확인
                    var userHero = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);
                    var costData = TableManager.Instance.HeroLevelTable.Get(userHero.level.ToString());

                    // 최종 조건 체크 (이미 경험치가 찼는지 && 골드가 충분한지)
                    if (userHero.exp >= costData.expRequirement && UserDataManager.Instance.WalletModel.gold >= costData.goldRequirement)
                    {
                        HeroStatData oldStatus = HeroManager.Instance.GetStatus(_heroData.id);

                        int nextLevel = userHero.level + 1;
                        // 레벨업 시 골드,경험치 삭감
                        await UserDataManager.Instance.UpdateWallet(-costData.goldRequirement);
                        await UserDataManager.Instance.UpdateHero(_heroData.id, nextLevel, userHero.exp - costData.expRequirement, true);

                        RefreshAll();
                        ShowUpgradeResult(oldStatus);
                    }
                },
                isExpFull && isGoldEnough, // 버튼 활성화 조건: 경험치와 골드 모두 충족 시
                alertMsg
            );
        }
    }

    private void UpdateSkillDesPage()
    {
        _view.ClearDescription(DescriptionType.Skill);
        _view.ClearIcons(DescriptionType.Skill);

        var allSkills = TableManager.Instance.SkillInfoTable.GetAll();
        var mySkills = allSkills.Find(s => s.heroId == _heroData.id && s.skillName.Contains("skill"));

        if (_heroIcons != null)
        {
            Sprite skillIcons = _heroIcons.GetSkillIconByIndex(_heroData.id,0);

            if (skillIcons != null)
            {
                _view.AddSkillIcon(DescriptionType.Skill, skillIcons);              
            }
        }
        if(mySkills != null)
        {
            string name = TableManager.Instance.GetString(mySkills.skillName);
            string desc = TableManager.Instance.GetString(mySkills.skillDes);

            _view.AddDescriptionItem(DescriptionType.Skill, name, desc);
        }
                
    } 
    private void UpdateAugmentDesPage()
    {
        _view.ClearDescription(DescriptionType.Augment);
        _view.ClearIcons(DescriptionType.Augment);

        var allSkills = TableManager.Instance.SkillInfoTable.GetAll();
        var augmentSkill = allSkills.Find(s => s.heroId == _heroData.id && s.skillName.Contains("augment"));

        if (_heroIcons != null)
        {
            Sprite skillIcons = _heroIcons.GetSkillIconByIndex(_heroData.id, 1);

            if (skillIcons != null)
            {
                _view.AddSkillIcon(DescriptionType.Augment, skillIcons);
            }
        }
        if(augmentSkill != null)
        {
            string name = TableManager.Instance.GetString(augmentSkill.skillName);
            string desc = TableManager.Instance.GetString(augmentSkill.skillDes);

            _view.AddDescriptionItem(DescriptionType.Augment, name, desc);
        }
    }
    private void UpdateLevelRewardDesPage()
    {

    }

    //스텟 페이지 갱신
    private void UpdateStatPage()
    {
        _view.ClearStats();
        HeroStatData status = HeroManager.Instance.GetStatus(_heroData.id);
        HeroStatData tableBase = TableManager.Instance.HeroStatTable.Get(_heroData.heroStatId);
        if (status == null) return;

        //성장수치 보여줄 애들은 성장수치도 보여주기
        _view.AddStatItem("체력", $"{status.BaseHp} <color=#FF0500>(+{tableBase.ipLvHp})</color>", _statIcons.GetIcon(StatType.Hp),Color.green);
        _view.AddStatItem("공격력", $"{status.baseAttackPower} <color=#FF0500>(+{tableBase.ipLvAttackPower})</color>", _statIcons.GetIcon(StatType.AttackPower), Color.green);

        if (tableBase.baseHealingPower > 0 || tableBase.ipLvHealingPower > 0)
            _view.AddStatItem("치유력", $"{status.baseHealingPower} <color=#FF0500>(+{tableBase.ipLvHealingPower})</color>", _statIcons.GetIcon(StatType.HealingPower), Color.green);

        _view.AddStatItem("공격 속도", status.attackSpeed.ToString("F2"), _statIcons.GetIcon(StatType.AttackSpeed));
        _view.AddStatItem("이동 속도", status.moveSpeed.ToString("F1"), _statIcons.GetIcon(StatType.MoveSpeed));
        _view.AddStatItem("감지 범위", status.detectionRange.ToString("F1"), _statIcons.GetIcon(StatType.DetectionRange));
        _view.AddStatItem("재소환 시간", status.spawnCooldown.ToString("F1"), _statIcons.GetIcon(StatType.RespawnTime));
        _view.AddStatItem("쿨타임 증감", status.cooltimeReduce.ToString("F1"), _statIcons.GetIcon(StatType.SkillCooldown));
        _view.AddStatItem("피해 감소량", status.damageReduce.ToString("F1"), _statIcons.GetIcon(StatType.DamageReduction));

        string typeKey = $"hero_movetype_{status.moveType.ToString().ToLower()}";
        _view.AddStatItem("이동 유형", TableManager.Instance.GetString(typeKey), _statIcons.GetIcon(StatType.MoveType));
    }

    private void ShowUpgradeResult(HeroStatData oldStat)
    {
        //갱신된 런타임 스텟 가져오기
        HeroStatData newStat = HeroManager.Instance.GetStatus(_heroData.id);
        //테이블 원본 데이터 가져오기 (ipLv 수치를 확인하기 위함)
        HeroStatData tableBase = TableManager.Instance.HeroStatTable.Get(_heroData.heroStatId);
        // 현재 유저의 갱신된 레벨 정보를 가져옴
        var userHero = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);

        //결과 팝업 띄우기
        var resultUI = UIManager.Instance.ShowUI<UpgradeResultPopup>(_upgradeResultPrefab);
        if (resultUI != null)
        {
            resultUI.Setup(_heroData, userHero, oldStat, newStat, tableBase, _heroIcons);
        }
    }
}
