using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged;

    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private PlayerHUDController hud; // Reference to the PlayerHUDController
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealth();
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealth();
        
        if (currentHealth <= 0) Die();
    }

    private void UpdateHealth()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player died!");
    }

    public void SetHUD(PlayerHUDController hud)
    {
        this.hud = hud;
        hud?.UpdateHealth(currentHealth, maxHealth);
    }
}