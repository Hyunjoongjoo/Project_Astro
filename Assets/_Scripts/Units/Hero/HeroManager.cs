using System.Collections.Generic;
using UnityEngine;

public class HeroManager : Singleton<HeroManager>
{
    // 최종 계산된 런타임 스텟 보관소 담당
    private Dictionary<string, HeroStatData> _runtimeHeroStats = new Dictionary<string, HeroStatData>();

    // 유저 정보 캐싱할떄 호출해서 캐싱하기
    public void InitAllHeroStats(List<HeroDbModel> dbHeroes)
    {
        foreach (var dbData in dbHeroes)
        {
            UpdateHeroRuntimeStatus(dbData.heroId, dbData.level);
        }
    }

    // 특정 영웅 레벨값 갱신되면 여기
    public void UpdateHeroRuntimeStatus(string heroId, int level)
    {
        // 베이스 스텟이랑 레벨 상승 스텟(둘다 CSV 파일 가져다 쓰기)
        var baseData = TableManager.Instance.HeroTable.Get(heroId); //일단 되는대로 집어넣긴 했는데 Status쪽 CSV 오면 그때 제대로 수정 들어갈듯

        if (baseData != null)
        {
            // Handler에 넣어서 계산해오기
            HeroStatData calculatedStatus = HeroStatusHandler.CalculateRuntimeStatus(baseData, level);

            // 딕셔너리에 저장하기
            _runtimeHeroStats[heroId] = calculatedStatus;

            Debug.Log($"[HeroManager] {heroId} 데이터 갱신 완료 (Lv.{level})");
        }
    }

    // 데이터를 요청 할때 이거 쓰면 됩니다
    public HeroStatData GetStatus(string heroId)
    {
        foreach (var key in _runtimeHeroStats.Keys)
        {
            Debug.Log($"현재 딕셔너리 키: [{key}] / 요청한 키: [{heroId}]");
        }

        if (_runtimeHeroStats.TryGetValue(heroId, out var status))
        {
            Debug.Log("겟스테이터스 뿌리기까지 성공함.");
            return status;
        }
        Debug.Log("겟스테이터스 뿌리기 실패함.");
        return null;
    }
}
