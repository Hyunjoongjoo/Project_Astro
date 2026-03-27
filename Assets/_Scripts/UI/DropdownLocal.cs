using TMPro;
using UnityEngine;

public class DropdownLocal : MonoBehaviour
{
    private TMP_Dropdown _dropdown;


    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        TableManager.Instance.OnLanguageChanged += UpdateDropdownTxt;
        UpdateDropdownTxt();
    }
    private void OnDisable()
    {
        TableManager.Instance.OnLanguageChanged -= UpdateDropdownTxt;
    }

    private void UpdateDropdownTxt()
    {
        _dropdown.options[0].text = TableManager.Instance.GetString("btn_hero_name");
        _dropdown.options[1].text = TableManager.Instance.GetString("btn_hero_level");
        _dropdown.options[2].text = TableManager.Instance.GetString("btn_hero_role");

        _dropdown.RefreshShownValue();
    }
}
