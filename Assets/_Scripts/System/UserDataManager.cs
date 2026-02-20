using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class UserDataManager : Singleton<UserDataManager>
{
    private UserDbModel _profileModel;
    private RecordModel _recordModel;
    private WalletModel _walletModel;
    private List<HeroDbModel> _heroesModel = new List<HeroDbModel>();

    public void SetAllUserData(UserDbModel profile, RecordModel record, WalletModel wallet, List<HeroDbModel> heroes)
    {
        _profileModel = profile;
        _recordModel = record;
        _walletModel = wallet;
        _heroesModel = heroes;

        HeroManager.Instance.InitAllHeroStats(_heroesModel);

        Debug.Log($"[UserDataManager] 캐싱 완료: {profile.nickName}님 환영합니다.");
    }

    public UserDbModel ProfileModel => _profileModel;
    public RecordModel RecordModel => _recordModel;
    public WalletModel WalletModel => _walletModel;
    public List<HeroDbModel> HeroesModel => _heroesModel;

    public async Task UpdateWallet(int amount)
    {
        // DB 업데이트 하기
        await UserDataStore.Instance.UpdateWalletAsync(_profileModel.uuid, amount);

        // 성공하면 로컬 캐싱 데이터 갱신
        _walletModel.gold += amount;

        Debug.Log($"[Sync] DB와 로컬 골드 동기화 완료: {_walletModel.gold}");
    }

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
    }

    public async Task UpdateUserDb(int expDelta, int levelDelta = 0) 
    {
        // 변경될 데이터 딕셔너리
        var updates = new Dictionary<string, object>
        {
            { "userLevel", _profileModel.userLevel + levelDelta },
            { "userExp", _profileModel.userExp + expDelta }
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

    public async Task UpdateRecord(int winDelta, int loseDelta) 
    {
        try
        {
            // DB 업데이트
            await UserDataStore.Instance.UpdateRecordAsync(_profileModel.uuid, winDelta, loseDelta);

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
}
