using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HeroManager : Singleton<HeroManager>
{
    private Dictionary<string, HeroStatusHandler> _activeHeroes = new Dictionary<string, HeroStatusHandler>();

    // 1. 씬에 있는 핸들러들 딕셔너리에 등록
    public void RegisterHeroHandler(string heroId, HeroStatusHandler handler)
    {
        if (!_activeHeroes.ContainsKey(heroId))
        {
            _activeHeroes.Add(heroId, handler);
        }
    }

    // 2. Firestore에서 가져온 리스트 한번에 주입 (데이터 브릿지)
    public void InitAllHeroes(List<HeroDbModel> dbHeroList)
    {
        foreach (var dbData in dbHeroList)
        {
            if (_activeHeroes.TryGetValue(dbData.heroId, out var handler))
            {
                handler.Initialize(dbData.level, dbData.exp, dbData.isUnlock);
            }
        }
        Debug.Log($"[HeroManager] {dbHeroList.Count}종의 영웅 데이터 동기화 완료.");
    }

    // 3. 특정 영웅 DB정보 변경 시 사용
    public void SyncWithDB(string heroId, int level, int exp ,bool isUnlock)
    {
        if (_activeHeroes.TryGetValue(heroId, out var handler))
        {
            handler.Initialize(level, exp, isUnlock);
        }

        var cachedHero = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == heroId);
        if (cachedHero != null)
        {
            cachedHero.level = level;
            cachedHero.exp = exp;
            cachedHero.isUnlock = isUnlock;
        }
    }

    // 4. 데이터 갱신 이벤트 처리
    public void BindSaveEvents(string userUuid, UserDataStore dataStore)
    {
        foreach (var handler in _activeHeroes.Values)
        {
            // 영웅의 데이터가 바뀔 때마다 DB 업데이트 메서드 호출
            handler.OnHeroDataChanged += (heroId, level, exp, unlock) =>
            {
                Task.Run(async () => await dataStore.UpdateHeroDataAsync(userUuid, heroId, level, exp, unlock));

                var cached = UserDataManager.Instance.HeroesModel.Find(h => h.heroId == heroId);
                if (cached != null)
                {
                    cached.level = level;
                    cached.exp = exp;
                    cached.isUnlock = unlock;
                }
            };
        }
    }
}
