using UnityEngine;
using System.Collections.Generic;

public class DeployZone : MonoBehaviour
{
    [SerializeField] private Transform[] _zoneVisuals;// 배치 가능 영역

    private void Awake()
    {
        HideZones();
    }

    public void ShowZones(List<DeployZoneData> zones)
    {
        HideZones();

        if (zones == null || _zoneVisuals == null || _zoneVisuals.Length == 0)
        {
            return;
        }

        int count = Mathf.Min(zones.Count, _zoneVisuals.Length);

        for (int index = 0; index < count; index++)
        {
            Transform zoneVisual = _zoneVisuals[index];
            DeployZoneData zoneData = zones[index];

            if (zoneVisual == null)
            {
                continue;
            }

            zoneVisual.gameObject.SetActive(true);

            zoneVisual.position = new Vector3(zoneData.Center.x, 0f, zoneData.Center.z);

            zoneVisual.localScale = new Vector3(zoneData.Size.x, zoneData.Size.y, 1f);
        }
    }

    public void HideZones()
    {
        if (_zoneVisuals == null)
        {
            return;
        }

        for (int index = 0; index < _zoneVisuals.Length; index++)
        {
            if (_zoneVisuals[index] != null)
            {
                _zoneVisuals[index].gameObject.SetActive(false);
            }
        }
    }

}
