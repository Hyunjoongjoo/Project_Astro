using UnityEngine;

public abstract class BaseUI : MonoBehaviour
{
    [Header("기본 세팅")]
    [SerializeField] protected string _uiName;
    public string UIName => _uiName;

    //열릴 때
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    //닫힐 때
    public virtual void Close()
    {
        Destroy(gameObject);
    }

    //뒤로가기 버튼용
    public virtual void OnBackButtonPressed()
    {
        UIManager.Instance.CloseTopPopup();
    }
}
