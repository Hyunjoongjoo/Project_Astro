using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//3.5 리팩토링
//덱 매니저가 뽑아준 카드 타입 보고 지정된 프리팹 생산 및 데이터 주입하도록 변경
public class AugmentWindowUI : BaseUI
{
    [SerializeField] Transform _cardContainer; // 카드 배치시킬 위치

    [Header("UI")]
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

    public void SetupAndOpen(List<AugmentData> datas)
    {
        _currentDatas = datas;
        _stageManager = FindFirstObjectByType<StageManager>();
        _isForcePicked = false;
        _selectedData = null;
        _selectedCardUI = null;


        //확정버튼초기화
        if (_confirmBtn != null)
        {
            _confirmBtn.interactable = false;
            _confirmBtn.onClick.RemoveAllListeners();
            _confirmBtn.onClick.AddListener(OnConfirmClicked);
        }

        ClearCards();

        //새 카드 생성
        foreach (var data in datas)
        {
            GameObject prefabToSpawn = null;

            //타입에 따라 알맞은 프리팹 선택
            switch (data.type)
            {
                case AugmentType.Hero:
                    prefabToSpawn = _heroCardPrefab;
                    break;
                case AugmentType.Skill:
                    prefabToSpawn = _skillCardPrefab;
                    break;
                case AugmentType.Item:
                    prefabToSpawn = _itemCardPrefab;
                    //아이템 프리팹이 비어있다면 에러 방지용 임시 할당
                    if (prefabToSpawn == null) prefabToSpawn = _heroCardPrefab;
                    break;
            }

            if (prefabToSpawn != null)
            {
                //프리팹 생성 및 리스트에 추가
                GameObject cardObj = Instantiate(prefabToSpawn, _cardContainer);
                _spawnedCards.Add(cardObj);

                //IAugmentUI 인터페이스를 통해 데이터 주입
                //영웅이든 스킬이든 상관없이 Setup 함수를 호출
                if (cardObj.TryGetComponent(out IAugmentUI augmentUI))
                {
                    augmentUI.Setup(data);
                }
                else
                {
                    Debug.LogError($"{prefabToSpawn.name} 프리팹에 IAugmentUI를 상속받은 스크립트가 없음");
                }
            }
        }

        base.Open();
    }

    //매 프레임 남은 시간 UI 갱신
    private void Update()
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

        AugmentManager.Instance.SelectAugment(_selectedData);
        Close();
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

            //유저가 직접 클릭한 것과 같은 로직
            AugmentManager.Instance.SelectAugment(pickedData);
            Close();
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
    public override void Close()
    {
        //생성했던 카드 처리
        ClearCards();

        base.Close();
    }
}
