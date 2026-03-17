using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

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

    //3.12 서버의 응답을 기다리고있는지 체크하는 변수
    private bool _isWaitingForServerResponse = false;

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
                //서버에 패킷 쏘기 전에 기다린다고 표시
                _isWaitingForServerResponse = true;
                ExecuteHideToggleBtn();
                AugmentController.Instance.RPC_RequestAugmentCards(AugmentController.Instance.Runner.LocalPlayer);
            });
            _toggleBtn.gameObject.SetActive(true);
        }
    }

    //3.13 변경 (내가 아직 확정을 내리지 않았다면 외부의 숨김 요청을  return)
    public void HideAugmentToggleBtn()
    {
        if (_currentWindow != null && !_currentWindow.IsForcePicked)
        {
            return;
        }
        ExecuteHideToggleBtn();
    }

    //3.13 추가
    private void ExecuteHideToggleBtn()
    {
        if (_toggleBtn != null && _toggleBtn.gameObject.activeSelf)
        {
            _toggleBtn.gameObject.SetActive(false);
        }
    }


    //3.12 리팩토링
    //매개변수에 isForcedOpen 추가
    public void ShowAugmentWindow(List<AugmentData> myDatas, List<AugmentData> teamDatas = null, string teamName = "", bool isForcedOpen = true)
    {
        //강제로 열거나 내가 버튼을 눌러서 기다리던 중일때만 열기
        bool finalOpen = isForcedOpen || _isWaitingForServerResponse;
        _isWaitingForServerResponse = false; //됐으면 초기화

        //새 창을 열기 전 이전 라운드의 창이 남아있으면 닫아줌
        if (_currentWindow != null)
        {
            _currentWindow.Close();
            _currentWindow = null;
        }

        //UIManager를 통해 팝업 형식으로 띄움
        _currentWindow = UIManager.Instance.ShowUI<AugmentWindowUI>(_augmentWindowPrefab, true);
        if (_cachedStageManager == null) _cachedStageManager = FindFirstObjectByType<StageManager>();

        //Playing 상태일 때만 토글 버튼 활성화
        if (_cachedStageManager != null && _cachedStageManager.CurrentState == StageState.Playing)
        {
            _toggleBtn.gameObject.SetActive(true);
            _toggleBtn.onClick.RemoveAllListeners();
            _toggleBtn.onClick.AddListener(() =>
            {
                if (_currentWindow != null) _currentWindow.Toggle();
                else HideAugmentToggleBtn();
            });

            if (UIManager.Instance.TopContainer != null)
                _toggleBtn.transform.SetParent(UIManager.Instance.TopContainer);
        }
        else
        {
            //PreGameAugment 상태에서는 숨김, 방어막에 안막히게 직접 끄기(처음만)
            ExecuteHideToggleBtn();
        }

        if (_currentWindow != null)
        {
            _currentWindow.SetupAndOpen(myDatas, teamDatas, teamName, finalOpen);
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
            int myIndex = _slotContainer.childCount - 1;
            card.Setup(data, myIndex);
        }

        if (_cachedStageManager == null)
        {
            _cachedStageManager = FindFirstObjectByType<StageManager>();
        }

        //3.n 윤혁 수정사항
        if (_cachedStageManager != null)
        {
            // 증강 선택은 로컬 플레이어가 수행하므로 Runner.LocalPlayer를 사용
            _cachedStageManager.RPC_MarkHeroUsed(_cachedStageManager.Runner.LocalPlayer, data.targetId);
            Debug.Log($"[Masking] 증강 선택으로 영웅 기록됨: {data.targetId}");
        }
        //카드 새로 깔릴 때, 아군이 먹어둔 스킬증강이 있으면 아이콘 바로 반영
        RefreshHeroSkillIcons(AugmentController.Instance.Runner.LocalPlayer);
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

    //캡슐화 메서드
    public void NotifyTeammateConfirmed()
    {
        if (_currentWindow != null)
        {
            //UI에게 아군이 확정했다는 사실만 전달함
            //절 대 직 접 닫 지 마
            _currentWindow.ReceiveTeammateConfirmed();
        }
    }


    //3.17
       //딜레이 호출용 헬퍼
   public void DelayedRefreshSkillIcons()
   {
       if (AugmentController.Instance != null)
           RefreshHeroSkillIcons(AugmentController.Instance.Runner.LocalPlayer);
   }

   //화면에 깔린 영웅 카드 돌고 스킬 증강 꽂아주기
   public void RefreshHeroSkillIcons(PlayerRef player)
   {
       if (_cachedStageManager == null || _slotContainer == null) return;
       if (!_cachedStageManager.PlayerDataMap.TryGet(player, out var data)) return;

       //팀이 보유한 모든 스킬 증강 ID 수집
       List<string> mySkills = new List<string>();
       for (int i = 0; i < SlotData_5.Length; i++)
       {
           string skillId = data.OwnedSkillAugments.Get(i).Replace("\0", "").Trim();
           if (!string.IsNullOrEmpty(skillId)) mySkills.Add(skillId);
       }

       //현재 티어 계산 (6픽 이상이면 1티어, 아니면 0티어)
       int reinforceNum = 6;
       var config = TableManager.Instance.ConfigTable.Get("augment_reinforce_number");
       if (config != null) reinforceNum = int.Parse(config.configValue);
       int tierIndex = (data.TotalAugmentPicks >= reinforceNum) ? 1 : 0;

       //하단 덱을 순회하여 영웅 ID가 일치하는 스킬 아이콘 할당
       foreach (Transform child in _slotContainer)
       {
           if (child.TryGetComponent(out HeroHandCardUI card))
           {
               List<Sprite> icons = new List<Sprite>();
               foreach (var skillId in mySkills)
               {
                   var so = AugmentController.Instance.GetSkillAugmentById(skillId);
                   //내가 찾은 스킬의 타겟 영웅 ID가 이 카드의 영웅 ID와 일치하면 추가
                   if (so != null && so.TargetHeroID == card.HeroId)
                   {
                       if (so.Tiers != null && so.Tiers.Length > tierIndex)
                       {
                           icons.Add(so.Tiers[tierIndex].Icon);
                       }
                   }
               }
               card.UpdateSkillAugmentIcons(icons);
           }
       }
   }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        _cachedStageManager = null;
        _currentWindow = null;
        _isWaitingForServerResponse = false;
    }
}

