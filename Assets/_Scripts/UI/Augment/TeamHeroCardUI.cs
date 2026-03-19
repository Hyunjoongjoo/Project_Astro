using UnityEngine;
using UnityEngine.UI;

public class TeamHeroCardUI : MonoBehaviour
{
    [SerializeField] private Image _iconImg;
    [SerializeField] private HeroIconDataSO _heroIcons;

    public void Setup(string heroId)
    {
        if (string.IsNullOrEmpty(heroId) || _heroIcons == null)
        {
            _iconImg.gameObject.SetActive(false);
            return;
        }

        _iconImg.sprite = _heroIcons.GetIcon(heroId);
        _iconImg.gameObject.SetActive(true);
    }
}
