using UnityEngine;
using TMPro;
using System.Collections;

//일단 아이템 토스트메시지 전달용 UI
public class ToastMessageUI : BaseUI
{
    public static ToastMessageUI Instance { get; private set; }

    [Header("Toast Components")]
    [SerializeField] private TextMeshProUGUI _toastText;

    [Header("Settings")]
    [SerializeField] private float _displayTime = 2.0f;

    private Coroutine _toastCoroutine;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == null)
        {
            Instance = this;
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowToast(string message)
    {
        if (_toastText == null) return;

        _toastText.text = message;

        if (_toastCoroutine != null)
        {
            StopCoroutine(_toastCoroutine);
        }

        Open(false);

        _toastCoroutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        yield return CoroutineManager.waitForSeconds(_displayTime);

        DeActivate(false);

        _toastCoroutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}