using Fusion;
using UnityEngine;

[System.Serializable]
public class AugmentData
{
    public string instanceId; //이번 턴 화면에 뜬 카드의 고유 ID (중복 선택 방어용)
    public AugmentType type;  //영웅, 아이템, 스킬
    public string targetId;   //테이블, SO 연결용 ID (Hero_Knight, Skill_Corsair_B 등)
    public string name;       //증강에 띄울 이름
    public string description;//증강에 띄울 설명
    public Sprite icon;       //증강에 띄울 아이콘
    public NetworkPrefabRef heroPrefab;

    [Header("Runtime Stats")]
    public float baseSpawnCooldown;    //원본 쿨타임
    public float currentSpawnCooldown; //최종
}