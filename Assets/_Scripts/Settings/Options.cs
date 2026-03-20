using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class Options : MonoBehaviour
{
    [SerializeField] private Button _korBtn;
    [SerializeField] private Button _engBtn;

    private void Start()
    {
        RefreshLanguageButtons();

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
}
