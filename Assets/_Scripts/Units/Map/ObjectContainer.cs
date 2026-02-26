using UnityEngine;
using TMPro;

public class ObjectContainer : Singleton<ObjectContainer>
{
    public UnitBase[] blueSideStructure;
    public UnitBase[] redSideStructure;
    public TextMeshProUGUI timer;

    public int BridgeIndex {  get; private set; }

    private void Start()
    {
        BridgeIndex = 2;
    }
}
