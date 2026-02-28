using Fusion;

// 퓨전에 접속했을 때 필요한 플레이어 데이터들
// 1. 계정 닉네임
// 2. 배정된 팀
// 지금은 위 두개가 필요한데 나중에 계정 레벨, 프로필 사진? 등 추가될 수 있음

public struct PlayerNetworkData : INetworkStruct
{
    public NetworkString<_16> PlayerName; // 닉네임 최대 16자
    public Team Team;
}
