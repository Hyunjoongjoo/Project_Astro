using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
public class CardData : ScriptableObject
{
    public Sprite heroImg;
    public NetworkPrefabRef heroPrefab;
}
