using UnityEngine;
using UnityEngine.EventSystems;

public class SoundSetBtn : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private UISfxList _selectSound;
    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlayUISfx(_selectSound);
    }
}
