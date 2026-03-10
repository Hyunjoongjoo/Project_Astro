using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//3.3 여현구
//증강 관련 UI연출, 클라이언트 조작만 담당하도록 분리.

public class AugmentManager : Singleton<AugmentManager>
{
    [Header("증강선택 UI")]
    [SerializeField] private GameObject _augmentWindowPrefab;
    [SerializeField] private Button _toggleBtn;

    [Header("하단 영웅 카드 인벤토리")]
    [SerializeField] private GameObject _heroCardSlotPrefab; //히어로 핸드 카드 붙은 프리팹
    [SerializeField] Transform _slotContainer;

    private StageManager _cachedStageManager;

    //3.9 열려있는 윈도우 캐싱용 변수 추가
    private AugmentWindowUI _currentWindow;

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

    public void ShowAugmentWindow(List<AugmentData> myDatas, List<AugmentData> teamDatas = null, string teamName = "")
    {
        //UIManager를 통해 팝업 형식으로 띄움
        _currentWindow = UIManager.Instance.ShowUI<AugmentWindowUI>(_augmentWindowPrefab, true);
        _toggleBtn.gameObject.SetActive(true);
        if (_currentWindow != null)
        {
            _currentWindow.SetupAndOpen(myDatas, teamDatas, teamName);

            _toggleBtn.onClick.RemoveAllListeners();
            _toggleBtn.onClick.AddListener(() =>
            {
                if (_currentWindow != null)
                {
                    _currentWindow.Toggle();
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

    //3.9 타임아웃 시 실행될 강제 픽 함수 추가
    public void ForceRandomPick()
    {
        //창이 아직 열려있다면 (유저가 아직 카드를 안 골랐다면)
        if (_currentWindow != null && _currentWindow.gameObject.activeInHierarchy)
        {
            _currentWindow.ForceRandomPick();
        }
    }
}
