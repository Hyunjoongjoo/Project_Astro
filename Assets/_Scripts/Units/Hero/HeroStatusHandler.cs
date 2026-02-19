using System;
using UnityEngine;

/*
    각 유닛에 붙여주는 핸들러로써, 각 유닛의 데이터인 CSV의 베이스 데이터를 기반으로 레벨당 스텟을 계산하는 클래스
    +) Level, Exp, isUnlock 데이터가 변동되면 바로 DB에 갱신해주는 기능
    +) 계산된 데이터들을 복제(스냅샷 = HeroStatus)하여 다른곳에서 런타임 스텟을 사용할 수 있도록 처리
 */

public class HeroStatusHandler : MonoBehaviour
{
    [SerializeField] private string _heroId;

    //실시간 캐싱된 데이터값들
    private int _level;
    private int _exp;
    private bool _isUnlock;
    private HeroStatus _currentHeroStatus;
    private HeroData _baseHeroData;

    // 프로퍼티
    public string HeroId => _heroId;
    public HeroStatus CurrentHeroStatus => _currentHeroStatus;
    
    public int Level
    {
        get => _level;
        set
        {
            if (_level == value) return;
            _level = value;
            RefreshData();    //레벨은 스텟도 바뀌니깐 여기서 한번 리프레쉬 해줍니다
            NotifyDataChanged();
        }
    }

    public int Exp
    {
        get => _exp;
        set
        {
            if (_exp == value) return;
            _exp = value;
            NotifyDataChanged();
        }
    }

    public bool IsUnlock
    {
        get => _isUnlock;
        set
        {
            if (_isUnlock == value) return;
            _isUnlock = value;
            NotifyDataChanged();
        }
    }

    // 이벤트
    public event Action<string, int, int, bool> OnHeroDataChanged;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(_heroId))
        {
            HeroManager.Instance.RegisterHeroHandler(_heroId, this);
        }
        else
        {
            Debug.LogError($"[HeroStatusHandler] {gameObject.name}에 HeroData가 할당되지 않았습니다.");
        }
    }

    public void Initialize(int level, int exp, bool isUnlock)
    {
        _level = level;
        _exp = exp;
        _isUnlock = isUnlock;

        _baseHeroData = TableManager.Instance.HeroTable.Get(_heroId);

        if (_baseHeroData != null)
        {
            RefreshData();
        }
    }

    // 레벨에 따른 Status변화 
    private void RefreshData()
    {
        if (_baseHeroData == null) return;

        //_currentHeroStatus = new HeroStatus();    <-    CSV 베이스로 되어있는 해당 유닛의 기본 스텟 가져오기.

        ApplyLevelStatus(_currentHeroStatus, _level);
    }

    private void ApplyLevelStatus(HeroStatus status, int level)
    {
        // 레벨업 시 상승 로직 (간이식 집어넣은 상태임다)

        float growthRate = 0.1f * (level - 1);
        status.Hp += status.Hp * growthRate;

        // TODO
        // CSV 파서 도입 시 _baseHeroData.HpGrowth 등을 활용하도록 수정
    }

    // DB에 데이터 갱신 요청하는 메서드
    private void NotifyDataChanged()
    {
        OnHeroDataChanged?.Invoke(_heroId, _level, _exp, _isUnlock);
    }
}
