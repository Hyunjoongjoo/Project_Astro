using UnityEngine;
using TMPro;

public class OptimizedFPS : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private float _updateInterval = 0.5f;

    private float _accum = 0f;
    private int _frames = 0;
    private float _timeLeft;

    private void Start()
    {
        if (_fpsText == null) _fpsText = GetComponent<TextMeshProUGUI>();
        _timeLeft = _updateInterval;
    }

    private void Update()
    {
        _timeLeft -= Time.unscaledDeltaTime;
        _accum += Time.unscaledDeltaTime;
        _frames++;

        //지정된 시간이 지날 때만 텍스트 갱신
        if (_timeLeft <= 0.0)
        {
            float fps = _frames / _accum;
            float msec = (1.0f / fps) * 1000.0f;

            _fpsText.text = $"{msec:0.0} ms ({fps:0.} fps)";

            if (fps < 30)
                _fpsText.color = Color.red;
            else if (fps < 50)
                _fpsText.color = Color.yellow;
            else
                _fpsText.color = Color.green;

            _timeLeft = _updateInterval;
            _accum = 0.0f;

            _frames = 0;
        }
    }
}
