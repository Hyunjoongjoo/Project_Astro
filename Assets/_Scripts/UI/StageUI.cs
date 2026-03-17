using DG.Tweening;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Stage 씬 인게임 시퀀스에 쓰이는 UI들을 제어하는 클래스
public class StageUI : MonoBehaviour
{
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private GameObject _vsPanel;
    [SerializeField] private GameObject[] _rotationPanel;
    [SerializeField] private TextMeshProUGUI[] _introNameLabel;
    [SerializeField] private TextMeshProUGUI[] _ingameEnemyNameLabel;
    [SerializeField] private TextMeshProUGUI _countdownIndicator;
    [SerializeField] private TextMeshProUGUI _gameTimer;
    [SerializeField] private GameObject _teamMemberSlot;
    [SerializeField] private Transform _teammateContainer;
    [SerializeField] private Slider _augmentGauge;

    [Header("Result")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private GameObject _defeatPanel;
    [SerializeField] private GameObject _heroResultPrefab;
    [SerializeField] private Transform _heroListPanel;
    [SerializeField] private TextMeshProUGUI _resultGoldText;
    public Button goLobbyBtn;

    [Header("채팅 앵커")]
    [SerializeField] private Transform _myAnchor;
    [SerializeField] private Transform _teammateAnchor;
    [SerializeField] private Transform _enemy1Anchor;
    [SerializeField] private Transform _enemy2Anchor;

    [Header("네트워크 관련 처리")]
    [SerializeField] private GameObject _disconnectPanel;
    [SerializeField] private GameObject _waitingHost;
    [SerializeField] private GameObject _disconnected;

    private MatchType _matchType;

    // 호스트가 홈으로 가서 일시정지 되었음을 감지하기 위한 변수
    private float _lastRecievedRPCTime = 0f;
    private const float TIMER_RPC_TIMEOUT = 3f;
    private bool _isHostPaused = false;

    private void Awake()
    {
        _countdownIndicator.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_lastRecievedRPCTime == 0f) return;

        // 마지막 RPC 받은 시간이 3초를 넘겼다면
        bool hostUnresponsive = Time.realtimeSinceStartup - _lastRecievedRPCTime > TIMER_RPC_TIMEOUT;

        if (hostUnresponsive == true && _isHostPaused == false)
        {
            _isHostPaused = true;
            SetNetworkExceptionPanel(true, true);
        }
        else if (!hostUnresponsive && _isHostPaused)
        {
            _isHostPaused = false;
            SetNetworkExceptionPanel(false, true);
        }
    }

    public void SetMaxValueAugmentSlider(int value)
    {
        _augmentGauge.maxValue = value;
    }

    public void LocalInitialize(MatchType matchType, Team team)
    {
        _matchType = matchType;
        if (_matchType == MatchType.OneVsOne)
        {
            if (_introNameLabel.Length >= 4)
            {
                // 1:1이면 2:2 전용 UI 요소들은 가림.
                _introNameLabel[2].transform.parent.gameObject.SetActive(false);
                _introNameLabel[3].transform.parent.gameObject.SetActive(false);
                _teamMemberSlot.SetActive(false);
            }
        }

        if (team == Team.Red)
            InitRedTeam();
    }

    // 레드 팀일 때 UI 초기화
    public void InitRedTeam()
    {
        Camera.main.transform.Rotate(0f, 0f, 180f);

        foreach (var element in _rotationPanel)
            element.transform.Rotate(0f, 0f, 180f);

        foreach (var element in _introNameLabel)
            element.transform.Rotate(0f, 0f, 180f);
    }

