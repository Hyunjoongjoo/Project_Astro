using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class Options : MonoBehaviour
{
    [Header("Popup Prefab")]
    [SerializeField] private GameObject _confirmPopupPrefab;

    [Header("Buttons")]
    [SerializeField] private Button _korBtn;
    [SerializeField] private Button _engBtn;
    [SerializeField] private Button _LinkBtn;

    private void Start()
    {
        RefreshLanguageButtons();
        // 구글로그인으로 켜면 연동버튼 꺼야함
        if (!AuthService.Instance.IsGuestUser())
        {
            SuccessLinked();
        }

        //언어가 바뀔 때마다 버튼 상태 자동 갱신
        if (TableManager.Instance != null)
        {
            TableManager.Instance.OnLanguageChanged += RefreshLanguageButtons;
        }
    }


    public void OnClickLogOut() 
    {
        AuthService.Instance.Logout();
        GameManager.Instance.SetSceneState(SceneState.Title);
        UserDataManager.Instance.ClearCache();
        SceneManager.LoadScene("Title");
    }
    
    public void OnClickCloseGame() 
    { 
        Application.Quit();
    }

    public async void OnClickDeleteUser()
    {
        string uid = AuthService.Instance.CurrentUser.UserId;

        await UserDataStore.Instance.DeleteUserId(uid);

        bool authDeleted = await AuthService.Instance.DeleteUserAuth();

        if (authDeleted)
        {
            UserDataManager.Instance.ClearCache();
            GameManager.Instance.SetSceneState(SceneState.Title);
            SceneManager.LoadScene("Title");
        }
    }

    //한국어 버튼
    public void OnClickKorean()
    {
        TableManager.Instance.ChangeLanguage(LanguageType.Kor);
    }

    //영어 버튼
    public void OnClickEnglish()
    {
        TableManager.Instance.ChangeLanguage(LanguageType.Eng);
    }

    //현재 선택된 언어 버튼 비활성화(겸 하이라이트 처리)
    private void RefreshLanguageButtons()
    {
        if (_korBtn == null || _engBtn == null) return;

        bool isKor = TableManager.Instance.CurrentLanguage == LanguageType.Kor;
        _korBtn.interactable = !isKor; 
        _engBtn.interactable = isKor;
    }


    private void OnDestroy()
    {
        //구취
        if (TableManager.Instance != null)
        {
            TableManager.Instance.OnLanguageChanged -= RefreshLanguageButtons;
        }
    }

    public void OnClickAccountLink()
    {
        ConfirmPopup popup = UIManager.Instance.ShowUI<ConfirmPopup>(_confirmPopupPrefab, true);

        // 중복 로그인 리스너 멈춤(튕기면 안대니깐)
        if (popup != null)
        {
            // 2. 팝업 설정 (수정된 Setup 파라미터 반영)
            string msg = "현재 게스트 데이터를 구글 계정으로 연동하시겠습니까?\n연동 후에도 현재 데이터를 계속 사용합니다.";

            popup.Setup(
                msg: msg,
                onYes: async () => await ExecuteLinkProcess(),
                canConfirm: true,
                denyMsg: "",
                yesText: "연동하기",
                noText: "나중에"
            );
        }
    }

    private async Task ExecuteLinkProcess()
    {
        // 연동 중 중복 로그인 리스너 정지 (튕기면 안대니깐)
        UserDataManager.Instance.SetLinkMode(true);
        // AuthService를 통한 연동 로직 실행
        bool isSuccess = await AuthService.Instance.LinkAccountWithGoogle(UserDataManager.Instance);

        if (isSuccess)
        {
            Debug.Log("[Options] 구글 계정 연동 성공");
            SuccessLinked();
        }
        else
        {
            // 원래 정보로 리스너 복구
            Debug.LogError("[Options] 연동 취소 또는 실패");
        }

        // 다시 중복 로그인 리스너 켜기
        UserDataManager.Instance.SetLinkMode(false);

        UserDataManager.Instance.StartDuplicateLoginListener(
            AuthService.Instance.CurrentUser.UserId,
            AuthService.Instance.MyLocalSessionId
        );
    }

    // 성공하면 연동하기 버튼 필요없으니.
    private void SuccessLinked()
    {
        if (_LinkBtn == null) return;
        var btnText = _LinkBtn.GetComponentInChildren<TMPro.TMP_Text>();
        if (btnText != null)
        {
            btnText.text = "연동 완료";
        }
        _LinkBtn.interactable = false;
    }
}
