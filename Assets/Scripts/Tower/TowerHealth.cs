using UnityEngine;
using System;

public class TowerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public event Action<float, float> OnHealthChanged; // current, max

    private void Start()
    {
        currentHealth = maxHealth;
        NotifyHealthChange();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"Tower took {damage} damage. Health: {currentHealth}/{maxHealth}");
        NotifyHealthChange();

        if (currentHealth <= 0)
        {
            HandleTowerDestruction();
        }
    }

    private void NotifyHealthChange()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void HandleTowerDestruction()
    {
        Debug.Log("The tower has been destroyed!");
        // Add death/destroy effects or game over logic here
    }
}
