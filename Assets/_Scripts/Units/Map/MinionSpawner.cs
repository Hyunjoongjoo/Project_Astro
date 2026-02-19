using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class MinionSpawner : NetworkBehaviour
{
    [Header("미니언 프리팹")]
    [SerializeField] private NetworkPrefabRef _blueMinionPrefab;
    [SerializeField] private NetworkPrefabRef _redMinionPrefab;

    [Header("각 진영 스폰 포인트")]
    [SerializeField] private Transform[] _blueLanes;
    [SerializeField] private Transform[] _redLanes;

    [Header("웨이브 설정")]
    [SerializeField] private float _waveInterval = 15f;  // 웨이브 주기 (초)
    [SerializeField] private int _minionsPerLane = 3;    // 레인당 미니언 수
    [SerializeField] private float _spawnDelay = 0.3f; // 미니언 간 스폰 간격 (초)

    [Networked] public int CurrentWave { get; private set; }

    [Networked] public bool IsActive { get; private set; }

    private TickTimer _waveTimer;

    private readonly Queue<(Team team, int laneIndex, int minionIndex)> _spawnQueue
        = new Queue<(Team, int, int)>();

    private TickTimer _spawnDelayTimer;


    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        IsActive = true;
        CurrentWave = 0;

        // 첫 웨이브는 게임 시작과 동시에 바로 나오도록 0초로 설정
        _waveTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, 0f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsActive) return;

        // 큐에 스폰 대기 중인 미니언이 있으면 간격을 두고 한 마리씩 처리
        if (_spawnQueue.Count > 0)
        {
            ProcessSpawnQueue();
            return; // 스폰 중에는 웨이브 타이머 체크 생략
        }

        // 웨이브 타이머 만료 시 다음 웨이브 큐 적재
        if (_waveTimer.ExpiredOrNotRunning(Runner))
        {
            EnqueueNextWave();
            _waveTimer = TickTimer.CreateFromSeconds(Runner, _waveInterval);
        }
    }

    private void EnqueueNextWave()
    {
        CurrentWave++;

        int maxLanes = Mathf.Max(
            _blueLanes != null ? _blueLanes.Length : 0,
            _redLanes != null ? _redLanes.Length : 0
        );

        if (maxLanes == 0)
        {
            Debug.LogWarning("[MinionSpawner] 레인이 설정되지 않았습니다.");
            return;
        }

        // 큐에 순차적으로 담음
        for (int minionIdx = 0; minionIdx < _minionsPerLane; minionIdx++)
        {
            for (int laneIdx = 0; laneIdx < maxLanes; laneIdx++)
            {
                if (_blueLanes != null && laneIdx < _blueLanes.Length)
                    _spawnQueue.Enqueue((Team.Blue, laneIdx, minionIdx));

                if (_redLanes != null && laneIdx < _redLanes.Length)
                    _spawnQueue.Enqueue((Team.Red, laneIdx, minionIdx));
            }
        }

        // 큐 처리 타이머 즉시 시작
        _spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, 0f);
    }

    private void ProcessSpawnQueue()
    {
        if (!_spawnDelayTimer.ExpiredOrNotRunning(Runner)) return;

        var (team, laneIdx, _) = _spawnQueue.Dequeue();
        SpawnMinion(team, laneIdx);

        _spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, _spawnDelay);
    }

    private void SpawnMinion(Team team, int laneIdx)
    {
        NetworkPrefabRef prefab;
        Transform spawnTransform;

        if (team == Team.Blue)
        {
            prefab = _blueMinionPrefab;
            spawnTransform = _blueLanes[laneIdx];
        }
        else
        {
            prefab = _redMinionPrefab;
            spawnTransform = _redLanes[laneIdx];
        }

        if (spawnTransform == null)
        {
            Debug.LogWarning($"[MinionSpawner] 레인 {laneIdx}의 spawnPoint가 null입니다.");
            return;
        }

        // 스폰 위치
        Vector3 spawnPos = spawnTransform.position;

        NetworkObject networkObj = Runner.Spawn(
            prefab,
            spawnPos,
            spawnTransform.rotation,
            inputAuthority: null,
            onBeforeSpawned: (runner, obj) =>
            {
                // Spawned() 호출 전 Setup 호출하여 초기화
                if (obj.TryGetComponent<MinionController>(out var minion))
                {
                    minion.Setup(team);
                }
            }
        );

        if (networkObj == null)
        {
            Debug.LogError($"[MinionSpawner] 레인 {laneIdx} 미니언 스폰 실패");
        }
    }

    public void StopSpawning()
    {
        if (!Object.HasInputAuthority) return;

        IsActive = false;
        _spawnQueue.Clear();
        Debug.Log($"[MinionSpawner] 스포너 중단 (함교 파괴)");
    }

    public void SetMinionsPerLane(int count)
    {
        if (!Object.HasInputAuthority) return;
        _minionsPerLane = Mathf.Max(1, count);
    }
}
