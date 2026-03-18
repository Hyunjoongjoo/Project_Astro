using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUserInfoView : MonoBehaviour
{
    [Header("유저 정보 UI")]
    [SerializeField] private TMP_Text _nickNameTxt;
    [SerializeField] private TMP_Text _levelTxt;
    [SerializeField] private TMP_Text _goldTxt;
    [SerializeField] private TMP_Text _winCountTxt;

    [Header("경험치 게이지")]
    [SerializeField] private Image _expBar;
    [SerializeField] private TMP_Text _expTxt;

    //닉네임
    public void SetNickName(string name) => _nickNameTxt.text = name;

    //골드 텍스트 갱신
    public void SetGold(int gold) => _goldTxt.text = gold.ToString("N0");

    //레벨 정보 세팅
    public void SetLevel(int level) => _levelTxt.text = $"{level}";

    // 경험치 게이지 및 텍스트 세팅
    public void SetExp(float current, float max)
    {
        if (_expBar != null)
            _expBar.fillAmount = current / max;

        if (_expTxt != null)
            _expTxt.text = $"{current} / {max}";
    }

    //승리수 갱신
    public void SetWinCount(int win)
    {
        if (_winCountTxt != null)
            _winCountTxt.text = $"{win}";
    }
}
