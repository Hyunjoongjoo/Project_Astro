using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroHandCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _iconImg;
    [SerializeField] private LayerMask _groundLayer;
    private AugmentData _data;
    private Vector3 _originPos;
    private Camera _mainCam;

    [Header("Cooldown UI")]
    [SerializeField] private Image _cooldownCover;      //쿨타임 오브젝트
    [SerializeField] private TextMeshProUGUI _cooldownText; //텍스트

    private float _currentTimer = 0f;
    private bool IsCooldown => _currentTimer > 0f;

    public void Setup(AugmentData data)
    {
        _data = data;
        _iconImg.sprite = data.icon;
        _mainCam = Camera.main;

        UpdateCooldownUI();
    }
    private void Update()
    {
        //쿨타임 중일 때만 매 프레임 타이머 감소
        if (IsCooldown)
        {
            _currentTimer -= Time.deltaTime;

            if (_currentTimer <= 0f)
            {
                _currentTimer = 0f;
            }

            // UI 실시간 갱신
            UpdateCooldownUI();
        }
    }

    //쿨타임에 맞춰 커버 텍스트 갱신
    private void UpdateCooldownUI()
    {
        if (_cooldownCover == null || _cooldownText == null) return;

        if (IsCooldown)
        {
            _cooldownCover.gameObject.SetActive(true);

            //커버 => 시간이 지날수록 위에서부터 줄어듦
            _cooldownCover.fillAmount = _currentTimer / _data.currentSpawnCooldown;

            //텍스트는 올림 처리(기획 상엔 없음 근데 맞겠지)
            int secondsLeft = Mathf.CeilToInt(_currentTimer);
            _cooldownText.text = $"{secondsLeft}초";
        }
        else
        {
            // 쿨타임이 끝났으면 UI 비활성화
            _cooldownCover.gameObject.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //쿨타임 중이면 드래그 무시
        if (IsCooldown)
        {
            eventData.pointerDrag = null;
            return;
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
        if (IsCooldown) return;
        _iconImg.color = new Color(1, 1, 1, 1f);

        Ray ray = _mainCam.ScreenPointToRay(eventData.position);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);  //에디터에서 확인용 

        if (Physics.Raycast(ray, out hit, 100f, _groundLayer))
        {
            Debug.Log($"소환 지점 발견: {hit.point}");
            Debug.Log($"맞은 오브젝트: {hit.collider.name}");

            HeroSpawner.Instance.RPC_SpawnUnit(
                GetUnitPrefab(),
                hit.point,
                GameManager.Instance.PlayerTeam
            );
            //소환 성공 시 타이머 시작 및 UI 갱신
            _currentTimer = _data.currentSpawnCooldown;
            UpdateCooldownUI();
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
}
