using UnityEngine;

[CreateAssetMenu(fileName = "BarrageSkillSO", menuName = "Scriptable Objects/BarrageSkillSO")]
public class BarrageSkillSO : SkillDataSO
{
    [Header("포격형 설정")]
    public int shotCount = 3;
    public float shotInterval = 0.05f;
    public float damageMultiplier = 1f;
}
