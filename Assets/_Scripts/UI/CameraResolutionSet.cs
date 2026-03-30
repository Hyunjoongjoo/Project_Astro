using UnityEngine;
using UnityEngine.UI;

public class CameraResolutionSet : MonoBehaviour
{
    [Header("기준 화면 비율")]
    [SerializeField] private float _targetWidth = 9f;
    [SerializeField] private float _targetHeight = 19.5f;
    
    [Header("레터박스 적용 여부")]
    [SerializeField] private bool _applyLetterbox;
    
    [Header("캔버스 스케일러")]
    [SerializeField] CanvasScaler[] _canvasScaler;

    [Header("Safe Area 적용할 요소")]
    [SerializeField] RectTransform[] _applySafeArea;

    private float _targetAspect;

    void Awake()
    {
        _targetAspect = _targetWidth / _targetHeight;

        if (_canvasScaler != null && _canvasScaler.Length > 0)
            SetScaler();

        if (_applyLetterbox)
            ApplyLetterbox();

        if (_applySafeArea != null && _applySafeArea.Length > 0)
            ApplySafeArea();
    }

    private void ApplyLetterbox()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect <= _targetAspect)
        {
            // 기준보다 좁거나 같으면 그대로
            Camera.main.rect = new Rect(0, 0, 1, 1);
            return;
        }

        // 기준보다 넓은 경우 좌우에 레터박스 설정
        float normalizedWidth = _targetAspect / currentAspect;
        float xOffset = (1f - normalizedWidth) / 2f;

        Camera.main.rect = new Rect(xOffset, 0, normalizedWidth, 1);
    }

    private void SetScaler()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        float matchValue = 1f;
        // 기준보다 좁다 -> 폴드 접음 등 세로로 더 길다면
        if (currentAspect < _targetAspect) 
        {
            matchValue = 0f;
        }

        foreach (var scaler in _canvasScaler)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight = matchValue;
        }
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        foreach (var rectTransform in _applySafeArea)
        {
            Vector2 anchorMax = rectTransform.anchorMax;
            anchorMax.y = safeArea.height / rectTransform.rect.height;
            rectTransform.anchorMax = anchorMax;
        }
    }
}
