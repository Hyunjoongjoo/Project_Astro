using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

// Stage 씬 인게임 시퀀스에 쓰이는 UI들을 제어하는 클래스
public class StageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textIndicator;
    public Button goLobbyBtn;

    private void Awake()
    {
        _textIndicator.gameObject.SetActive(false);
        goLobbyBtn.gameObject.SetActive(false);
    }

    public void ShowPlayerInfo()
    {
        Debug.Log("매칭된 플레이어 정보를 보여줌");
    }

    public void HidePlayerInfo()
    {
        Debug.Log("매칭된 플레이어 정보 패널 숨김");
    }

    public void ShowCountdown(int count)
    {
        _textIndicator.gameObject.SetActive(true);
        Debug.Log("카운트 다운 패널 보여줌");
    }

    public void UpdateCountdown(int count)
    {
        _textIndicator.text = count.ToString();
        Debug.Log("카운트 다운 갱신 (3 -> 2 -> 1 -> Start 등");
    }

    public void HideCountdown()
    {
        gameObject.SetActive(false);
        Debug.Log("카운트 다운 패널 숨김");
    }

    public void ShowResultPanel(bool isVictory)
    {
        _textIndicator.text = isVictory ? "승리" : "패배";
        goLobbyBtn.gameObject.SetActive(true);
    }
}
