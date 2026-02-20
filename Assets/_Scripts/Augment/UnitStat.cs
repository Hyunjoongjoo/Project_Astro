using Fusion;
using UnityEngine;

//현재 스탯을 계산하여 유닛에게 달아줄 관리자
//
public class UnitStat : MonoBehaviour //추후 NetWorkBehavior
{

    //네트워크 동기화 변수
    //얘도 추후 Networked
    public float BaseHealth {  get; set; }
    public float BaseAttack {  get; set; }
    public float BaseDefense {  get; set; }
    public float BaseMoveSpeed {  get; set; }

    public float CurrentHealth { get; set; }
    //추후 기본스탯 변경 및 추가

    //인게임 아이템/버프 연산용
    private float _addedAttack = 0f;
    private float _addedDefense = 0f;
    private float _addedMoveSpeed = 0f;

    //최종 스탯 프로퍼티
    public float FinalAttack => BaseAttack + _addedAttack;
    public float FinalDefense => BaseDefense + _addedDefense;
    public float FinalMoveSpeed => BaseMoveSpeed + _addedMoveSpeed;

    //초기화
    public void Init(HeroGrowthData data)
    {
        BaseHealth = data.health;
        BaseAttack = data.attack;
        BaseMoveSpeed = data.moveSpeed;

        CurrentHealth = BaseHealth;

        Debug.Log($"UnitStat => {data.nickname} 스탯 초기화 완료");
        Debug.Log($"테스트용 => HP: {BaseHealth}, ATK: {BaseAttack}, DEF: {BaseDefense}");
    }

    //일단 3개
    public void AddModifier(float atk, float def, float spd)
    {
        //나중엔 연산식이 들어갈 예정
        //오버로딩도 생각
        _addedAttack += atk;
        _addedDefense += def;
        _addedMoveSpeed += spd;

        Debug.Log($"UnitStat => 최종 공격력:{FinalAttack} = 기본 {BaseAttack} + 추가 {_addedAttack}");
    }

    public void TakeDamage(float damage)
    {
        //데미지 계산식도 여기 추가, 지금은 방어력 차감만
        float actualDamage = Mathf.Max(damage - FinalDefense, 1f);
        CurrentHealth -= actualDamage;

        Debug.Log($"UnitStat TakeDamage => {actualDamage} 피해, 남은 체력 {CurrentHealth}");
    }
}
