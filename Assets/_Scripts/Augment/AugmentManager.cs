using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AugmentManager : Singleton<AugmentManager>
{
    [Header("증강 SO")]
    [SerializeField] private AugmentDataSO _masterSO;
    private AugmentDeck _deck;

    [Header("증강선택 UI")]
    [SerializeField] private GameObject _augmentWindowPrefab;
    [SerializeField] private Button _toggleBtn;

    [Header("하단 영웅 카드 인벤토리")]
    [SerializeField] private GameObject _heroCardSlotPrefab; //히어로 핸드 카드 붙은 프리팹
    [SerializeField] Transform _slotContainer;

    private StageManager _cachedStageManager;

    protected override void Awake()
    {
        base.Awake();
        if(_masterSO != null)
        {
            _deck = new AugmentDeck(_masterSO);
        }
    }

    // 2026-03-03 윤혁 수정 : 증강 여닫는 버튼만 보여주고 숨기기 (Show, Hide)
    public void ShowAugmentToggleBtn()
    {
        if (_toggleBtn != null && _toggleBtn.gameObject.activeSelf == false)
        {
            _toggleBtn.onClick.RemoveAllListeners();
            _toggleBtn.onClick.AddListener(() =>
            {
                var options = GetRandomAugments(AugmentType.Hero, 3);
                ShowAugmentWindow(options);
            });
            _toggleBtn.gameObject.SetActive(true);
        }  
    }

    public void HideAugmentToggleBtn()
    {
        if (_toggleBtn.gameObject.activeSelf)
            _toggleBtn.gameObject.SetActive(false);
    }

    // 3장의 랜덤 카드 추출
    public List<AugmentData> GetRandomAugments(AugmentType type, int count = 3)
    {
        List<AugmentData> result = new List<AugmentData>();
        List<string> exclude = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var card = _deck.GetRandomCard(type, exclude);
            if (card != null)
            {
                result.Add(card);
                exclude.Add(card.id);
            }
        }
        return result;
    }
    public void ShowAugmentWindow(List<AugmentData> datas)
    {
        // UIManager를 통해 팝업 형식으로 띄움
        var window = UIManager.Instance.ShowUI<AugmentWindowUI>(_augmentWindowPrefab, true);
        _toggleBtn.gameObject.SetActive(true);
        if (window != null)
        {
            window.SetupAndOpen(datas);

            _toggleBtn.onClick.RemoveAllListeners();
            _toggleBtn.onClick.AddListener(() => window.Toggle());
        }
    }
    // 카드 선택 처리
    public void SelectAugment(AugmentData data)
    {
        Debug.Log($"[Augment] 선택됨: {data.name} ({data.type})");

        if (data.type == AugmentType.Hero)
        {
            AddHeroCard(data);
        }
        else if (data.type == AugmentType.Skill)
        {
            // 스킬 강화 로직
        }

        //토글버튼 비활성화, 리스너 정리
        if (_toggleBtn != null)
        {
            _toggleBtn.gameObject.SetActive(false);
            _toggleBtn.onClick.RemoveAllListeners(); 
        }

        UIManager.Instance.CloseTopPopup();

        // 2026-03-03 윤혁 수정 : 카드 선택 완료 인게임 플레이 중일 때 처리 로직 분리
        // 선택 완료 후 게임 상태에 따른 처리
        if (_cachedStageManager == null)
            _cachedStageManager = FindFirstObjectByType<StageManager>();

        if (_cachedStageManager != null)
        {
            // 전투 시작 전 최초 증강 선택 상태일 땐 RPC를 쏜다.
            if (_cachedStageManager.CurrentState == StageState.AugmentSelection)
                _cachedStageManager.RPC_ReportAugmentComplete(_cachedStageManager.Runner.LocalPlayer);

            else // 그 외 증강 선택은 인게임 플레이 중일 때.
            {
                _cachedStageManager.DecreaseAugmentGauge(
                    GameManager.Instance.PlayerTeam, 100);
            }
        }
    }

    //하단 UI패널에 영웅 카드 추가
    public void AddHeroCard(AugmentData data)
    {
        var go = Instantiate(_heroCardSlotPrefab, _slotContainer);
        if (go.TryGetComponent(out HeroHandCardUI card))
        {
            card.Setup(data);
        }
    }
}
