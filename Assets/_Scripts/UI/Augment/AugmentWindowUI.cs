using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//3.5 리팩토링
//덱 매니저가 뽑아준 카드 타입 보고 지정된 프리팹 생산 및 데이터 주입하도록 변경

//3.10 리팩토링
//Augment_Panel 2:2 로직에서 아군 출력 로직 구현
public class AugmentWindowUI : BaseUI
{
    [Header("내 증강 UI")]
    [SerializeField] private Transform _myCardContainer; //MyAugment/Panel 연결

    [Header("아군 증강 UI (2vs2 전용)")]
    [SerializeField] private GameObject _teamAugmentGroup; //TeamAugment 객체 자체 연결 (1vs1 땐 끄기 위함)
    [SerializeField] private Transform _teamCardContainer; //TeamAugment/Panel 연결
    [SerializeField] private TMP_Text _teamNameTxt; //TeamAugment/Team_Name 연결

    [Header("공통 UI")]
    [SerializeField] private TMP_Text _timerTxt; //타이머
    [SerializeField] private Button _confirmBtn;

    //프리팹 3종
    [Header("UI Prefabs")]
    [SerializeField] private GameObject _heroCardPrefab;
    [SerializeField] private GameObject _skillCardPrefab;
    [SerializeField] private GameObject _itemCardPrefab;

    //현재 유저가 클릭해둔 카드 데이터 추적용
    private AugmentData _selectedData;
    private IAugmentUI _selectedCardUI;

    private StageManager _stageManager;
    private List<AugmentData> _currentDatas; //뽑은 3장 기억용
    private bool _isForcePicked = false;

    //생성된 카드 추적용 리스트
    private List<GameObject> _spawnedCards = new List<GameObject>();

    //3.11 리팩토링
    //상태 체크용 변수 추가
    //1vs1, 2vs2 체크
    private bool _is2vs2;

    public bool IsForcePicked => _isForcePicked;

    //아군 확정 여부 추적 프로퍼티
    public bool IsTeammateConfirmed { get; set; } = false;


    //3.24 추가
    private void OnEnable()
    {
        //창이 켜지는 순간 Update가 돌기 전이라도 즉시 시간 표시
        UpdateTimerUI();
    }


    //3.10 리팩토링
    //아군 데이터와 이름도 받을 수 있도록 매개변수 추가 (아군 데이터는 없을 수도 있으므로 null 허용)
    //3.12 리팩토링
    public void SetupAndOpen(List<AugmentData> myDatas, List<AugmentData> teamDatas = null, string teamName = "", bool isForcedOpen = true)
    {
        _currentDatas = myDatas;
        _stageManager = FindFirstObjectByType<StageManager>();
        _isForcePicked = false;
        _selectedData = null;
        _selectedCardUI = null;

        //2vs2 여부 판단 및 저장, 아군 확정 상태 초기화
        _is2vs2 = (teamDatas != null && teamDatas.Count > 0);
        IsTeammateConfirmed = false;


        //확정버튼초기화
        if (_confirmBtn != null)
        {
            _confirmBtn.interactable = false;
            _confirmBtn.onClick.RemoveAllListeners();
            _confirmBtn.onClick.AddListener(OnConfirmClicked);
        }

        ClearCards();

        //내 카드 생성(클릭O)
        SpawnCards(myDatas, _myCardContainer, true);

        //아군 카드 생성(데이터가 넘어왔을 경우 = 2vs2 모드)
        if (teamDatas != null && teamDatas.Count > 0)
        {
            if (_teamAugmentGroup != null) _teamAugmentGroup.SetActive(true);
            if (_teamNameTxt != null) _teamNameTxt.text = teamName;

            //아군 카드는 클릭 불가능
            SpawnCards(teamDatas, _teamCardContainer, false);
        }
        else
        {
            //1vs1 모드이거나 아군 데이터가 없으면 아군 패널 숨김
            if (_teamAugmentGroup != null) _teamAugmentGroup.SetActive(false);
        }
        if (isForcedOpen)
        {
            base.Open(); //내가 눌렀거나 강제 오픈 시엔 눈앞에 띄움
        }
        else
        {
            DeActivate(); //아군이 눌렀을 땐 생성만하고 숨겨두기
        }
    }

    //3.10
    //카드 생성하는 중복 코드를 별도의 함수로 분리
    private void SpawnCards(List<AugmentData> datas, Transform container, bool isInteractable)
    {
        foreach (var data in datas)
        {
            GameObject prefabToSpawn = null;

            switch (data.type)
            {
                case AugmentType.Hero: prefabToSpawn = _heroCardPrefab; break;
                case AugmentType.Skill: prefabToSpawn = _skillCardPrefab; break;
                case AugmentType.Item:
                    prefabToSpawn = _itemCardPrefab;
                    if (prefabToSpawn == null) prefabToSpawn = _heroCardPrefab;
                    break;
            }

            if (prefabToSpawn != null)
            {
                GameObject cardObj = Instantiate(prefabToSpawn, container);
                _spawnedCards.Add(cardObj);

                if (cardObj.TryGetComponent(out IAugmentUI augmentUI))
                {
                    augmentUI.Setup(data);
                }
                else
                {
                    Debug.LogError($"{prefabToSpawn.name} 프리팹에 IAugmentUI 스크립트가 없음");
                }

                //아군 카드일 경우 클릭하지 못하도록 Button 비활성화
                if (!isInteractable)
                {
                    if (cardObj.TryGetComponent(out Button btn)) btn.interactable = false;
                }
            }
        }
    }



