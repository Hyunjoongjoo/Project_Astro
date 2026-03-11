using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Stage 씬 인게임 시퀀스에 쓰이는 UI들을 제어하는 클래스
public class StageUI : MonoBehaviour
{
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
    private MatchType _matchType;

    private void Awake()
    {
        _countdownIndicator.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(false);
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

        List<string> myTeamNames = new List<string>();
        List<string> enemyTeamNames = new List<string>();

        Team myTeam = Team.Blue;

        // 넘어온 데이터가 있다면 팀별로 안전하게 분류
        if (playersData != null)
        {
            myTeam = playersData[0].Team;
            foreach (var player in playersData)
            {
                if (player.Team == myTeam)
                    myTeamNames.Add(player.PlayerName.ToString());
                else
                    enemyTeamNames.Add(player.PlayerName.ToString());
            }
        }

        // 배열 길이가 부족하다면(빈자리가 있다면) "Dummy"로 채워 넣기
        while (myTeamNames.Count < teamSize)
            myTeamNames.Add("Dummy");

        while (enemyTeamNames.Count < teamSize)
            enemyTeamNames.Add("Dummy");

        // 인트로 UI 적용 (0, 2번 슬롯은 Blue / 1, 3번 슬롯은 Red로 짝지어짐)
        if (_introNameLabel.Length >= 2)
        {
            _introNameLabel[0].text = myTeamNames[0];
            _introNameLabel[1].text = enemyTeamNames[0];
        }

        if (_matchType == MatchType.TwoVsTwo && _introNameLabel.Length >= 4)
        {
            _introNameLabel[2].text = myTeamNames[1];
            _introNameLabel[3].text = enemyTeamNames[1]; 
        }

        // 인게임 UI 적용 (기존 로직의 흐름을 유지하되 인덱스 에러 방지)
        if (_ingameEnemyNameLabel.Length > 0)
            _ingameEnemyNameLabel[0].text = enemyTeamNames[0]; // 적 1

        if (_matchType == MatchType.TwoVsTwo)
        {
            if (_teamMemberSlot != null)
                _teamMemberSlot.transform.GetComponentInChildren<TextMeshProUGUI>().text = myTeamNames[1]; // 아군

            if (_ingameEnemyNameLabel.Length > 1)
                _ingameEnemyNameLabel[1].text = enemyTeamNames[1]; // 적 2
        }

        _vsPanel.SetActive(true);
        Debug.Log("매칭된 플레이어 정보를 보여줌");
    }

    public TeamCardSlotUI GetTeammateSlot(string nickname)
    {
        var ui = _teamMemberSlot.GetComponent<TeamCardSlotUI>();
        ui.Initialize(nickname);
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
        int minute = timeSeconds / 60;
        int second = timeSeconds % 60;
        _gameTimer.text = $"{minute} : {second:D2}";
    }

    public void UpdateAugmentGauge(int value)
    {
        _augmentGauge.value = value;
        if (_augmentGauge.value >= 100f)
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
}
