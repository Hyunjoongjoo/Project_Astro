using Fusion;
using UnityEngine;
public enum AugmentType
{
    Hero,Item,Skill
}
[System.Serializable]
public class AugmentData
{
    public string id;
    public AugmentType type;
    public string name;
    public string description;
    public Sprite icon;  // 아이콘 이미지 나중에 SO나 스프라이트나 머든간에 만들어서 연결
    public string targetId;  //테이블 연결용 id
    public NetworkPrefabRef heroPrefab; //영웅 증강이면 소환용 프리팹
}
[CreateAssetMenu(fileName = "AugmentDataSO", menuName = "Scriptable Objects/AugmentDataSO")]
public class AugmentDataSO : ScriptableObject
{
    public AugmentData[] data;
}
