using System;
using UnityEngine;

public class Bridge : MonoBehaviour
{
    [SerializeField] private Team _team;
    [SerializeField] private float _hp;
    private bool _isDestroyed = false;

    public Team Team => _team;

    public event Action OnDestroyed;

    public void TakeDamage(float amount)
    {
        if (_isDestroyed) return;

        _hp -= amount;
        if (_hp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (_isDestroyed)
        {
            return;
        }

        _isDestroyed = true;
        OnDestroyed?.Invoke();
        gameObject.SetActive(false);
    }
}
