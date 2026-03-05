using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class HeroSpawner : NetworkBehaviour
{
    public static HeroSpawner Instance { get; private set; }

    [Header("배치 설정")]
    [SerializeField] private float _minDeployTime = 0.25f;
    [SerializeField] private float _maxDeployTime = 2.5f;
    [SerializeField] private float _minDeployDistance = 1f;
    [SerializeField] private float _maxDeployDistance = 15f;

    // 플레이어,영웅 프리팹 기준으로 소환 쿨다운을 분리 관리
    private readonly Dictionary<(PlayerRef, NetworkPrefabRef), TickTimer> _respawnTimers
    = new Dictionary<(PlayerRef, NetworkPrefabRef), TickTimer>();

    public override void Spawned()
    {
        Instance = this;
    }

    private bool CanSummon(PlayerRef player, NetworkPrefabRef prefab)//소환 가능한지 여부
    {
        var key = (player, prefab);

        if (!_respawnTimers.TryGetValue(key, out TickTimer timer))
        {
            return true;
        }

        return timer.ExpiredOrNotRunning(Runner);
    }

    public bool CanDeployHero(Vector3 spawnPos, Team team)//해당 위치에 영웅 배치가 가능한지 검사
    {
        if (!Object.HasStateAuthority)
        {
            return false;
        }

        if (!GameManager.Instance.IsGameStarted) // 카운트다운 중 차단
        {
            return false;
        }

        Transform deployOrigin = GetDeployOrigin(team);
        if (deployOrigin == null)
        {
            return false;
        }

        //최대 배치 거리 초과 시 차단
        float distance = Vector3.Distance(deployOrigin.position, spawnPos);
        return distance <= _maxDeployDistance;
    }

    private Transform GetDeployOrigin(Team team)//함교 위치를 반환
    {
        UnitBase[] myStructures = team == Team.Blue
            ? ObjectContainer.Instance.blueSideStructure
            : ObjectContainer.Instance.redSideStructure;

        //index 2 = 함교
        return myStructures != null && myStructures.Length > 2
            ? myStructures[2].transform
            : null;
    }

    private float GetDeployDelay(float distance)//거리 기반 배치 지연시간
    {
        float time = Mathf.InverseLerp(_minDeployDistance, _maxDeployDistance, distance);
        return Mathf.Lerp(_minDeployTime, _maxDeployTime, time);
    }

    private void StartSummonCooldown(PlayerRef player, NetworkPrefabRef prefab, float cooldown)
    {
        var key = (player, prefab);
        _respawnTimers[key] = TickTimer.CreateFromSeconds(Runner, cooldown);
    }

    //UI에서 사용할수있도록 메서드로 지정한 플레이어와 프리팹에 대한 남은 소환 쿨타임을 반환
    public float GetRemainingCooldown(PlayerRef player, NetworkPrefabRef prefab)
    {
        //Runner가 아직 준비되지 않았으면 쿨 없음으로 처리
        if (Runner == null || !Runner.IsRunning)
        {
            return 0f;
        }

        var key = (player, prefab);

        if (!_respawnTimers.TryGetValue(key, out TickTimer timer))
        {
            return 0f;
        }

        if (timer.ExpiredOrNotRunning(Runner))
        {
            return 0f;
        }

        return timer.RemainingTime(Runner).GetValueOrDefault();
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team, RpcInfo info = default)
    {
        if (prefab == default)
        {
            return;
        }

        PlayerRef caller = info.Source;

        if (!CanSummon(caller, prefab))
        {
            return;
        }

        if (!CanDeployHero(spawnPos, team))
        {
            return;
        }

        Transform origin = GetDeployOrigin(team);
        float distance = Vector3.Distance(origin.position, spawnPos);
        float deployDelay = GetDeployDelay(distance);

        //영웅 초기 방향 결정
        Vector3 forwardDir = team == Team.Blue ? Vector3.forward : Vector3.back;
        Quaternion spawnRot = Quaternion.LookRotation(forwardDir);

        Runner.Spawn(prefab, spawnPos, spawnRot,
            onBeforeSpawned: (Runner, obj) =>
            {
                HeroController hero = obj.GetComponent<HeroController>();
                hero.Setup(team);
                //배치 및 지연 처리는 컨트롤러가 수행
                hero.BeginDeploy(spawnPos, deployDelay);
                StartSummonCooldown(caller, prefab, hero.RespawnTime);
            });

        Debug.Log($"영웅 소환 완료!");
    }
}