    //3.24 수정, Update 아닌 별도의 메서드로 변경
    //매 프레임 남은 시간 UI 갱신
    private void Update()
    {
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (_stageManager != null && _timerTxt != null && !_isForcePicked)
        {
            //상태(시작 전, 진행중) 에 따라 알맞은 곳에서 타이머 정보
            float timeLeft = 0f;

            if (_stageManager.CurrentState == StageState.PreGameAugment)
            {
                //시작 전 2연속 증강은 StageManager의 글로벌 시계 참조
                timeLeft = _stageManager.StateTimer;
            }
            else if (_stageManager.CurrentState == StageState.Playing)
            {
                //인게임 증강은 AugmentController의 개별 유저 시계 참조
                if (AugmentController.Instance.PlayerAugmentTimers.TryGet(_stageManager.Runner.LocalPlayer, out float time))
                {
                    timeLeft = time;
                }
            }
            //소수점 올림
            if (timeLeft > 0 || _stageManager.CurrentState == StageState.PreGameAugment)
            {
                int displayTime = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                //콘피그 테이블 생기면 이 포맷 하드코딩 한 거 없애야함!(3.24 여현구)
                _timerTxt.text = $"제한 시간: {displayTime}초";
            }
        }
    }

    //카드들이 자신을 터치했을 때 호출하는 함수
    public void OnCardSelected(IAugmentUI cardUI, AugmentData data)
    {
        //이미선택한거있으면false
        if (_selectedCardUI != null)
        {
            _selectedCardUI.ToggleHighlight(false);
        }

        //새로운 카드를 기억 & 하이라이트
        _selectedCardUI = cardUI;
        _selectedData = data;
        _selectedCardUI.ToggleHighlight(true);

        //확정 버튼 활성화
        if (_confirmBtn != null) _confirmBtn.interactable = true;
    }

    //확정 버튼을 눌렀을 때 발동
    private void OnConfirmClicked()
    {
        if (_selectedData == null || _isForcePicked) return;

        _isForcePicked = true;
        _confirmBtn.interactable = false;

        //1vs1 / 2vs2 분기 처리
        if (_is2vs2)
        {
            //아군보다 나중에 확정했다면 창과 버튼 파괴
            if (IsTeammateConfirmed)
            {
                AugmentManager.Instance.HideAugmentToggleBtn();
                this.Close(); //무조건 현재 본인 UI 윈도우만 닫도록(다음꺼 제외)
            }
            else
                DeActivate(); //아군이 아직 안 골랐다면 토글 가능하도록 숨기기만
        }
        else
        {
            //1:1 모드일 때는 무조건 파괴 및 여닫기 버튼 증발
            AugmentManager.Instance.HideAugmentToggleBtn();
            this.Close();
        }
        AugmentManager.Instance.SelectAugment(_selectedData);
    }

    //타임아웃 시
    public void ForceRandomPick()
    {
        //타임아웃과 클릭 확정 겹칠 때 패킷 2번 날아가는 거 방지
        if (_isForcePicked) return;
        _isForcePicked = true;

        if (_currentDatas != null && _currentDatas.Count > 0)
        {
            //0~2 중 하나 무작위 추첨
            int randIndex = Random.Range(0, _currentDatas.Count);
            AugmentData pickedData = _currentDatas[randIndex];

            Debug.Log($"타임아웃으로인해 자동 선택: {pickedData.titleName}");
            //타임아웃 강제선택 시에도 클릭 확정과 동일한 분기 로직 적용
            if (_is2vs2)
            {
                if (IsTeammateConfirmed)
                {
                    AugmentManager.Instance.HideAugmentToggleBtn();
                    this.Close();
                }

                else
                {
                    DeActivate();
                }
            }
            else
            {
                AugmentManager.Instance.HideAugmentToggleBtn();
                this.Close();
            }

            //유저가 직접 클릭한 것과 같은 로직
            AugmentManager.Instance.SelectAugment(pickedData);

        }
    }

    //화면에 띄워진 카드 삭제
    private void ClearCards()
    {
        foreach (var card in _spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        _spawnedCards.Clear();
    }

    //BaseUI 오버라이드
    public override void Close(bool playSound = true)
    {
        //생성했던 카드 처리
        ClearCards();

        base.Close();
    }

    //아군의 확정 신호를 수신했을 때 스스로 판단해서 닫음
    public void ReceiveTeammateConfirmed()
    {
        IsTeammateConfirmed = true;

        //내가 이미 고른 상태에서 아군까지골랐으면 파괴
        if (_isForcePicked)
        {
            AugmentManager.Instance.HideAugmentToggleBtn();
            this.Close();
        }
    }

}
