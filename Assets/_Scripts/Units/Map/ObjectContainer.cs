using UnityEngine;

// 스테이지 씬에서 전역 접근 필요하면 여기 추가하세요
public class ObjectContainer : Singleton<ObjectContainer>
{
    public UnitBase[] blueSideStructure;
    public UnitBase[] redSideStructure;

    public int BridgeIndex {  get; private set; }

    private void Start()
    {
        BridgeIndex = 2;
    }

    public void IncreaseAugmentGauge(int amount)
    {

    }
}
