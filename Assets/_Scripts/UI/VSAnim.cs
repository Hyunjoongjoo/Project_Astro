using UnityEngine;
using DG.Tweening;

public class VSAnim : BaseUI
{
    [Header("그라데이션 연출")]
    [SerializeField] private CanvasGroup _redGradationCG;
    [SerializeField] private CanvasGroup _blueGradationCG;

    [Header("팀 이름 연출")]
    [SerializeField] private RectTransform[] _redTeamNames;
    [SerializeField] private RectTransform[] _blueTeamNames;

    [Header("vs 표시 연출")]
    [SerializeField] private RectTransform _vsTxtRect;
    [SerializeField] private CanvasGroup _vsTxtCG;
    [SerializeField] private CanvasGroup _effectCG;

    [Header("세팅")]
    [SerializeField] private float _animDuration = 1f;

    private void OnEnable()
    {
        PlayVSAnimation();
    }
    public void PlayVSAnimation()
    {
        PrepareAnimation();

        // 시퀀스 생성
        Sequence vsSeq = DOTween.Sequence();

        // 그라데이션 페이드인
        vsSeq.Join(_redGradationCG.DOFade(1, _animDuration));
        vsSeq.Join(_blueGradationCG.DOFade(1, _animDuration));

        // 팀 이름들 날아오기
        for (int i = 0; i < _redTeamNames.Length; i++)
        {
            // 레드팀: 오른쪽(500) -> 원래 위치(0)
            vsSeq.Join(_redTeamNames[i].DOAnchorPosX(0, _animDuration).From(new Vector2(-500, 0)).SetEase(Ease.OutCubic));
        }

        for (int i = 0; i < _blueTeamNames.Length; i++)
        {
            // 블루팀: 왼쪽(-500) -> 원래 위치(0)
            vsSeq.Join(_blueTeamNames[i].DOAnchorPosX(0, _animDuration).From(new Vector2(500, 0)).SetEase(Ease.OutCubic));
        }

        // VS 텍스트 회전하며 등장
        vsSeq.AppendInterval(0.1f); // 팀 이름들이 올 때쯤 시작
        vsSeq.Join(_vsTxtCG.DOFade(1, _animDuration));

        vsSeq.Join(_vsTxtRect.DOScale(1, _animDuration)
            .From(new Vector3(5f, 5f, 1f)) // 원래 크기의 5배에서 시작
            .SetEase(Ease.OutBack)); // 마지막에 살짝 튕기는 탄성 효과

        // 회전하며 등장
        vsSeq.Join(_vsTxtRect.DORotate(new Vector3(0, 360, 0), _animDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic)); // 부드럽게 멈춤

        // 이펙트 페이드인
        vsSeq.Join(_effectCG.DOFade(1, 0.3f).SetDelay(0.1f));

        // 모든 연출 종료 후 처리 (필요시)
        vsSeq.OnComplete(() => Debug.Log("VS 연출 완료"));
    }

    private void PrepareAnimation()
    {
        _redGradationCG.alpha = 0;
        _blueGradationCG.alpha = 0;
        _vsTxtCG.alpha = 0;
        _effectCG.alpha = 0;
        _vsTxtRect.localScale = Vector3.zero;

    }
}
