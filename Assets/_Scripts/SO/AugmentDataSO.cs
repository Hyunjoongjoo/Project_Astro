using Fusion;
using UnityEngine;

[System.Serializable]
public class AugmentData
{
    //공통 정보
    public string instanceId; //이번 턴 화면에 뜬 카드의 고유 ID (중복 선택 방어용)
    public AugmentType type;  //영웅, 아이템, 스킬
    public string targetId;   //테이블, SO 연결용 ID (Hero_Knight, Skill_Corsair_B 등)

    //카드 메인 UI 정보
    public string titleName;      //증강에 띄울 이름
    public string description;//증강에 띄울 설명
    public Sprite mainIcon;        //증강에 띄울 아이콘

    [Header("Hero Specific")]
    public NetworkPrefabRef heroPrefab;
    //영웅 타입, 역할, 이동 유형
    public HeroType heroType;
    public HeroRole heroRole;
    public MoveType moveType;
    public float baseSpawnCooldown;    //원본 쿨타임
    public float currentSpawnCooldown; //최종

    [Header("Skill Specific")]
    public string targetHeroName;      //"~커세어~의 스킬을 강화합니다" 에 들어갈 영웅 이름
    public Sprite targetHeroIcon;      //카드 구석에 띄울 원본 영웅 얼굴 아이콘
    public string baseSkillName;       //"~코로나 폭발~ 업그레이드" 에 들어갈 원본 스킬 이름
    public SkillDataSO skillData;
}