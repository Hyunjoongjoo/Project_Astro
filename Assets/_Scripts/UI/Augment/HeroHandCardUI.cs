using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class HeroHandCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    //private float _currentTimer = 0f;
    //private bool IsCooldown => _currentTimer > 0f;

    public string HeroId => _data != null ? _data.targetId : "";

    public void Setup(AugmentData data, int heroIndex)
    {
        HeroIndex = heroIndex;
        _data = data;
        _iconImg.sprite = data.mainIcon;
        _mainCam = Camera.main;

        //스테이지매니저캐싱
        _stageManager = FindFirstObjectByType<StageManager>();

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
        Debug.Log($"[UI쿨] player:{player.PlayerId} prefab:{prefab}, remaining:{remaining}, total:{total}");
        UpdateCooldownUI(remaining, total);
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
            _cooldownCover.gameObject.SetActive(false);
            _cooldownText.text = string.Empty;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        _iconImg.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //if (IsCooldown) return;
        var runner = HeroSpawner.Instance.Runner;

        if (runner != null && runner.IsRunning)
        {
            float remaining = HeroSpawner.Instance.GetRemainingCooldown(runner.LocalPlayer, GetUnitPrefab());
            if (remaining > 0f)
            {
                transform.position = _originPos;
                _iconImg.color = new Color(1, 1, 1, 1f);
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
                return;
            }

            HeroSpawner.Instance.RPC_SpawnUnit(
                GetUnitPrefab(),
                spawnPos,
                team
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
    }

    public NetworkPrefabRef GetUnitPrefab()
    {
        return _data != null ? _data.heroPrefab : default;
    }

    //3.15
    //아이템갱신용
    public void UpdateEquippedItems(Sprite itemIcon1, Sprite itemIcon2)
    {
        if (_itemSlot1 != null)
        {
            _itemSlot1.sprite = itemIcon1;
            _itemSlot1.gameObject.SetActive(itemIcon1 != null);
        }
        if (_itemSlot2 != null)
        {
            _itemSlot2.sprite = itemIcon2;
            _itemSlot2.gameObject.SetActive(itemIcon2 != null);
        }
    }

    
    //매니저가 스킬 아이콘 리스트를 주면 슬롯에 채워주는 함수
    //3.17 부모 프레임도 같이 출력되도록(기본 비활)
    public void UpdateSkillAugmentIcons(List<Sprite> icons)
    {
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
}
