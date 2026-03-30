using UnityEngine;

public class LobbyUserInfoPresenter : MonoBehaviour
{
    [SerializeField] private LobbyUserInfoView _view;

    private void Start()
    {
        InitSetup();
    }

    private void OnEnable()
    {
        // 이벤트 구독
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnGoldChanged += UpdateGoldView;
            UserDataManager.Instance.OnWinCountChanged += UpdateWinView;
            UserDataManager.Instance.OnNickNameChanged += UpdateNicknameView;
        }
    }

    private void OnDisable()
    {
        //이벤트 구독 해제
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnGoldChanged -= UpdateGoldView;
            UserDataManager.Instance.OnWinCountChanged -= UpdateWinView;
            UserDataManager.Instance.OnNickNameChanged -= UpdateNicknameView;
        }
    }

    private void InitSetup()
    {
        var profile = UserDataManager.Instance.ProfileModel;
        var wallet = UserDataManager.Instance.WalletModel;
        var record = UserDataManager.Instance.RecordModel;

        if (profile == null || wallet == null || record == null) return;

        // View에 초기값 전달
        _view.SetNickName(profile.nickName);
        _view.SetGold(wallet.gold);
        _view.SetLevel(profile.userLevel);
        _view.SetWinCount(record.win);

        float maxExp = GetMaxExpForLevel(profile.userLevel);
        _view.SetExp(profile.userExp, maxExp);
    }

    // 골드 변경 이벤트 발생 시 실행될 콜백
    private void UpdateGoldView(int newGold) => _view.SetGold(newGold);
    // 승리수 갱신 콜백
    private void UpdateWinView(int newWin) => _view.SetWinCount(newWin);
    private void UpdateNicknameView(string newNickName) => _view.SetNickName(newNickName);

    // 계산 로직은 Presenter가 담당 (나중에 테이블 매니저 참조로 변경 가능)
    private float GetMaxExpForLevel(int level)
    {
        return 100 + (level - 1) * 50;
    }
}
