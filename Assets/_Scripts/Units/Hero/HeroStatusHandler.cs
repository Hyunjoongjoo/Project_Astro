using System;

public class HeroStatusHandler
{
    // 특정 영웅의 레벨에 따른 최종 스텟을 계산하여 반환
    public static HeroStatData CalculateRuntimeStatus(HeroData heroData, int level)
    {
        if (heroData == null) return null;

        string statusId = heroData.heroStatId;

        // 레벨 1 기준 베이스 데이터 복제
        //여현구 02.23  수정
        //1. 확장메서드(Clone) 사용
        //2. 불필요한 이중복사 중복코드 제거
        var resultStatus = TableManager.Instance.HeroStatTable.Get(statusId).Clone();

       
        if(resultStatus == null) return null;

        // 레벨업 스텟 계산하기
        if (level > 1)
        {
            int growthStep = level - 1;
            resultStatus.BaseHp = (int)Math.Round(resultStatus.BaseHp + (resultStatus.ipLvHp * growthStep));
            resultStatus.baseShield = (int)Math.Round(resultStatus.baseShield + (resultStatus.ipLvShield * growthStep));
            resultStatus.baseAttackPower = (int)Math.Round(resultStatus.baseAttackPower + (resultStatus.ipLvAttackPower * growthStep));
            resultStatus.baseHealingPower = (int)Math.Round(resultStatus.baseHealingPower + (resultStatus.ipLvHealingPower * growthStep));
        }

        return resultStatus;
    }
}
