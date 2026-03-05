using UnityEngine;
using Fusion;

//플레이어의 런타임 데이터 업데이트용 스태틱 클래스

public static class AugmentExecutor
{
    public static void ApplyAugment(StageManager stageManager, PlayerRef player, AugmentType type, string refId)
    {
        //플레이어 데이터 가져오기
        PlayerNetworkData playerData = stageManager.PlayerDataMap.Get(player);

        //타입에 맞춰서 데이터만 기록
        switch (type)
        {
            case AugmentType.Hero:
                for (int i = 0; i < SlotData_5.Length; i++) 
                {
                    if (string.IsNullOrEmpty(playerData.OwnedHeroes.Get(i).Replace("\0", "").Trim()))
                    {
                        playerData.OwnedHeroes = playerData.OwnedHeroes.Set(i, refId);
                        break;
                    }
                }
                break;

            case AugmentType.Item:
                for (int i = 0; i < SlotData_3.Length; i++)
                {
                    if (string.IsNullOrEmpty(playerData.InventoryItems.Get(i).Replace("\0", "").Trim()))
                    {
                        playerData.InventoryItems = playerData.InventoryItems.Set(i, refId);
                        break;
                    }
                }
                break;

            case AugmentType.Skill:
                for (int i = 0; i < SlotData_5.Length; i++)
                {
                    if (string.IsNullOrEmpty(playerData.OwnedSkillAugments.Get(i).Replace("\0", "").Trim()))
                    {
                        playerData.OwnedSkillAugments = playerData.OwnedSkillAugments.Set(i, refId);
                        break;
                    }
                }
                break;
        }

        //누적 횟수 증가
        playerData.TotalAugmentPicks++;

        //수정한 복사본을 다시 서버 딕셔너리에 덮어씌우기
        stageManager.PlayerDataMap.Set(player, playerData);

        Debug.Log($"{player} 의 데이터에 {type} : {refId} 저장");
    }
}