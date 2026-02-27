using UnityEngine;

[CreateAssetMenu(fileName = "AssaultSkillSO", menuName = "Scriptable Objects/AssaultSkillSO")]
public class AssaultSkillSO : SkillDataSO
{
    [Header("강습형 설정")]
    public float radius;
    public float damageMultiplier;
    public float effectLifeTime;
}
