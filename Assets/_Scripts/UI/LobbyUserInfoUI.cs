using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUserInfoUI : MonoBehaviour
{
    [Header("유저 정보 UI")]
    [SerializeField] private TMP_Text _nickNameTxt;
    [SerializeField] private TMP_Text _levelTxt;
    [SerializeField] private TMP_Text _goldTxt;

    [Header("경험치 게이지")]
    [SerializeField] private Image _expBar;
    [SerializeField] private TMP_Text _expTxt;

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        var userProfile = UserDataManager.Instance.ProfileModel;
        var userWallet = UserDataManager.Instance.WalletModel;

        if(userProfile == null ||  userWallet == null)
        {
            Debug.LogError("유저 데이터가 로드되지 않았습니다 (지갑, 프로파일)");
            return;
        }

        //기본 텍스트 세팅
        _nickNameTxt.text = userProfile.nickName;
        _levelTxt.text = $"Lv. {userProfile.userLevel}";
        _goldTxt.text = userWallet.gold.ToString("N0");

        //경험치 바 세팅
        float currentExp = userProfile.userExp;
        float maxExp = GetMaxExpForLevel(userProfile.userLevel);

        if (_expBar != null)
            _expBar.fillAmount = currentExp / maxExp;

        if (_expTxt != null)
            _expTxt.text = $"{currentExp} / {maxExp}";
    }

    // 일단 경험치 테이블 없어서 임의로 만듬
    private float GetMaxExpForLevel(int level)
    {
        // 레벨 1일 때 100, 레벨당 50씩 증가하는 식 (기획에 따라 변경)
        return 100 + (level - 1) * 50;
    }
}
