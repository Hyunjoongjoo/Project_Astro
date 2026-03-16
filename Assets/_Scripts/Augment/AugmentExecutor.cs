using Fusion;
using System.Collections.Generic;
using UnityEngine;

//플레이어의 런타임 데이터 업데이트용 스태틱 클래스

public static class AugmentExecutor
{
    public static void ApplyAugment(StageManager stageManager, PlayerRef player, AugmentType type, string refId)
    {
        //플레이어 데이터 가져오기
        PlayerNetworkData playerData = stageManager.PlayerDataMap.Get(player);

        //누적 횟수 증가 => 카드확정시에만
        playerData.TotalAugmentPicks++;

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
                stageManager.PlayerDataMap.Set(player, playerData);
                Debug.Log($"{player} 의 데이터에 {type} : {refId} 저장");
                break;

            case AugmentType.Item:
                //아이템은 본인 보관소로
                //3.14 꽉 찼는 지 체크용
                bool isStored = false;
                for (int i = 0; i < SlotData_3.Length; i++)
                {
                    if (string.IsNullOrEmpty(playerData.InventoryItems.Get(i).Replace("\0", "").Trim()))
                    {
                        playerData.InventoryItems = playerData.InventoryItems.Set(i, refId);
                        isStored = true;
                        break;
                    }
                }
                //인벤토리가 꽉 찼다면 임시 슬롯에 보관
                if (!isStored)
                {
                    if (string.IsNullOrEmpty(playerData.TempItemSlot.ToString().Replace("\0", "").Trim()))
                    {
                        playerData.TempItemSlot = refId;
                        Debug.Log($"{player} 보관함 가득참. 임시 슬롯에 아이템 {refId} 저장");
                    }
                    else
                    {
                        Debug.LogWarning($"{player} 임시 슬롯도 가득참, 그냥 로그만 => 근데 그럴 일이 있을까?");
                    }
                }
                else
                {
                    Debug.Log($"{player} 의 데이터에 {type} : {refId} 저장");
                }
                stageManager.PlayerDataMap.Set(player, playerData);
                ItemManager.Instance.RPC_RefreshItemUI(player);
                break;

            case AugmentType.Skill:
                //스킬 증강은 진영 간에 공유됨
                //본인과 아군 모두 스킬 상태 슬롯에 증강 기록 적용
                Team myTeam = playerData.Team;

                //구매자의 TotalAugmentPicks 증가된 내역 덮어씌우기
                stageManager.PlayerDataMap.Set(player, playerData);

                //현재 접속 중인 우리 팀원들 모두 찾기
                List<PlayerRef> teamMembers = new List<PlayerRef>();
                foreach (var kvp in stageManager.PlayerDataMap)
                {
                    if (kvp.Value.Team == myTeam) teamMembers.Add(kvp.Key);
                }


                //팀원 전원의 스킬 슬롯에 증강 효과 일괄 적용
                foreach (var member in teamMembers)
                {
                    PlayerNetworkData memberData = stageManager.PlayerDataMap.Get(member);

                    for (int i = 0; i < SlotData_5.Length; i++)
                    {
                        //빈 슬롯을 찾아 스킬 증강 상태 기록
                        if (string.IsNullOrEmpty(memberData.OwnedSkillAugments.Get(i).Replace("\0", "").Trim()))
                        {
                            memberData.OwnedSkillAugments = memberData.OwnedSkillAugments.Set(i, refId);
                            break;
                        }
                    }
                    stageManager.PlayerDataMap.Set(member, memberData);
                    Debug.Log($"아군 플레이어 {member} 의 스킬 상태 갱신 (적용된 스킬: {refId})");
                }
                break;
        }
    }
}