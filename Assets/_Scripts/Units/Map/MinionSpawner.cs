using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class MinionSpawner : NetworkBehaviour
{
    [System.Serializable]
    public struct SpawnLane
    {
        [Tooltip("이 레인의 스폰 위치")]
        public Transform spawnPoint;

        [Tooltip("이 레인 미니언의 최종 목표 (적 함교)")]
        public UnitBase targetBase;

        [Tooltip("이 레인에서 우선 공격할 적 타워 A")]
        public UnitBase enemyTowerA;

        [Tooltip("이 레인에서 우선 공격할 적 타워 B")]
        public UnitBase enemyTowerB;
    }

    [Header("팀 설정")]
    [SerializeField] private Team _team;

    [Header("미니언 프리팹")]
    [SerializeField] private NetworkPrefabRef _minionPrefab;

    [Header("레인 구성")]
    [SerializeField] private SpawnLane[] _lanes;

    [Header("웨이브 설정")]
    [SerializeField] private float _waveInterval = 15f;  // 웨이브 주기 (초)
    [SerializeField] private int _minionsPerLane = 3;    // 레인당 미니언 수
    [SerializeField] private float _spawnDelay = 0.3f; // 미니언 간 스폰 간격 (초)

    [Networked] public int CurrentWave { get; private set; }

    [Networked] public bool IsActive { get; private set; }

    private TickTimer _waveTimer;

    private readonly Queue<(int laneIndex, int minionIndex)> _spawnQueue
        = new Queue<(int, int)>();

    private TickTimer _spawnDelayTimer;

    public override void Spawned()
    {
        // InputAuthority(소유 플레이어)만 스폰 로직 초기화
        if (!Object.HasInputAuthority) return;

        IsActive = true;
        CurrentWave = 0;

        // 첫 웨이브는 게임 시작과 동시에 바로 나오도록 0초로 설정
        _waveTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, 0f);
    }

    public override void FixedUpdateNetwork()
    {
        // Shared Mode: 소유 플레이어(InputAuthority)만 실행
        if (!Object.HasInputAuthority) return;
        if (!IsActive) return;

        // 큐에 스폰 대기 중인 미니언이 있으면 간격을 두고 한 마리씩 처리
        if (_spawnQueue.Count > 0)
        {
            ProcessSpawnQueue();
            return; // 스폰 중에는 웨이브 타이머 체크 생략 (동시 진행 원하면 return 제거)
        }

        // 웨이브 타이머 만료 → 다음 웨이브 큐 적재
        if (_waveTimer.ExpiredOrNotRunning(Runner))
        {
            EnqueueNextWave();
            _waveTimer = TickTimer.CreateFromSeconds(Runner, _waveInterval);
        }
    }

    private void EnqueueNextWave()
    {
        if (_lanes == null || _lanes.Length == 0)
        {
            Debug.LogWarning($"[MinionSpawner] {_team} 팀: 레인이 설정되지 않았습니다.");
            return;
        }

        CurrentWave++;

        for (int minionIdx = 0; minionIdx < _minionsPerLane; minionIdx++)
        {
            for (int laneIdx = 0; laneIdx < _lanes.Length; laneIdx++)
            {
                _spawnQueue.Enqueue((laneIdx, minionIdx));
            }
        }

        // 큐 처리 타이머 즉시 시작
        _spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, 0f);

        Debug.Log($"[MinionSpawner] {_team} 팀 웨이브 {CurrentWave} 적재 완료 " +
                  $"({_lanes.Length}레인 × {_minionsPerLane}마리 = {_spawnQueue.Count}마리)");
    }

    private void ProcessSpawnQueue()
    {
        if (!_spawnDelayTimer.ExpiredOrNotRunning(Runner)) return;

        var (laneIdx, _) = _spawnQueue.Dequeue();
        SpawnMinion(laneIdx);

        // 다음 미니언 스폰까지 대기
        _spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, _spawnDelay);
    }

    private void SpawnMinion(int laneIdx)
    {
        SpawnLane lane = _lanes[laneIdx];

        if (lane.spawnPoint == null)
        {
            Debug.LogWarning($"[MinionSpawner] 레인 {laneIdx}의 spawnPoint가 null입니다.");
            return;
        }

        // 스폰 위치에 약간의 랜덤 오프셋으로 겹침 방지
        Vector3 spawnPos = lane.spawnPoint.position;

        NetworkObject networkObj = Runner.Spawn(
            _minionPrefab,
            spawnPos,
            lane.spawnPoint.rotation,
            Object.InputAuthority,           // Shared Mode: 소유자가 StateAuthority 유지
            onBeforeSpawned: (runner, obj) =>
            {
                // Spawned() 호출 전 Setup 주입 → Spawned에서 올바른 값으로 초기화됨
                if (obj.TryGetComponent<MinionController>(out var minion))
                {
                    minion.Setup(lane.enemyTowerA, lane.enemyTowerB, lane.targetBase);
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
        Debug.Log($"[MinionSpawner] {_team} 팀 스포너 중단 (함교 파괴)");
    }

    public void SetMinionsPerLane(int count)
    {
        if (!Object.HasInputAuthority) return;
        _minionsPerLane = Mathf.Max(1, count);
    }
}
