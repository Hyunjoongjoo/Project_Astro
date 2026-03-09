using UnityEngine;
using UnityEngine.UI;

public class HpBarItem : MonoBehaviour
{
    [SerializeField] private Image _fillImg;
    [SerializeField] private GameObject _heroMark;
    [SerializeField] private Sprite _blueTeamSpr;
    [SerializeField] private Sprite _redTeamSpr;
    [SerializeField] private float _showDuration = 2f;
    [SerializeField] private Vector3 _offset = new Vector3(0, 2.5f, 0);

    private Transform _target;
    private Camera _mainCam;
    private float _timer;
    private string _poolTag;

    public void Setup(Transform target,Team team,float hpRatio,string poolTag,bool isHero)
    {
        _target = target;
        _mainCam = Camera.main;
        _poolTag = poolTag;

        _fillImg.sprite = (team == Team.Blue) ? _blueTeamSpr : _redTeamSpr;
        if (_heroMark != null)
        {
            _heroMark.SetActive(isHero); //영웅이면 영웅아이콘 활성화
        }
        UpdateHp(hpRatio);

        _timer = _showDuration;
    }

    public void UpdateHp(float ratio)
    {
        _fillImg.fillAmount = ratio;
        _timer = _showDuration;
    }

    private void LateUpdate()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            Return();
            return;
        }

        // 위치 추적
        Vector3 screenPos = _mainCam.WorldToScreenPoint(_target.position + _offset);
        transform.position = screenPos;

        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            Return();
        }
    }

    private void Return()
    {
        PoolManager.Instance.ReturnToPool(_poolTag, gameObject);
    }
}
