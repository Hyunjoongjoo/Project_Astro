using UnityEngine;
using System.Collections.Generic;

public class DeployZone : MonoBehaviour
{
    [SerializeField] private Transform[] _zoneBorders; // 테두리
    [SerializeField] private Transform[] _zoneFills;   // 배치 가능 영역 내부
    [SerializeField] private float _fillInset = 0.2f;
    [SerializeField] private float _fillYOffset = 0.01f;

    private void Awake()
    {
        HideZones();
    }

    public void ShowZones(List<DeployZoneData> zones)
    {
        HideZones();

        if (zones == null || _zoneBorders == null || _zoneFills == null)
        {
            return;
        }

        int count = Mathf.Min(zones.Count, Mathf.Min(_zoneBorders.Length, _zoneFills.Length));

        for (int index = 0; index < count; index++)
        {
            Transform border = _zoneBorders[index];
            Transform fill = _zoneFills[index];
            DeployZoneData zoneData = zones[index];

            if (border != null)
            {
                border.gameObject.SetActive(true);
                border.position = zoneData.Center;
                border.localScale = new Vector3(zoneData.Size.x, zoneData.Size.y, 1f);
            }

            if (fill != null)
            {
                float fillWidth = Mathf.Max(0f, zoneData.Size.x - (_fillInset * 2f));
                float fillHeight = Mathf.Max(0f, zoneData.Size.y - (_fillInset * 2f));

                fill.gameObject.SetActive(true);
                fill.position = zoneData.Center + new Vector3(0f, _fillYOffset, 0f);
                fill.localScale = new Vector3(fillWidth, fillHeight, 1f);
            }
        }
    }

    public void HideZones()
    {
        {
            for (int index = 0; index < _zoneBorders.Length; index++)
            {
                if (_zoneBorders[index] != null)
                {
                    _zoneBorders[index].gameObject.SetActive(false);
                }
            }
        }

        if (_zoneFills != null)
        {
            for (int index = 0; index < _zoneFills.Length; index++)
            {
                if (_zoneFills[index] != null)
                {
                    _zoneFills[index].gameObject.SetActive(false);
                }
            }
        }
    }

}
