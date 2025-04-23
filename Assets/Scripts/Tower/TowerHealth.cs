using UnityEngine;
using System;

public class TowerHealth : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged;

    [SerializeField] private float maxHealth = 500f;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealth();
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealth();
        
        if (currentHealth <= 0) DestroyTower();
    }

    private void UpdateHealth()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void DestroyTower()
    {
        Debug.Log("Tower destroyed!");
    }
}