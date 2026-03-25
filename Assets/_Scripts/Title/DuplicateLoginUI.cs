using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DuplicateLoginUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Button _btn;

    private string _message = "다른 환경에서 접속되어 로그아웃 됩니다.";

    private void Awake()
    {
        _text.text = _message;
        Time.timeScale = 0f;
        _btn.onClick.AddListener(OnClickBtn);
    }

    public void OnClickBtn()
    {
        // 타임스케일 다시 돌려줌
        Time.timeScale = 1f;

        // 로그아웃 및 세션/리스너 정리
        if (AuthService.Instance != null)
        {
            AuthService.Instance.Logout();
        }

        // DB 캐싱 데이터 클리어
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.ClearCache();
        }

        // 타이틀로 이동 및 상태 변경
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSceneState(SceneState.Title);
        }

        Debug.Log("[DuplicateLogin] 모든 상태 초기화 후 타이틀로 이동");
        SceneManager.LoadScene("Title");
    }
}
