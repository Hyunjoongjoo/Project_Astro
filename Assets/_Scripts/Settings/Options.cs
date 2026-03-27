using System;
using System.Threading.Tasks;
using TMPro;
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

    [SerializeField] private TMP_InputField _nickNameText;
    [SerializeField] private TextMeshProUGUI _resultText;

    private string _verifiedNickname = string.Empty;
    private bool _isNicknameVerified = false;

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
        ConfirmPopup popup = UIManager.Instance.ShowUI<ConfirmPopup>(_confirmPopupPrefab, true);

        // 중복 로그인 리스너 멈춤(튕기면 안대니깐)
        if (popup != null)
        {
            // 2. 팝업 설정 (수정된 Setup 파라미터 반영)
            string msg = "타이틀로 돌아가시겠습니까?";

            popup.Setup(
                msg: msg,
                onYes: async  () => await LogOut(),
                canConfirm: true,
                denyMsg: "",
                yesText: "로그아웃",
                noText: "취소"
            );
        }
    }
    
    private async Task LogOut()
    {
        AuthService.Instance.Logout();
        GameManager.Instance.SetSceneState(SceneState.Title);
        UserDataManager.Instance.ClearCache();
        await Task.Yield();
        SceneManager.LoadScene("Title");
    }

    public void OnClickCloseGame() 
    { 
        Application.Quit();
    }

    public void OnClickDeleteUser()
    {
        ConfirmPopup popup = UIManager.Instance.ShowUI<ConfirmPopup>(_confirmPopupPrefab, true);

        // 중복 로그인 리스너 멈춤(튕기면 안대니깐)
        if (popup != null)
        {
            // 2. 팝업 설정 (수정된 Setup 파라미터 반영)
            string msg = "계정을 삭제하시겠습니까?";

            popup.Setup(
                msg: msg,
                onYes: async () => await DeleteUser(),
                canConfirm: true,
                denyMsg: "",
                yesText: "삭제",
                noText: "취소"
            );
        }
    }
    private async Task DeleteUser()
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
            string msg = "구글 계정으로 연동하시겠습니까?";

            popup.Setup(
                msg: msg,
                onYes: async () => await ExecuteLinkProcess(),
                canConfirm: true,
                denyMsg: "",
                yesText: "확인",
                noText: "취소"
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

    public async void OnClickCheckNickName()
    {
        string inputName = _nickNameText.text;

        try
        {
            InputValidator.ValidateOrThrow(inputName);

            bool isDuplicate = await UserDataStore.Instance.IsNicknameDuplicateAsync(inputName);

            if (isDuplicate)
            {
                _isNicknameVerified = false;
                _verifiedNickname = string.Empty;
                _resultText.text = "이미 사용 중인 닉네임입니다.";
                return;
            }

            // 성공 시 상태 저장
            _isNicknameVerified = true;
            _verifiedNickname = inputName;
            _resultText.text = $"'{inputName}'은(는) 사용 가능한 닉네임입니다.";
        }
        catch (Exception ex)
        {
            _isNicknameVerified = false;
            _resultText.text = "검증에 실패하였습니다.";
            Debug.LogError($"검증 실패: {ex.Message}");
        }
    }

    public async void OnClickNickNameEdit()
    {
        string currentInput = _nickNameText.text;

        // 중복 확인을 아예 안 했거나, 확인받은 이름과 현재 입력창 이름이 다른 경우
        if (!_isNicknameVerified || _verifiedNickname != currentInput)
        {
            _resultText.text = "닉네임 중복 확인을 다시 해주세요.";
            return;
        }

        try
        {
            await UserDataManager.Instance.UpdateNicknameAsync(currentInput);

            _isNicknameVerified = false;
            _verifiedNickname = string.Empty;

            _resultText.text = "닉네임 변경이 완료되었습니다.";
        }
        catch (Exception ex)
        {
            _resultText.text = "최종 변경 실패";
        }
    }

    //운영정책 사이트 연결
    public void OnClickOpenInfoSite()
    {
        string url = "https://sites.google.com/view/astrocommandersinfo/%ED%99%88";

        try
        {
            Application.OpenURL(url);
            Debug.Log($"[Options] URL 열기 시도: {url}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Options] URL을 열 수 없습니다: {ex.Message}");
        }
    }
}
