using UnityEngine;

public class CameraResolutionSet : MonoBehaviour
{
    private float _targetWidth = 9f;
    private float _targetHeight = 19.5f;

    private float _targetAspect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _targetAspect = _targetWidth / _targetHeight;
        ApplyLetterbox();
    }

    void ApplyLetterbox()
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
}
