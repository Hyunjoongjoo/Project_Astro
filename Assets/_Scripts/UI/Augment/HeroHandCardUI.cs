using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class HeroHandCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private StageManager _stageManager;

    [SerializeField] private Image _iconImg;
    [SerializeField] private LayerMask _groundLayer;
    private AugmentData _data;
    private Vector3 _originPos;
    private Camera _mainCam;

    [Header("Cooldown UI")]
    [SerializeField] private Image _cooldownCover;      //쿨타임 오브젝트
    [SerializeField] private TextMeshProUGUI _cooldownText; //텍스트

    //3.15 추가
    public int HeroIndex { get; private set; } //인덱스 표시

    [Header("Skill Augment UI")]
    [SerializeField] private Image[] _skillAugmentImgs;

    //장착템1,2
    [Header("Equipped Items UI")]
    [SerializeField] private Image _itemSlot1;
    [SerializeField] private Image _itemSlot2;

    [Header("상세 정보 설정")]
    [SerializeField] private float _longPressThreshold = 3.0f;
    [SerializeField] private GameObject _detailPrefab;
    [SerializeField] private Image _holdGauge;
    private Coroutine _longPressCoroutine;
    private bool _isPointerDown = false;
    private string _currentItemId1;
    private string _currentItemId2;
    private List<string> _currentAugmentIds = new List<string>();

    [Header("Deploy Zone")]
    [SerializeField] private DeployZone _deployZone;
    private bool _isDragging = false;
    private float _zoneRefreshTimer = 0f;
    [SerializeField] private float _zoneRefreshInterval = 0.1f;

    //private float _currentTimer = 0f;
    //private bool IsCooldown => _currentTimer > 0f;

    public event Action<string> AvailableState;

    public string HeroId => _data != null ? _data.targetId : "";

    public void Setup(AugmentData data, int heroIndex)
    {
        HeroIndex = heroIndex;
        _data = data;
        _iconImg.sprite = data.mainIcon;
        _mainCam = Camera.main;

        //스테이지매니저캐싱
        _stageManager = FindFirstObjectByType<StageManager>();

        if (_deployZone == null)
        {
            _deployZone = FindFirstObjectByType<DeployZone>();
        }
        AvailableState?.Invoke(_data.targetId);
        UpdateCooldownUI(0f, 1f);
    }
    private void Update()
    {
        //쿨타임 중일 때만 매 프레임 타이머 감소
        //if (IsCooldown)
        //{
        //    _currentTimer -= Time.deltaTime;

        //    if (_currentTimer <= 0f)
        //    {
        //        _currentTimer = 0f;
        //    }

        //    // UI 실시간 갱신
        //    UpdateCooldownUI();
        //}
        if (HeroSpawner.Instance == null) return;

        var runner = HeroSpawner.Instance.Runner;
        if (runner == null || !runner.IsRunning) return;

        PlayerRef player = runner.LocalPlayer;
        NetworkPrefabRef prefab = GetUnitPrefab();

        float remaining = HeroSpawner.Instance.GetRemainingCooldown(player, prefab);
        float total = HeroSpawner.Instance.GetTotalCooldown(player, prefab);

        UpdateCooldownUI(remaining, total);
        if (!_isDragging) return;
        if (_deployZone == null) return;
        _zoneRefreshTimer += Time.deltaTime;
        if (_zoneRefreshTimer < _zoneRefreshInterval) return;
        _zoneRefreshTimer = 0f;
        Team team = GameManager.Instance.PlayerTeam;
        List<DeployZoneData> zones = HeroSpawner.Instance.GetAvailableDeployZones(team);
        _deployZone.ShowZones(zones);

    }

    private void OnDestroy()
    {
        AvailableState = null;
    }

    //쿨타임에 맞춰 커버 텍스트 갱신
    private void UpdateCooldownUI(float remaining, float total)
    {
        if (_cooldownCover == null || _cooldownText == null) return;

        //if (remaining > 0f)
        //{
        //    _cooldownCover.gameObject.SetActive(true);

        //    //커버 => 시간이 지날수록 위에서부터 줄어듦
        //    _cooldownCover.fillAmount = remaining / total;

        //    //텍스트는 올림 처리(기획 상엔 없음 근데 맞겠지)
        //    int secondsLeft = Mathf.CeilToInt(remaining);
        //    _cooldownText.text = $"{secondsLeft}초";
        //}
        //else
        //{
        //    // 쿨타임이 끝났으면 UI 비활성화
        //    _cooldownCover.gameObject.SetActive(false);
        //}
        if (remaining > 0f && total > 0f)
        {
            _cooldownCover.gameObject.SetActive(true);
            _cooldownCover.fillAmount = remaining / total;

            int secondsLeft = Mathf.CeilToInt(remaining);
            _cooldownText.text = $"{secondsLeft}초";
        }
        else
        {
            if (_cooldownCover.gameObject.activeSelf)
            {
                AvailableState?.Invoke(_data.targetId);
                _cooldownText.text = string.Empty;
                _cooldownCover.gameObject.SetActive(false);
            }
                
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //드래그중에는 롱 프레스 체크 중단하게
        _isPointerDown = false;
        if (_longPressCoroutine != null)
        {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
        }
        ResetHoldGauge();

        //게임 playing(카운트다운끝) 상태 아니면 드래그 막기
        if (_stageManager != null && _stageManager.CurrentState != StageState.Playing)
        {
            eventData.pointerDrag = null;
            return;
        }
        //쿨타임 중이면 드래그 무시
        //if (IsCooldown)
        //{
        //    eventData.pointerDrag = null;
        //    return;
        //}
        if (HeroSpawner.Instance != null)
        {
            var runner = HeroSpawner.Instance.Runner;
            if (runner != null && runner.IsRunning)
            {
                float remaining = HeroSpawner.Instance.GetRemainingCooldown(runner.LocalPlayer, GetUnitPrefab());
                if (remaining > 0f)
                {
                    eventData.pointerDrag = null;
                    return;
                }
            }
        }
        _originPos = transform.position;
        _isDragging = true;
        _zoneRefreshTimer = 0f;
        _iconImg.color = new Color(1, 1, 1, 0.5f);

        // 드래그 시 배치 가능 영역 표시
        if (_deployZone != null && HeroSpawner.Instance != null)
        {
            Team team = GameManager.Instance.PlayerTeam;
            List<DeployZoneData> zones = HeroSpawner.Instance.GetAvailableDeployZones(team);
            Debug.Log($"[HeroHandCardUI] ShowZones 호출, zones.Count = {zones.Count}");
            _deployZone.ShowZones(zones);
        }

    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //if (IsCooldown) return;
        _isDragging = false;
        _zoneRefreshTimer = 0f;
        var runner = HeroSpawner.Instance.Runner;

        if (runner != null && runner.IsRunning)
        {
            float remaining = HeroSpawner.Instance.GetRemainingCooldown(runner.LocalPlayer, GetUnitPrefab());
            if (remaining > 0f)
            {
                transform.position = _originPos;
                _iconImg.color = new Color(1, 1, 1, 1f);

                if (_deployZone != null)
                {
                    _deployZone.HideZones();
                }

                return;
            }
        }
        _iconImg.color = new Color(1, 1, 1, 1f);

        Ray ray = _mainCam.ScreenPointToRay(eventData.position);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);  //에디터에서 확인용 

        if (Physics.Raycast(ray, out hit, 100f, _groundLayer))
        {
            //Debug.Log($"소환 지점 발견: {hit.point}");
            //Debug.Log($"맞은 오브젝트: {hit.collider.name}");

            //03-16
            Vector3 spawnPos = hit.point;
            Team team = GameManager.Instance.PlayerTeam;

            if (!HeroSpawner.Instance.CanPreviewDeployHero(spawnPos, team))
            {
                //Debug.Log("배치 거리 초과 - 소환 불가");
                transform.position = _originPos;
                if (_deployZone != null)
                {
                    _deployZone.HideZones();
                }
                return;
            }

            HeroStatData myStat = HeroManager.Instance.GetStatus(HeroId);
            if (myStat == null)
            {
                Debug.LogError($"[HeroHandCardUI] GetStatus 실패: {HeroId}");
                transform.position = _originPos;
                if (_deployZone != null)
                {
                    _deployZone.HideZones();
                }
                return;
            }

            HeroStatNetworkData netStat = new HeroStatNetworkData
            {
                MaxHp = myStat.BaseHp,
                AttackPower = myStat.baseAttackPower,
                HealPower = myStat.baseHealingPower
            };

            HeroSpawner.Instance.RPC_SpawnUnit(
                GetUnitPrefab(),
                spawnPos,
                team,
                netStat
            );

            //소환 성공 시 타이머 시작 및 UI 갱신
            //_currentTimer = _data.currentSpawnCooldown;
            //UpdateCooldownUI();
        }
        else
        {
            Debug.LogWarning("소환실패");
        }
        transform.position = _originPos;
        if (_deployZone != null)
        {
            _deployZone.HideZones();
        }
    }

    public NetworkPrefabRef GetUnitPrefab()
    {
        return _data != null ? _data.heroPrefab : default;
    }

    //3.15
    //아이템갱신용
    public void UpdateEquippedItems(string id1, string id2, Sprite icon1, Sprite icon2)
    {
        _currentItemId1 = id1;
        _currentItemId2 = id2;

        if (_itemSlot1 != null)
        {
            _itemSlot1.sprite = icon1;
            _itemSlot1.gameObject.SetActive(icon1 != null);
        }
        if (_itemSlot2 != null)
        {
            _itemSlot2.sprite = icon2;
            _itemSlot2.gameObject.SetActive(icon2 != null);
        }
    }

    //매니저가 스킬 아이콘 리스트를 주면 슬롯에 채워주는 함수
    //3.17 부모 프레임도 같이 출력되도록(기본 비활)
    public void UpdateSkillAugmentIcons(List<string> rawIds, List<Sprite> icons)
    {
        _currentAugmentIds = new List<string>(rawIds);

        if (_skillAugmentImgs == null) return;

        for (int i = 0; i < _skillAugmentImgs.Length; i++)
        {
            if (i < icons.Count && icons[i] != null)
            {
                _skillAugmentImgs[i].sprite = icons[i];
                _skillAugmentImgs[i].gameObject.SetActive(true);

                //부모 프레임도 켬
                _skillAugmentImgs[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                //부모프레임 채로 숨김
                _skillAugmentImgs[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        if (_longPressCoroutine != null) StopCoroutine(_longPressCoroutine);
        _longPressCoroutine = StartCoroutine(CO_CheckLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        if (_longPressCoroutine != null) StopCoroutine(_longPressCoroutine);
        ResetHoldGauge();
    }
    private IEnumerator CO_CheckLongPress()
    {
        float timer = 0f;

        if (_holdGauge != null)
        {
            _holdGauge.fillAmount = 0f;
            _holdGauge.gameObject.SetActive(true);
        }

        while (timer < _longPressThreshold)
        {
            timer += Time.deltaTime;

            if (_holdGauge != null)
            {
                _holdGauge.fillAmount = timer / _longPressThreshold;
            }

            if (!_isPointerDown)
            {
                ResetHoldGauge(); // 떼면 게이지 초기화
                yield break;
            }

            yield return null;
        }

        // 3초 경과 시 팝업 열기
        ResetHoldGauge();
        OpenDetail();
    }
    private void OpenDetail()
    {
        // 1. 프리팹 생성
        var detailObj = UIManager.Instance.ShowUI<HandCardDetail>(_detailPrefab, true);

        if (detailObj != null)
        {
            // 2. 이 함수 호출이 누락되었거나, detailObj가 null이라 실행 안 될 수 있음
            // 여기서 SetupByIds를 반드시 호출해야 로그가 찍힙니다.
            detailObj.SetupByIds(_data, _currentItemId1, _currentItemId2, _currentAugmentIds);
            Debug.Log("SetupByIds 호출 시도함");
        }
        else
        {
            Debug.LogError("상세창 UI 생성 실패!");
        }
    }
    private void ResetHoldGauge()
    {
        if (_holdGauge != null)
        {
            _holdGauge.fillAmount = 0f;
            _holdGauge.gameObject.SetActive(false);
        }
    }

}
