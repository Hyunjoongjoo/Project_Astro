using UnityEngine;

public class PingIcon : MonoBehaviour
{
    [SerializeField] private float _duration = 2f;
    void Start()
    {
        Destroy(gameObject,_duration);
    }

}
