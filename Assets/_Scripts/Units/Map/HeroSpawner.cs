using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct CooldownData : INetworkStruct
{
    public float EndTime;
    public float TotalDuration;
}

public class HeroSpawner : NetworkBehaviour
{
    public static HeroSpawner Instance { get; private set; }

    [Header("배치 설정")]
    [SerializeField] private float _minDeployTime = 0.25f;
    [SerializeField] private float _maxDeployTime = 2.5f;
    [SerializeField] private float _minDeployDistance = 1f;

    [Header("배치 거리 확장")]
    [SerializeField] private float _baseDeployDistance = 8f;
    [SerializeField] private float _deployExpandPerTower = 3f;
    [SerializeField] private float _maxDeployDistance = 14f;

    [SerializeField] private List<NetworkPrefabRef> _prefabs;

    [Networked, Capacity(32)]
    private NetworkDictionary<int, CooldownData> CooldownMap => default;

    // 플레이어,영웅 프리팹 기준으로 소환 쿨다운을 분리 관리
  //  private readonly Dictionary<(PlayerRef, int), float> _endTimes
  //= new Dictionary<(PlayerRef, int), float>();

  //  private readonly Dictionary<(PlayerRef, int), float> _cooldownDurations
  //  = new Dictionary<(PlayerRef, int), float>();

    public override void Spawned()
    {
        Instance = this;
    }
    private int GetPrefabId(NetworkPrefabRef prefab)
    {
        //return _prefabs.IndexOf(prefab);
        int index = _prefabs.IndexOf(prefab);

        if (index < 0)
        {
            Debug.LogError($"[HeroSpawner] prefabId 못찾음: {prefab}");
            return 0;
        }

        return index;
    }

    private int GetKey(PlayerRef player, int prefabId)
    {
        //return player.PlayerId * 100 + prefabId;
        return (player.PlayerId << 16) | prefabId;
    }
    //private int GetPrefabId(NetworkPrefabRef prefab)
    //{
    //    return prefab.GetHashCode();
    //}

    //private bool CanSummon(PlayerRef player, NetworkPrefabRef prefab)//소환 가능한지 여부
    //{
    //    int prefabId = GetPrefabId(prefab);
    //    var key = (player, prefabId);

    //    if (!_endTimes.TryGetValue(key, out float endTime))
    //    {
    //        return true;
    //    }

    //    return Runner.SimulationTime >= endTime;
    //}
    private bool CanSummon(PlayerRef player, NetworkPrefabRef prefab)
    {
        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        if (!CooldownMap.TryGet(key, out var data))
            return true;

        return Runner.SimulationTime >= data.EndTime;
    }

    private int GetDestroyedTowerCount(Team team)//현재 파괴된 포탑 수
    {
        int aliveCount = 0;

        Team enemyTeam = team == Team.Blue ? Team.Red : Team.Blue;

        foreach (var tower in Tower.AliveTowers)
        {
            if (tower.networkedTeam == enemyTeam)
            {
                aliveCount++;
            }
        }

        return Mathf.Clamp(2 - aliveCount, 0, 2);//팀당 포탑 2개 기준
    }
    
    private float GetCurrentDeployDistance(Team team)//현재 배치 가능 거리 계산
    {
        int destroyedCount = GetDestroyedTowerCount(team);

        float distance = _baseDeployDistance + destroyedCount * _deployExpandPerTower;

        return Mathf.Min(distance, _maxDeployDistance);
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

        //Vector3 dir = spawnPos - deployOrigin.position;//이부분은 이야기가 나올시...

        ////함교 뒤쪽 배치 방지
        //if (team == Team.Blue && dir.z < 0)
        //{
        //    Debug.Log($"함교보다 뒤에 배치됨");
        //    return false;
        //}

        //if (team == Team.Red && dir.z > 0)
        //{
        //    Debug.Log($"함교보다 뒤에 배치됨");
        //    return false;
        //}

        //최대 배치 거리 초과 시 차단
        float distance = Vector3.Distance(deployOrigin.position, spawnPos);
        float maxDistance = GetCurrentDeployDistance(team);//현재 남은 포탑 기반 배치 거리
        return distance <= maxDistance;
    }

    public bool CanPreviewDeployHero(Vector3 spawnPos, Team team)//UI체크
    {
        Transform origin = GetDeployOrigin(team);

        if (origin == null)
        {
            return false;
        }

        //Vector3 dir = spawnPos - origin.position;//이부분은 이야기가 나올시...

        ////함교 뒤쪽 배치 방지
        //if (team == Team.Blue && dir.z < 0)
        //{
        //    return false;
        //}

        //if (team == Team.Red && dir.z > 0)
        //{
        //    return false;
        //}

        float distance = Vector3.Distance(origin.position, spawnPos);
        float maxDistance = GetCurrentDeployDistance(team);//현재 남은 포탑 기반 배치 거리
        return distance <= maxDistance;
    }

    private Transform GetDeployOrigin(Team team)//함교 위치를 반환
    {
        UnitBase[] myStructures = team == Team.Blue
            ? ObjectContainer.Instance.blueSideStructure
            : ObjectContainer.Instance.redSideStructure;

        if (myStructures == null || myStructures.Length <= 2)
        {
            return null;
        }

        if (myStructures[2] == null)
        {
            return null;
        }

        return myStructures[2].transform;
    }

