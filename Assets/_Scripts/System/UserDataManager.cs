using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class UserDataManager : Singleton<UserDataManager>
{
    //옵저버 패턴용 이벤트
    public event Action<int> OnGoldChanged;
    public event Action OnHeroDataChanged;

    private ProfileDbModel _profileModel;
    private RecordModel _recordModel;
    private WalletModel _walletModel;
    private List<HeroDbModel> _heroesModel = new List<HeroDbModel>();

    public void SetAllUserData(ProfileDbModel profile, RecordModel record, WalletModel wallet, List<HeroDbModel> heroes)
    {
        _profileModel = profile;
        _recordModel = record;
        _walletModel = wallet;
        _heroesModel = heroes;

        HeroManager.Instance.InitAllHeroStats(_heroesModel);

        Debug.Log($"[UserDataManager] 캐싱 완료: {profile.nickName}님 환영합니다.");
    }

    public ProfileDbModel ProfileModel => _profileModel;
    public RecordModel RecordModel => _recordModel;
    public WalletModel WalletModel => _walletModel;
    public List<HeroDbModel> HeroesModel => _heroesModel;

    // 골드 갱신
    public async Task UpdateWallet(int amount)
    {
        var updates = new Dictionary<string, object> 
        {
            { "Wallet.gold", _walletModel.gold + amount }
        };
        // DB 업데이트 하기
        await UserDataStore.Instance.UpdateUserDataAsync(_profileModel.uuid, updates);

        // 성공하면 로컬 캐싱 데이터 갱신
        _walletModel.gold += amount;

        Debug.Log($"[Sync] DB와 로컬 골드 동기화 완료: {_walletModel.gold}");

        // 골드 변경 알림 (구독자들에게 전파)
        OnGoldChanged?.Invoke(_walletModel.gold);
    }

    // 영웅 갱신
    public async Task UpdateHero(string heroId, int level, int exp, bool unlock)
    {
        // DB 업데이트
        await UserDataStore.Instance.UpdateHeroDataAsync(_profileModel.uuid, heroId, level, exp, unlock);

        // 성공 로컬 갱신
        var hero = _heroesModel.Find(h => h.heroId == heroId);
        if (hero != null)
        {
            hero.level = level;
            hero.exp = exp;
            hero.isUnlock = unlock;
        }

        // 런타임 스텟 재계산
        HeroManager.Instance.UpdateHeroRuntimeStatus(heroId, level);

        //데이터 변경됬다 구독자한테 알리기
        OnHeroDataChanged?.Invoke();
    }

    // 프로파일 갱신
    public async Task UpdateUserDb(int expDelta, int levelDelta = 0) 
    {
        // 변경될 데이터 딕셔너리
        var updates = new Dictionary<string, object>
        {
            { "Profile.userLevel", _profileModel.userLevel + levelDelta },
            { "Profile.userExp", _profileModel.userExp + expDelta }
        };

        try
        {
            // DB 업데이트
            await UserDataStore.Instance.UpdateUserDataAsync(_profileModel.uuid, updates);

            // 성공 로컬 갱신
            _profileModel.userLevel += levelDelta;
            _profileModel.userExp += expDelta;

            Debug.Log($"[UserDataManager] Profile Sync Success: Lv.{_profileModel.userLevel}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UserDataManager] Profile Sync Failed: {e.Message}");
        }
    }

    // 전적 갱신
    public async Task UpdateRecord(int winDelta, int loseDelta) 
    {
        var updates = new Dictionary<string, object>
        {
            { "Record.win", _recordModel.win + winDelta },
            { "Record.lose", _recordModel.lose + loseDelta }
        };

        try
        {
            // DB 업데이트
            await UserDataStore.Instance.UpdateUserDataAsync(_profileModel.uuid, updates);

            // 성공 로컬 갱신
            _recordModel.win += winDelta;
            _recordModel.lose += loseDelta;

            Debug.Log($"[UserDataManager] Record Sync Success: {_recordModel.win}W {_recordModel.lose}L");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UserDataManager] Record Sync Failed: {e.Message}");
        }
    }

    // 신규 영웅 생성 시(CSV 기준!) DB 대조하여 가감 (DB 로드 완료 이후 호출되어야함.)
    public async Task SyncHeroDataAsync()
    {
        string uuid = _profileModel.uuid;

        //CSV에 있는 모든 영웅 ID 가져오기 (최신 리스트)
        var allCsvHeroes = TableManager.Instance.HeroTable.GetAll();
        
        HashSet<string> csvHeroIds = new HashSet<string>();
        foreach (var csvHero in allCsvHeroes)
        {
            csvHeroIds.Add(csvHero.PrimaryID);
        }

        //유저가 현재 DB에 가지고 있는 영웅 리스트와 비교
        HashSet<string> userHeroIds = new HashSet<string>();
        foreach (var h in _heroesModel)
        {
            userHeroIds.Add(h.heroId);
        }

        List<Task> dbTasks = new List<Task>();

        // CSV에 있는데 기존 DB에 없으면 추가
        foreach (string cid in csvHeroIds)
        {
            if (!userHeroIds.Contains(cid))
            {
                Debug.Log($"[Sync] 누락된 영웅 발견: {cid}. DB에 추가합니다.");
                dbTasks.Add(UserDataStore.Instance.AddNewHeroAsync(uuid, cid));

                // 로컬 캐시 미리 추가
                _heroesModel.Add(new HeroDbModel
                {
                    heroId = cid,
                    level = 1,
                    exp = 0,
                    isUnlock = false
                });
            }
        }

        // CSV에 없는데 기존 DB에 있으면 삭제
        for (int i = _heroesModel.Count - 1; i >= 0; i--)
        {
            string currentHeroId = _heroesModel[i].heroId;

            if (!csvHeroIds.Contains(currentHeroId))
            {
                Debug.LogWarning($"[Sync] 제거된 영웅 발견: {currentHeroId}. DB에서 삭제합니다.");

                dbTasks.Add(UserDataStore.Instance.DeleteHeroAsync(uuid, currentHeroId));

                // 로컬 캐시에서 제거
                _heroesModel.RemoveAt(i);
            }
        }

        if (dbTasks.Count > 0)
        {
            await Task.WhenAll(dbTasks);
            Debug.Log($"[Sync] 데이터 동기화 완료 (작업 수: {dbTasks.Count})");

            // 4. HeroManager 런타임 스텟 재초기화
            HeroManager.Instance.InitAllHeroStats(_heroesModel);
        }
    }
}
