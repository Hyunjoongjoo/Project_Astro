using UnityEngine;

[CreateAssetMenu(fileName = "DefenseSkillSO", menuName = "Scriptable Objects/DefenseSkillSO")]
public class DefenseSkillSO : SkillDataSO
{
    [Header("방어형 설정")]
    public float duration;
    public float damageReductionRate;
}
