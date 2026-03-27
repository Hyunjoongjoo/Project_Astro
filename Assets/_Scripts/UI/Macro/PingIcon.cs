using UnityEngine;
using DG.Tweening;
using Fusion;

public class PingIcon : MonoBehaviour
{
    [SerializeField] private float _duration = 2f;
    [SerializeField] private GameObject _checkMark;

    [Header("두트윈 효과 설정")]
    [SerializeField] private float _punchStrength = 0.5f; // 펀치 강도
    [SerializeField] private int _vibrato = 10;          // 떨림 횟수

    private PlayerRef _owner;

    public Vector2 Position => transform.position; //위치 체크용

    void Start()
    {
        if (_checkMark != null) _checkMark.SetActive(false);

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        AudioManager.Instance.PlayUISfx(UISfxList.Ping);

        ResetDestroyTimer();
    }

    public void Init(PlayerRef creator)
    {
        _owner = creator;
    }

    public bool IsOwner(PlayerRef player)
    {
        return _owner == player;
    }

    public void ActivateCheckMark()
    {
        if (_checkMark != null)
        {
            _checkMark.transform.localScale = Vector3.one;
            _checkMark.SetActive(true);

            _checkMark.transform.DOPunchScale(Vector3.one * _punchStrength, 0.5f, _vibrato, 0.5f);

            AudioManager.Instance.PlayUISfx(UISfxList.Ping);

            ResetDestroyTimer(); //상호작용 성공하면 2초정도 더 유지하게
        }
    }

    private void ResetDestroyTimer()
    {
        // 기존에 예약된 삭제 호출을 취소
        CancelInvoke(nameof(DestroyMe));

        // 2초후에 삭제하게 예약
        Invoke(nameof(DestroyMe), _duration);
    }

    private void DestroyMe()
    {
        Destroy(gameObject);
    }

    //파괴될때 두트윈 종료
    private void OnDestroy()
    {
        transform.DOKill();
        if (_checkMark != null) _checkMark.transform.DOKill();
    }
}
