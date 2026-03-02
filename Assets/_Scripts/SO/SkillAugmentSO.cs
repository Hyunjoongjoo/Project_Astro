using UnityEngine;

//스킬 증강 데이터

[CreateAssetMenu(fileName = "SkillAugmentData_", menuName = "Scriptable Objects/Augment/Skill Augment Data")]
public class SkillAugmentSO : ScriptableObject
{
    [Header("식별 및 조건 데이터")]
    [Tooltip("이 증강 ID 적는 란, ex) Skill_Corsair_A")]
    public string AugmentID;

    [Tooltip("다른 트리 증강 ID, ex ~~~B, A랑 같이 등장하면 안됨")]
    public string ConflictID;

    [Tooltip("이 스킬 증강을 받을 수 있는 영웅 테이블 ID")] //현재 영웅 csv에 각 영웅의 스킬명 데이터가 없어서 여기서 연결하면 될 듯
    public string TargetHeroID;

    [Header("UI 기본 정보")]
    public string Title;
    public Sprite Icon;

    [Header("티어별 UI 설명 => 0: 기본, 1: config N회 달성 후 강화")]
    [TextArea]
    public SkillAugmentTier[] Tiers = new SkillAugmentTier[2];
}

//UI 설명 텍스트와 실제 전투에 적용될 스킬 데이터 묶어주는 구조체
[System.Serializable]
public struct SkillAugmentTier
{
    [TextArea]
    [Tooltip("설명")]
    public string Description;

    [Tooltip("증강 선택 시, 영웅에게 실제로 꽂아줄 스킬 전투 데이터")]
    //SO 클래스 참조
    public SkillDataSO CombatSkillData;
}
