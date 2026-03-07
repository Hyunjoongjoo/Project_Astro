using UnityEngine;

[CreateAssetMenu(fileName = "ShieldSkillSO", menuName = "Scriptable Objects/Skills/Shield Skill")]
public class ShieldSkillSO : ScriptableObject
{
    public float damageReduction;
    public float duration;
    public GameObject shieldVFX;
}
