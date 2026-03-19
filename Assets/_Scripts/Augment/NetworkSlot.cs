using Fusion;

//5칸 영웅/스킬 보관함 전용 구조체
public struct SlotData_5 : INetworkStruct
{
    public const int Length = 5;//기획서대로
    public NetworkString<_32> Slot0, Slot1, Slot2, Slot3, Slot4;
    public int Count => Length;
    public string Get(int i)
    {
        switch (i)
        {
            case 0: return Slot0.ToString();
            case 1: return Slot1.ToString();
            case 2: return Slot2.ToString();
            case 3: return Slot3.ToString();
            case 4: return Slot4.ToString();
            default: return "";
        }
    }
    public SlotData_5 Set(int i, string val) 
    {
        switch (i) 
        { 
            case 0: Slot0 = val; 
                break; 
            case 1: Slot1 = val; 
                break; 
            case 2: Slot2 = val; 
                break; 
            case 3: Slot3 = val; 
                break; 
            case 4: Slot4 = val; 
                break; 
        }
        return this; // 수정된 복사본 반환
    }
}

//3칸짜리 아이템 보관함 전용 구조체
public struct SlotData_3 : INetworkStruct
{

    public const int Length = 3;//기획서대로
    public NetworkString<_32> Slot0, Slot1, Slot2;
    public string Get(int i)
    {
        switch (i)
        {
            case 0: return Slot0.ToString();
            case 1: return Slot1.ToString();
            case 2: return Slot2.ToString();
            default: return "";
        }
    }
    public SlotData_3 Set(int i, string val) 
    { 
        switch (i) 
        { 
            case 0: Slot0 = val; 
                break; 
            case 1: Slot1 = val; 
                break; 
            case 2: Slot2 = val; 
                break; 
        } 
        return this;
    }

}

//영웅 장착 아이템용 배열 5x2
public struct SlotData_10 : INetworkStruct
{
    public const int Length = 10;
    public NetworkString<_32> Slot0, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7, Slot8, Slot9;

    public string Get(int i)
    {
        switch (i)
        {
            case 0: return Slot0.ToString();
            case 1: return Slot1.ToString();
            case 2: return Slot2.ToString();
            case 3: return Slot3.ToString();
            case 4: return Slot4.ToString();
            case 5: return Slot5.ToString();
            case 6: return Slot6.ToString();
            case 7: return Slot7.ToString();
            case 8: return Slot8.ToString();
            case 9: return Slot9.ToString();
            default: return "";
        }
    }
    public SlotData_10 Set(int i, string val)
    {
        switch (i)
        {
            case 0: Slot0 = val; break;
            case 1: Slot1 = val; break;
            case 2: Slot2 = val; break;
            case 3: Slot3 = val; break;
            case 4: Slot4 = val; break;
            case 5: Slot5 = val; break;
            case 6: Slot6 = val; break;
            case 7: Slot7 = val; break;
            case 8: Slot8 = val; break;
            case 9: Slot9 = val; break;
        }
        return this;
    }
}