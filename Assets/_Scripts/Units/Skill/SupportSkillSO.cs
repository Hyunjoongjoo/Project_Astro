using UnityEngine;

[CreateAssetMenu(fileName = "SupportSkillSO", menuName = "Scriptable Objects/SupportSkillSO")]
public class SupportSkillSO : SkillDataSO
{
    [Header("지원형 설정")]
    public float healAmount;
    public float effectLifeTime;
}
