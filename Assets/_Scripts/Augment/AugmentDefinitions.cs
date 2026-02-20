using System;

//CSV 데이터는 TableEnums 에 정의

//여긴 인게임 연산 로직, 데이터 전송 받을 객체만 정의

//증강 타입
public enum AugmentType
{
    None = 0,
    Unlock = 1, //기체 해금
    Skill = 2,  //스킬 강화
    Item = 3    //아이템 장착
}

//임시
//영웅의 기본 스탯 + 각 유저의 성장치를 합친 결과물
//파이어베이스에서 캡슐화하여 받을 예정
[System.Serializable]
public class HeroGrowthData
{
    public string nickname; //유저 닉네임 혹은 viewID
    public int heroID; //테이블 참조

    //방어 관련
    public float health;            //체력
    public float shield;            //쉴드
    public float damageReduction;   //피감

    //공격 관련
    public float attack;
    public float healPower;
    public float attackSpeed;

    //유틸 관련
    public float moveSpeed;         //이속
    public float respawnTime;       //리스폰
    public float cooldownReduction; //쿨다운
    public float detectRange;       //탐지범위
}

//스탯 연산 로직용 Enum


//기획서 상 그룹별 합산 후 곱연산 방식을 위한 enum
//연산타입별 우선순위 부여
//리스트 정렬시켜서 깡스탯 먼저 계산하도록
public enum StatModType
{
    Flat = 100,         //고정값
    PercentAdd = 200,   //% 증가 합연산
    PercentMult = 300   //% 감소 곱연산, 복리
}


//합연산 하면 안되는 애들 => 쿨감 등등
//감소 옵션이 없거나 복잡한 로직이 아닌 애들 => 쉴드, 피감
//이외엔 그룹별 합산 후 곱연산


//스탯별 계산 공식
public enum StatCalcMode
{
    Standard = 0,   //체력, 공격력, 치유력 =>  증가량 감소량을 따로 계산
    SimpleSum = 1,  //실드, 탐지범위, 피해감소 =>  단순 값 더하기
    Individual = 2, //공속, 쿨감 =>  그냥 더하면 100% 감소가 되버릴 수 있어서 곱해서 줄여야 함
}

