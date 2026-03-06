using System;

//CSV 데이터는 TableEnums 에 정의

//여긴 인게임 연산 로직, 데이터 전송 받을 객체만 정의

//증강 타입
public enum AugmentType
{
    Hero, //영웅 해금
    Skill,  //스킬 강화
    Item    //아이템 장착
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


//스탯별 계산 공식
public enum StatCalcMode
{
    Standard,   //체력, 공격력, 치유력 등 =>                     (Base * %연산) + Flat
    Delay,      //공속, 리스폰 등 => 값이 작을수록 좋음 =>        Base * (1 - 버프% + 디버프%)
    Speed,      //이속, 담지범위 =>                               Base * (1 + 버프% - 디버프%)
    Additive    //받피감, 쿨감 => 순수 퍼센트 수치만 저장         0(Base) + 버프% - 디버프% 
}

