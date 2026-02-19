using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Stage 씬 인게임 시퀀스에 쓰이는 UI들을 제어하는 클래스
public class StageIntroUI : MonoBehaviour
{
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
        Debug.Log("카운트 다운 패널 보여줌");
    }

    public void UpdateCountdown(int count)
    {
        Debug.Log("카운트 다운 갱신 (3 -> 2 -> 1 -> Start 등");
    }

    public void HideCountdown()
    {
        gameObject.SetActive(false);
        Debug.Log("카운트 다운 패널 숨김");
    }
}
