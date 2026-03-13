using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubbleUI : MonoBehaviour
{
    [SerializeField] private GameObject _txtPanel;
    [SerializeField] private GameObject _emoticonPanel;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Image _emoticon;

    public void Setup(ChatMacroData data)
    {
        if (data.type == MacroType.Text)
        {
            _emoticonPanel.SetActive(false);
            _txtPanel.SetActive(true);
            _text.text = data.text;
        }
        else
        {
            _emoticonPanel.SetActive(true);
            _txtPanel.SetActive(false);
            _emoticon.sprite = data.emoticonSprite;
        }
    }
}
