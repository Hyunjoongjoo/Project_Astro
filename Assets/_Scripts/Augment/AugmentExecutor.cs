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
                for (int i = 0; i < playerData.OwnedHeroes.Length; i++)
                {
                    if (string.IsNullOrEmpty(playerData.OwnedHeroes[i].ToString()))
                    {
                        playerData.OwnedHeroes.Set(i, refId); //넣기
                        break;
                    }
                }
                break;

            case AugmentType.Item:
                for (int i = 0; i < playerData.InventoryItems.Length; i++)
                {
                    if (string.IsNullOrEmpty(playerData.InventoryItems[i].ToString()))
                    {
                        playerData.InventoryItems.Set(i, refId);
                        break;
                    }
                }
                break;

            case AugmentType.Skill:
                for (int i = 0; i < playerData.OwnedSkillAugments.Length; i++)
                {
                    if (string.IsNullOrEmpty(playerData.OwnedSkillAugments[i].ToString()))
                    {
                        playerData.OwnedSkillAugments.Set(i, refId);
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