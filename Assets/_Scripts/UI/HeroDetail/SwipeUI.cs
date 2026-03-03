using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwipeUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("연결할 컴포넌트들")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private Image[] _dots; // 하단 점 UI 배열

    [Header("세팅")]
    [SerializeField] private Color _activeColor; // 활성화된 점 색상
    [SerializeField] private Color _inactiveColor; // 비활성화된 점 색상
    [SerializeField] private float _lerpSpeed = 10f;

    private float[] _pagePositions;
    private int _pageCount;
    private int _currentPage = 0;
    private bool _isDragging = false;

    void Start()
    {
        _pageCount = _content.childCount;
        _pagePositions = new float[_pageCount];

        // 각 페이지의 스크롤 위치값 계산 (0~1 사이)
        for (int i = 0; i < _pageCount; i++)
        {
            _pagePositions[i] = (float)i / (_pageCount - 1);
        }
    }

    void Update()
    {
        if (_isDragging) return;

        // 가장 가까운 페이지로 부드럽게 이동
        float targetPos = _pagePositions[_currentPage];
        if(Mathf.Abs(_scrollRect.horizontalNormalizedPosition - targetPos) > 0.001f)
        {
            _scrollRect.horizontalNormalizedPosition = Mathf.Lerp
                (
                    _scrollRect.horizontalNormalizedPosition, 
                    targetPos,
                    Time.deltaTime * _lerpSpeed
                );
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        // 드래그가 끝난 시점의 위치에서 가장 가까운 페이지 계산
        float currentPos = _scrollRect.horizontalNormalizedPosition;
        int nearestPage = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < _pageCount; i++)
        {
            float distance = Mathf.Abs(currentPos - _pagePositions[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPage = i;
            }
        }

        if (_currentPage != nearestPage)
        {
            _currentPage = nearestPage;
            UpdateDots(_currentPage);
        }
    }
    private void UpdateDots(int index)
    {
        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].color = (i == index) ? _activeColor : _inactiveColor;
        }
    }
}