    private float GetDeployDelay(float distance)//거리 기반 배치 지연시간
    {
        float time = Mathf.InverseLerp(_minDeployDistance, _maxDeployDistance, distance);
        return Mathf.Lerp(_minDeployTime, _maxDeployTime, time);
    }

    //public void StartSummonCooldown(PlayerRef player, NetworkPrefabRef prefab, float cooldown)
    //{
    //    int prefabId = GetPrefabId(prefab);
    //    float endTime = Runner.SimulationTime + cooldown;
    //    var key = (player, prefabId);
    //    _endTimes[key] = endTime;
    //    _cooldownDurations[key] = cooldown;

    //    RPC_SyncCooldown(player, prefabId, endTime, cooldown);
    //    Debug.Log($"[쿨타임 시작] player:{player}, prefab:{prefab}, cd:{cooldown}");
    //}
    public void StartSummonCooldown(PlayerRef player, NetworkPrefabRef prefab, float cooldown)
    {
        if (!Object.HasStateAuthority) return;

        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        CooldownMap.Set(key, new CooldownData
        {
            EndTime = Runner.SimulationTime + cooldown,
            TotalDuration = cooldown
        });

        Debug.Log($"[쿨타임 시작] player:{player.PlayerId}, prefabId:{prefabId}, cd:{cooldown}");
    }

    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    //private void RPC_SyncCooldown(PlayerRef player, int prefabId, float endTime, float total)
    //{
    //    var key = (player, prefabId);

    //    _endTimes[key] = endTime;
    //    _cooldownDurations[key] = total;
    //}

    //UI에서 사용할수있도록 메서드로 지정한 플레이어와 프리팹에 대한 남은 소환 쿨타임을 반환
    //public float GetRemainingCooldown(PlayerRef player, NetworkPrefabRef prefab)
    //{
    //    //Runner가 아직 준비되지 않았으면 쿨 없음으로 처리
    //    if (Runner == null || !Runner.IsRunning)
    //    {
    //        return 0f;
    //    }
    //    int prefabId = GetPrefabId(prefab);
    //    var key = (player, prefabId);

    //    if (!_endTimes.TryGetValue(key, out float endTime))
    //    {
    //        return 0f;
    //    }

    //    float remaining = endTime - Runner.SimulationTime;

    //    return Mathf.Max(remaining, 0f);
    //}
    public float GetRemainingCooldown(PlayerRef player, NetworkPrefabRef prefab)
    {
        if (Runner == null || !Runner.IsRunning)
            return 0f;

        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        if (CooldownMap.TryGet(key, out var data))
        {
            float remaining = data.EndTime - Runner.SimulationTime;
            return Mathf.Max(remaining, 0f);
        }

        return 0f;
    }
    //public float GetTotalCooldown(PlayerRef player, NetworkPrefabRef prefab)
    //{
    //    int prefabId = GetPrefabId(prefab);
    //    var key = (player, prefabId);

    //    if (_cooldownDurations.TryGetValue(key, out float total))
    //    {
    //        return total;
    //    }

    //    return 0f;
    //}
    public float GetTotalCooldown(PlayerRef player, NetworkPrefabRef prefab)
    {
        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        if (CooldownMap.TryGet(key, out var data))
        {
            return data.TotalDuration;
        }

        return 0f;
    }



    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team, RpcInfo info = default)
    {
        if (prefab == default) return;

        PlayerRef caller = info.Source;

        if (!CanSummon(caller, prefab)) return;
        if (!CanDeployHero(spawnPos, team)) return;

        Transform origin = GetDeployOrigin(team);
        if (origin == null) return;

        float distance = Vector3.Distance(origin.position, spawnPos);
        float deployDelay = GetDeployDelay(distance);

        //영웅 초기 방향 결정
        Vector3 forwardDir = team == Team.Blue ? Vector3.forward : Vector3.back;
        Quaternion spawnRot = Quaternion.LookRotation(forwardDir);

        Runner.Spawn(prefab, origin.position, spawnRot, caller,
         onBeforeSpawned: (runner, obj) =>
         {
             HeroController hero = obj.GetComponent<HeroController>();
             hero.Setup(team, spawnPos, deployDelay, prefab, caller);
         });
    }

    //private float GetFinalCooldown(PlayerRef player, NetworkPrefabRef prefab)
    //{
    //    // 기본 쿨
    //    HeroStatData data = HeroManager.Instance.GetStatusByPrefab(prefab);

    //    float baseCooldown = data.spawnCooldown;

    //    // TODO: 나중에 플레이어 쿨감 적용
    //    float cooldownReduction = 0f;

    //    float final = baseCooldown * (1f - cooldownReduction);

    //    return Mathf.Max(final, 0.1f);
    //}

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Transform origin = GetDeployOrigin(Team.Blue);
        if (origin == null) return;

        float distance = GetCurrentDeployDistance(Team.Blue);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin.position, distance);
    }
#endif
}
