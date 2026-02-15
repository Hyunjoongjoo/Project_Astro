using System;

//열거형, 공용 구조체, 인터페이스 등 모아둘 Definitions


//증강 타입
public enum AugmentType
{
    None = 0,
    Unlock = 1, //기체 해금
    Skill = 2,  //스킬 강화
    Item = 3    //아이템 장착
}

//영웅의 기본 스탯 + 각 유저의 성장치를 합친 결과물
//파이어베이스에서 캡슐화하여 받을 예정
[System.Serializable]
public class DummyHeroData
{
    public string nickname; //유저 닉네임 혹은 viewID
    public int heroID; //테이블 참조

    public float health;
    public float attack;
    public float defense;
    public float moveSpeed;
    //더 있지만 임시
}
