using UnityEngine;

[CreateAssetMenu(fileName = "BarrageSkillSO", menuName = "Scriptable Objects/BarrageSkillSO")]
public class BarrageSkillSO : SkillDataSO
{
    [Header("포격형 설정")]
    [SerializeField] private int _shotCount = 3;
    [SerializeField] private float _shotInterval = 0.05f;
    [SerializeField] private float _damageMultiplier = 1f;

    public override SkillRuntimeData CreateRuntimeData()
    {
        SkillRuntimeData runtime = base.CreateRuntimeData();

        runtime.ShotCount = ShotCount;

        return runtime;
    }
    public int ShotCount => _shotCount;
    public float ShotInterval => _shotInterval;
    public float DamageMultiplier => _damageMultiplier;
}
