using System.Collections.Generic;
using UnityEngine;

public class AugmentManager : Singleton<AugmentManager>
{
    [Header("증강 SO")]
    [SerializeField] private AugmentDataSO _masterSO;
    private AugmentDeck _deck;

    [Header("증강선택 UI")]
    [SerializeField] private GameObject _augmentWindowPrefab;

    [Header("하단 영웅 카드 인벤토리")]
    [SerializeField] private GameObject _heroCardSlotPrefab; //히어로 핸드 카드 붙은 프리팹
    [SerializeField] Transform _slotContainer;

    protected override void Awake()
    {
        base.Awake();
        if(_masterSO != null)
        {
            _deck = new AugmentDeck(_masterSO);
        }
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
        if (window != null)
        {
            window.SetupAndOpen(datas);
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

        UIManager.Instance.CloseTopPopup();

        // 선택 완료 후 게임 상태 전환
        GameManager.Instance.OnAugmentSelectionComplete();
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
