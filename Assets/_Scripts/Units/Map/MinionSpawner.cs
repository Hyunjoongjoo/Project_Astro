using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionSpawner : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private Team _team;
    [SerializeField] private string _minionTag = "Minion";
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _enemyBase;

    [Header("스폰 설정")]
    [SerializeField] private int _baseSpawnCount = 3;
    [SerializeField] private int _bonusSpawnCount = 3;
    [SerializeField] private float _spawnInterval = 15f;

    [Header("파괴타워 감지용 리스트")]
    [SerializeField] private List<Tower> _towers;

    private int _destroyedTowerCount = 0;
    private Coroutine _minionSpawnCor;

    private void Start()
    {
        //타워 파괴 이벤트 구독 및 초기화
        _destroyedTowerCount = 0;
        foreach (var tower in _towers)
        {
            if(tower != null)
            {
                tower.OnTowerDestroyed += () =>
                {
                    _destroyedTowerCount++;
                };
            }
        }

        _minionSpawnCor = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
         yield return new WaitUntil(() => GameManager.Instance.IsGameStarted); //게임 시작전 대기

        while (true)
        {
            SpawnMinions();
            yield return CoroutineManager.waitForSeconds(_spawnInterval);
        }
    }

    private void SpawnMinions()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0) return;

        int totalToSpawn = _baseSpawnCount + (_destroyedTowerCount * _bonusSpawnCount);

        foreach (var targetPoint in _spawnPoints)
        {
            for (int i = 0; i < totalToSpawn; i++)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-0.05f, 0.05f), 0, Random.Range(-0.05f, 0.05f));

                GameObject minionObj = PoolManager.Instance.SpawnFromPool(
                    _minionTag,
                    targetPoint.position + randomOffset,
                    Quaternion.identity
                    );

                if (minionObj.TryGetComponent(out MinionAI ai))
                {
                    ai.Setup(_team, _enemyBase);
                }
            }
        }
    }
}
