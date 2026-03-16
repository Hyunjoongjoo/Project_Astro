using System.Collections.Generic;
using UnityEngine;

public class HeroDetailPresenter : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private HeroDetailView _view;
    [SerializeField] private GameObject _confirmPopupPrefab;
    [SerializeField] private GameObject _upgradeResultPrefab;
    [SerializeField] private GameObject _toastPrefab;

    [Header("리소스 SO")]
    [SerializeField] private StatIconDataSO _statIcons;
    [SerializeField] private HeroIconDataSO _heroIcons;

    private HeroData _heroData;
    private HeroDbModel _userHeroData;


    private void Start()
    {
        DBStatus.IsUpdating = false;
    }

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
        UpdateSkillAndAugmentPages();
        //UpdateLevelRewardDesPage();
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

        //DB 중복 처리 방어
        if (DBStatus.IsUpdating) return;

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
                    if (DBStatus.IsUpdating) return;
                    DBStatus.IsUpdating = true;

                    try
                    {

                        var updates = new Dictionary<string, object> { { "Wallet.gold", UserDataManager.Instance.WalletModel.gold - _heroData.goldRequirement } };
                        var heroes = new List<HeroDbModel> { new HeroDbModel { heroId = _heroData.id, level = 1, exp = _userHeroData.exp, isUnlock = true } };

                        await UserDataManager.Instance.UpdateAll(updates, heroes);
                        RefreshAll();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"구매 실패 : {ex.Message}");
                    }
                    finally
                    {
                        DBStatus.IsUpdating = false;
                    }
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
                    if (DBStatus.IsUpdating) return;
                    DBStatus.IsUpdating = true;

                    try
                    {
                        // 다시 한번 최종 데이터 확인
                        var userHero = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == _heroData.id);
                        var costData = TableManager.Instance.HeroLevelTable.Get(userHero.level.ToString());

                        // 최종 조건 체크 (이미 경험치가 찼는지 && 골드가 충분한지)
                        if (userHero.exp >= costData.expRequirement && UserDataManager.Instance.WalletModel.gold >= costData.goldRequirement)
                        {
                            HeroStatData oldStatus = HeroManager.Instance.GetStatus(_heroData.id);

                            var updates = new Dictionary<string, object> { { "Wallet.gold", UserDataManager.Instance.WalletModel.gold - costData.goldRequirement } };
                            var heroes = new List<HeroDbModel>
                            {
                                new HeroDbModel
                                {
                                    heroId = _heroData.id,
                                    level = userHero.level + 1,
                                    exp = userHero.exp - costData.expRequirement,
                                    isUnlock = true
                                }
                            };

                            await UserDataManager.Instance.UpdateAll(updates, heroes);
                            RefreshAll();
                            ShowUpgradeResult(oldStatus);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"레벨업 실패 : {ex.Message}");
                    }
                    finally
                    {
                        DBStatus.IsUpdating = false;
                    }
                },
                isExpFull && isGoldEnough, // 버튼 활성화 조건: 경험치와 골드 모두 충족 시
                alertMsg
            );
        }
    }

    private void UpdateSkillAndAugmentPages()
    {
        _view.ClearSkillPage();
        _view.ClearAugmentPage();

        // SO에서 해당 영웅의 스킬 데이터 리스트 가져오기
        var skillDatas = _heroIcons.GetSkillIcons(_heroData.id);
        if (skillDatas == null) return;

        foreach (var skillSO in skillDatas)
        {
            if (skillSO == null) continue;

            // 이름, 설명
            string translatedName = TableManager.Instance.GetString(skillSO.skillName);
            string translatedDesc = TableManager.Instance.GetString(skillSO.skillDescription);

            //스킬 타입에 따른 분류 처리
            if (skillSO.skillType == SkillType.AugmentSkill)
            {
                // 증강 페이지에 추가
                _view.AddAugmentItem(translatedName, translatedDesc, skillSO.skillIcon);
            }
            else if(skillSO.skillType == SkillType.NormalSkill)
            {
                // 일반 스킬 페이지에 추가 (쿨타임 포함)
                string cooltimeStr = skillSO.cooldown > 0 ? $"{skillSO.cooldown}s" : "Passive";
                _view.AddSkillItem(translatedName, translatedDesc, cooltimeStr, skillSO.skillIcon);
            }
        }
    }

    //레벨 보상 페이지
    //private void UpdateLevelRewardDesPage()
    //{
    //    if (_userHeroData == null || _heroData == null) return;
    //
    //    //리워드 데이터 가져오기
    //    var myRewards = TableManager.Instance.HeroLevelRewardTable.GetAll()
    //        .FindAll(r=>r.heroId == _heroData.id);
    //    myRewards.Sort((a,b) => a.level.CompareTo(b.level)); //레벨순 정렬 3,6,6,9
    //
    //    for(int i = 0; i < _view.RewardButtons.Length; i++)
    //    {
    //        int index = i;
    //        _view.RewardButtons[i].onClick.RemoveAllListeners();
    //
    //        if (index < myRewards.Count)
    //        {
    //            string skillId = myRewards[index].rewardId;
    //
    //            // 버튼 클릭 시 해당 스킬 정보 표시
    //            _view.RewardButtons[index].onClick.AddListener(() => ShowLevelRewardDetail(skillId,index));
    //
    //            // 초기 화면 설정 (첫 번째 보상인 3레벨 보상을 먼저 보여줌)
    //            if (index == 0) ShowLevelRewardDetail(skillId, index);
    //        }
    //        else
    //        {
    //            // 데이터가 없는 버튼은 비활성화하거나 숨김 처리
    //            _view.RewardButtons[index].gameObject.SetActive(false);
    //        }
    //    }
    //}
    //
    //private void ShowLevelRewardDetail(string skillId,int index)
    //{
    //    if (string.IsNullOrEmpty(skillId)) return;
    //
    //    //스킬인포 테이블에서 데이터 가저오기
    //    var skillInfo = TableManager.Instance.SkillInfoTable.Get(skillId);
    //    if (skillInfo == null) return;
    //
    //    // 스트링 테이블에서 번역 텍스트 가져오기
    //    string translatedName = TableManager.Instance.GetString(skillInfo.skillName);
    //    string translatedDes = TableManager.Instance.GetString(skillInfo.skillDes);
    //
    //    //view 업데이트
    //    _view.UpdateLevelRewardUI(_userHeroData.level, translatedName, translatedDes, index);
    //}

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
        string typeName = TableManager.Instance.GetString(typeKey);

        var moveTypeItem = _view.AddStatItem("이동 유형", typeName, _statIcons.GetIcon(StatType.MoveType));

        if (moveTypeItem != null)
        {
            moveTypeItem.EnableToastButton(() => {
                UIManager.Instance.ShowToast<ToastUI>(_toastPrefab);
                Debug.Log($"{typeName} 토스트 버튼 클릭됨!");
            });
        }
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
