using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Renderer _targetRenderer;
    private bool isInitialized = false;

    private float _finalPower;
    private int _remainingPierce;

    ProjectileSkillSO _data;
    private NetworkRunner _runner;
    Team _team;

    private HashSet<UnitBase> _hitTargets = new HashSet<UnitBase>();

    private Vector3 _target;
    private float _homingRotationSpeed = 10f; // 유도탄의 궤적 꺾임 속도 (유도탄 테스트를 위한 임시 값)
    private bool errorMsg = false;

    public void Initialize(ProjectileSkillSO data, Team team, float power, NetworkRunner runner)
    {
        _data = data;
        _team = team;
        _runner = runner;
        _finalPower = power * _data.damageRatio;
        _remainingPierce = _data.pierceCount;
        _hitTargets.Clear();
        ApplyTeamColor(team);
        isInitialized = true;
    }

    public void Fire(Vector3 targetPos)
    {
        _target = targetPos;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxList.ShotLaserSound);

        Destroy(gameObject, 3f); // TODO : 오브젝트 풀링 해야하는 부분
    }

    private void FixedUpdate()
    {
        if (isInitialized)
        {
            // 유도성(isHoming)이 켜져 있을 경우
            if (_data.isHoming)
            {
                Vector3 directionToTarget = (_target - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

                // 현재 방향에서 타겟 방향으로 부드럽게 회전 (보간)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _homingRotationSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _target) < 0.5f)
                    Destroy(gameObject);
            }

            transform.Translate(Vector3.forward * _data.projectileSpeed * Time.deltaTime);
        }
        else
        {
            if (errorMsg == false)
            {
                Debug.LogError($"[projectile] 이 투사체는 초기화가 되지 않았음!! {_runner.name}");
                errorMsg = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out UnitBase target)) return;
        if (target.Object == null || target.Object.IsValid == false) return;
        if (target.networkedTeam == _team) return;
        if (_hitTargets.Contains(target)) return;

        _hitTargets.Add(target);

        if (_runner.IsSharedModeMasterClient)
            target.TakeDamage(_finalPower);

        // 광역 피해
        if (_data.areaOfEffect && _runner.IsSharedModeMasterClient)
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                _data.attackRange
            );

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent(out UnitBase enemy)) continue;
                if (enemy.networkedTeam == _team) continue;
                if (enemy == target) continue;//타겟이 2번 맞는것은 제외
                enemy.TakeDamage(_finalPower);
            }
        }

        if (_remainingPierce > 0)
        {
            _remainingPierce--;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    // 팀 색에 맞춰 투사체에도 색상 적용
    private void ApplyTeamColor(Team team)
    {
        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponentInChildren<Renderer>();
        }

        Material mat = _targetRenderer.material;

        if (team == Team.Blue)
        {
            mat.SetColor("_Color01", new Color(0.2f, 0.6f, 1f));
            mat.SetColor("_Color02", new Color(0f, 0.8f, 1f));
        }
        else
        {
            mat.SetColor("_Color01", new Color(1f, 0.3f, 0.1f));
            mat.SetColor("_Color02", new Color(1f, 0.6f, 0.2f));
        }
    }
}
