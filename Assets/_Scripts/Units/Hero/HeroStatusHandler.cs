using System;

public class HeroStatusHandler
{
    // 특정 영웅의 레벨에 따른 최종 스텟을 계산하여 반환
    public static HeroStatData CalculateRuntimeStatus(HeroData heroData, int level)
    {
        if (heroData == null) return null;

        string statusId = heroData.heroStatId;

        // 레벨 1 기준 베이스 데이터 복제
        var statusSheetData = TableManager.Instance.HeroStatTable.Get(statusId);

        if(statusSheetData == null) return null;

        HeroStatData resultStatus = new HeroStatData();

        resultStatus.id = statusSheetData.id;
        resultStatus.BaseHp = statusSheetData.BaseHp;
        resultStatus.baseShield = statusSheetData.baseShield;
        resultStatus.baseAttackPower = statusSheetData.baseAttackPower;
        resultStatus.baseHealingPower = statusSheetData.baseHealingPower;
        resultStatus.spawnCooldown = statusSheetData.spawnCooldown;
        resultStatus.ipLvHp = statusSheetData.ipLvHp;
        resultStatus.ipLvShield = statusSheetData.ipLvShield;
        resultStatus.ipLvAttackPower = statusSheetData.ipLvAttackPower;
        resultStatus.attackSpeed = statusSheetData.attackSpeed;
        resultStatus.armorType = statusSheetData.armorType;
        resultStatus.moveType = statusSheetData.moveType;
        resultStatus.moveSpeed = statusSheetData.moveSpeed;
        resultStatus.detectionRange = statusSheetData.detectionRange;
        resultStatus.note = statusSheetData.note;
                
        // 레벨업 스텟 계산하기
        if (level > 1)
        {
            int growthStep = level - 1;
            resultStatus.BaseHp = (int)Math.Round(statusSheetData.BaseHp + (statusSheetData.ipLvHp * growthStep));
            resultStatus.baseShield = (int)Math.Round(statusSheetData.baseShield + (statusSheetData.ipLvShield * growthStep));
            resultStatus.baseAttackPower = (int)Math.Round(statusSheetData.baseAttackPower + (statusSheetData.ipLvAttackPower * growthStep));
            resultStatus.baseHealingPower = (int)Math.Round(statusSheetData.baseHealingPower + (statusSheetData.ipLvHealingPower * growthStep));
        }

        return resultStatus;
    }
}
