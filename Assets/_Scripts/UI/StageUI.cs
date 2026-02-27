using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

// Stage 씬 인게임 시퀀스에 쓰이는 UI들을 제어하는 클래스
public class StageUI : MonoBehaviour
{
    [SerializeField] private GameObject _vsPanel;
    [SerializeField] private GameObject _gradationImage;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private GameObject _defeatPanel;
    [SerializeField] private TextMeshProUGUI _countdownIndicator;
    [SerializeField] private TextMeshProUGUI _gameTimer;
    [SerializeField] private Slider _augmentGauge;
    public Button goLobbyBtn;

    private void Awake()
    {
        _countdownIndicator.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(false);
    }

    // 레드 팀일 때 UI 초기화
    public void InitRedTeam()
    {
        _gradationImage.transform.Rotate(0f, 0f, 180f);
    }

    public void ShowPlayerInfo()
    {
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
    }

    public void ShowResultPanel(bool isVictory)
    {
        GameObject result = isVictory ? _victoryPanel : _defeatPanel;
        result.SetActive(true);
        _resultPanel.SetActive(true);

    }
}