    public void ShowPlayerInfo(PlayerNetworkData[] playersData)
    {
        // 현재 매치 타입에 따라 한 팀당 필요한 인원수 계산 (1:1은 1명, 2:2는 2명)
        int maxPlayers = _matchType == MatchType.OneVsOne ? 2 : 4;
        int teamSize = maxPlayers / 2;

        List<string> BlueTeamNames = new List<string>();
        List<string> RedTeamNames = new List<string>();

        // 넘어온 데이터가 있다면 팀별로 안전하게 분류
        if (playersData != null)
        {
            foreach (var player in playersData)
            {
                if (player.Team == Team.Blue)
                    BlueTeamNames.Add(player.PlayerName.ToString());
                else
                    RedTeamNames.Add(player.PlayerName.ToString());
            }
        }

        // 배열 길이가 부족하다면(빈자리가 있다면) "Dummy"로 채워 넣기
        while (BlueTeamNames.Count < teamSize)
            BlueTeamNames.Add("Dummy");

        while (RedTeamNames.Count < teamSize)
            RedTeamNames.Add("Dummy");

        // 인트로 UI 적용 (0, 2번 슬롯은 Blue / 1, 3번 슬롯은 Red로 짝지어짐)
        if (_introNameLabel.Length >= 2)
        {
            _introNameLabel[0].text = BlueTeamNames[0];
            _introNameLabel[1].text = RedTeamNames[0];
        }

        if (_matchType == MatchType.TwoVsTwo && _introNameLabel.Length >= 4)
        {
            _introNameLabel[2].text = BlueTeamNames[1];
            _introNameLabel[3].text = RedTeamNames[1]; 
        }

        Team myTeam = playersData[0].Team;

        // 인게임 UI 적용 (기존 로직의 흐름을 유지하되 인덱스 에러 방지)
        if (_ingameEnemyNameLabel.Length > 0)
            _ingameEnemyNameLabel[0].text = myTeam == Team.Blue ? RedTeamNames[0] : BlueTeamNames[0]; // 적 1

        if (_matchType == MatchType.TwoVsTwo)
        {
            if (_teamMemberSlot != null)
                _teamMemberSlot.transform.GetComponentInChildren<TextMeshProUGUI>().text =
                    myTeam == Team.Blue ? BlueTeamNames[1] : RedTeamNames[1]; // 아군

            if (_ingameEnemyNameLabel.Length > 1)
                _ingameEnemyNameLabel[1].text =
                    myTeam == Team.Blue ? RedTeamNames[1] : BlueTeamNames[1]; // 아군; // 적 2
        }

        _loadingPanel.SetActive(false);
        _vsPanel.SetActive(true);
        Debug.Log("매칭된 플레이어 정보를 보여줌");
    }

    //3.15 매개변수 추가, 팀메이트 정보까지
    public TeamCardSlotUI GetTeammateSlot(string nickname, PlayerRef allyRef)
    {
        var ui = _teamMemberSlot.GetComponent<TeamCardSlotUI>();
        ui.Initialize(nickname, allyRef);
        return ui; // 생성된 UI 컴포넌트를 반환하여 StageManager에게 전달
    }

    public void HidePlayerInfo()
    {
        _vsPanel.SetActive(false);
        Debug.Log("매칭된 플레이어 정보 패널 숨김");
    }

    public void UpdateCountdown(int count)
    {
        if (_countdownIndicator.gameObject.activeSelf == false)
            _countdownIndicator.gameObject.SetActive(true);

        _countdownIndicator.text = count.ToString();
        Debug.Log("카운트 다운 갱신 (3 -> 2 -> 1 -> Start 등");
    }

    public void HideCountdown()
    {
        _countdownIndicator.gameObject.SetActive(false);
        Debug.Log("카운트 다운 종료");
    }

    public void UpdateStageTimer(int timeSeconds)
    {
        _lastRecievedRPCTime = Time.realtimeSinceStartup;
        int minute = timeSeconds / 60;
        int second = timeSeconds % 60;
        _gameTimer.text = $"{minute} : {second:D2}";
    }

    public void UpdateAugmentGauge(int value)
    {
        _augmentGauge.value = Mathf.Min(_augmentGauge.maxValue, value);
        if (_augmentGauge.value >= _augmentGauge.maxValue)
            AugmentManager.Instance.ShowAugmentToggleBtn();
        else
            AugmentManager.Instance.HideAugmentToggleBtn();
    }

    public void ShowResultPanel(bool isVictory, List<HeroResultData> heroes, int goldAmount)
    {
        _victoryPanel.SetActive(isVictory);
        _defeatPanel.SetActive(!isVictory);

        // 골드 텍스트 설정
        if (_resultGoldText != null)
            _resultGoldText.text = goldAmount.ToString("N0");

        // 기존에 생성되어 있던 프리팹 삭제
        foreach (Transform child in _heroListPanel)
            Destroy(child.gameObject);

        Debug.Log(heroes.Count);

        // 사용한 영웅들만 프리팹 생성 및 데이터 주입
        foreach (var data in heroes)
        {
            Debug.Log(data.HeroId);
            GameObject listObj = Instantiate(_heroResultPrefab, _heroListPanel);
            if (listObj.TryGetComponent<HeroResult>(out var list))
            {
                list.Setup(data);
            }
        }

        _resultPanel.SetActive(true);
    }

    public Transform GetChatAnchor(PlayerRef sender, PlayerRef localPlayer, Team senderTeam, Team myTeam, PlayerRef[] enemyRefs)
    {
        // 나 자신
        if (sender == localPlayer) return _myAnchor;

        // 아군
        if (senderTeam == myTeam) return _teammateAnchor;

        // 적군 구분 (적 배열과 비교)
        if (enemyRefs != null)
        {
            if (sender == enemyRefs[0]) return _enemy1Anchor;
            if (enemyRefs.Length > 1 && sender == enemyRefs[1]) return _enemy2Anchor;
        }

        return _enemy1Anchor; // 기본값
    }

    public void SetNetworkExceptionPanel(bool panelActive, bool isWaiting)
    {
        _disconnected.SetActive(!isWaiting);
        _waitingHost.SetActive(isWaiting);
        _disconnectPanel.SetActive(panelActive);
    }
}
