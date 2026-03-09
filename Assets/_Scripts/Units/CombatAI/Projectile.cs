using Fusion;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Renderer _targetRenderer;
    private bool isInitialized = false;
    
    private float _finalPower;

    ProjectileSkillSO _data;
    private NetworkRunner _runner;
    Team _team;

    private Vector3 _target;
    private float _homingRotationSpeed = 10f; // 유도탄의 궤적 꺾임 속도 (유도탄 테스트를 위한 임시 값)

    public void Initialize(ProjectileSkillSO data, Team team, float power, NetworkRunner runner)
    {
        _data = data;
        _team = team;
        _runner = runner;
        _finalPower = power * _data.damageRatio;
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
            }

            transform.Translate(Vector3.forward * _data.projectileSpeed * Time.deltaTime);
        }
        else
            Debug.LogError("[projectile] 초기화가 되지 않았음!!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if ( other.gameObject.TryGetComponent(out UnitBase target) )
        {
            if (target.networkedTeam != _team)
            {
                if (_runner.IsSharedModeMasterClient)
                    target.TakeDamage(_finalPower);
                Destroy(gameObject);
            }
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
