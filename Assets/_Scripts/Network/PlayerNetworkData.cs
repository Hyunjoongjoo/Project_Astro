using Fusion;

// 퓨전에 접속했을 때 필요한 플레이어 데이터들
// 1. 계정 닉네임
// 2. 배정된 팀
// 지금은 위 두개가 필요한데 나중에 계정 레벨, 프로필 사진? 등 추가될 수 있음

public struct PlayerNetworkData : INetworkStruct
{
    public NetworkString<_16> PlayerName; // 닉네임 최대 16자
    public Team Team;


    //인게임 증강 상태 추적용

    //지금까지 증강을 고른 총 횟수 (Config테이블 N회 검사용)
    public int TotalAugmentPicks;

    //내 필드/덱에 소환한 영웅 ID 목록 (최대 5마리)
    public SlotData_5 OwnedHeroes;

    //내가 선택해서 장착한 스킬 증강 ID 목록 (최대 5개 => 영웅당 1개)
    public SlotData_5 OwnedSkillAugments;
    
    //내 보관소에 들어있는 아이템 증강 ID 목록 (Config테이블 3개)
    public SlotData_3 InventoryItems;

    // [Networked] 속성을 사용하여 딕셔너리 선언 (최대 32개까지 저장 가능하도록 설정)
    [Networked, Capacity(32)]
    public NetworkDictionary<NetworkString<_32>, NetworkBool> UsedHeroes => default;

    // 영웅 사용 기록 메서드
    public void MarkHeroUsed(string heroId)
    {
        if (UsedHeroes.Count < 32)
        {
            // Dictionary이므로 중복 ID는 알아서 처리됨
            UsedHeroes.Set(heroId, true);
        }
    }
}
