using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//3.3 여현구
//증강 관련 UI연출, 클라이언트 조작만 담당하도록 분리.

public class AugmentManager : Singleton<AugmentManager>
{
    //3.3 구버전 데이터SO및 Deck 컨트롤러가 담당
    //[Header("증강 SO")]
    //[SerializeField] private AugmentDataSO _masterSO;
    //private AugmentDeck _deck;

    [Header("증강선택 UI")]
    [SerializeField] private GameObject _augmentWindowPrefab;
    [SerializeField] private Button _toggleBtn;

    [Header("하단 영웅 카드 인벤토리")]
    [SerializeField] private GameObject _heroCardSlotPrefab; //히어로 핸드 카드 붙은 프리팹
    [SerializeField] Transform _slotContainer;

    private StageManager _cachedStageManager;

    private void Start()
    {
        //토글버튼 최상단 컨테이너로 가게
        if (UIManager.Instance.TopContainer != null && _toggleBtn != null)
        {
            _toggleBtn.transform.SetParent(UIManager.Instance.TopContainer);
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
                AugmentController.Instance.OpenAugmentWindow();
                HideAugmentToggleBtn();
            });
            _toggleBtn.gameObject.SetActive(true);
        }  
    }

    public void HideAugmentToggleBtn()
    {
        if (_toggleBtn.gameObject.activeSelf)
            _toggleBtn.gameObject.SetActive(false);
    }

    public void ShowAugmentWindow(List<AugmentData> datas)
    {
        //UIManager를 통해 팝업 형식으로 띄움
        var window = UIManager.Instance.ShowUI<AugmentWindowUI>(_augmentWindowPrefab, true);
        _toggleBtn.gameObject.SetActive(true);
        if (window != null)
        {
            window.SetupAndOpen(datas);

            _toggleBtn.onClick.RemoveAllListeners();
            _toggleBtn.onClick.AddListener(() =>
            {
                if (window != null)
                {
                    window.Toggle();
                }
                else
                {
                    HideAugmentToggleBtn();
                }
            });

        }
    }

    //카드 선택 시 호출
    public void SelectAugment(AugmentData data)
    {
        Debug.Log($"유저가 카드 선택함: {data.titleName}");

        //서버에 장착 승인 요청
        AugmentController.Instance.SelectAugment(data);
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
