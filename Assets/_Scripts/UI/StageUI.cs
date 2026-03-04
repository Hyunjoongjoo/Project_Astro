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
    [SerializeField] private Slider _augmentGauge;

    [Header("Result")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private GameObject _defeatPanel;
    [SerializeField] private GameObject _heroResultPrefab;
    [SerializeField] private Transform _heroListPanel;
    [SerializeField] private TextMeshProUGUI _resultGoldText;

    public Button goLobbyBtn;

    private void Awake()
    {
        _countdownIndicator.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(false);
    }

    public void LocalInitialize(int playerNum, Team team)
    {
        if (playerNum == 2)
        {
            // 1:1이면 2:2 전용 UI 요소들은 가림.
            _introNameLabel[2].transform.parent.gameObject.SetActive(false);
            _introNameLabel[3].transform.parent.gameObject.SetActive(false);
            _teamMemberSlot.SetActive(false);
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
        // 인스펙터에 블루1 -> 레드1 -> 블루2 -> 레드2 순으로 꼽혀있음
        int blueIndex = 0;
        int redIndex = 1;
        foreach (var player in playersData)
        {
            if (player.Team == Team.Blue)
            {
                if (blueIndex < playersData.Length)
                {
                    _introNameLabel[blueIndex].text = player.PlayerName.ToString();
                    blueIndex += 2;
                }
                
            }
            else
            {
                if (redIndex < playersData.Length)
                {
                    _introNameLabel[redIndex].text = player.PlayerName.ToString();
                    redIndex += 2;
                } 
            }
        }

        string enemy = playersData[1].PlayerName.ToString();
        _ingameEnemyNameLabel[0].text = enemy;

        // 2:2면 인게임 UI에 이름 추가로 표시
        if (playersData.Length > 2)
        {
            string ally = playersData[2].PlayerName.ToString();
            _teamMemberSlot.transform.GetComponentInChildren<TextMeshProUGUI>().text = ally;

            string enemy2 = playersData[3].PlayerName.ToString();
            _ingameEnemyNameLabel[1].text = enemy2;
        }

        _vsPanel.SetActive(true);
        Debug.Log("매칭된 플레이어 정보를 보여줌");
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

    public void ShowResultPanel(bool isVictory, List<HeroResultData> heroes, int gold)
    {
        _victoryPanel.SetActive(isVictory);
        _defeatPanel.SetActive(!isVictory);

        // 골드 텍스트 설정
        if (_resultGoldText != null)
            _resultGoldText.text = gold.ToString("N0");

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
