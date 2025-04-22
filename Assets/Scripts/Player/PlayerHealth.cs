using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private GameObject hudContainer;
    private Slider healthSlider;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void SetHUDReference(GameObject hud)
    {
        hudContainer = hud;
        InitializeHealthBar();
    }

    private void InitializeHealthBar()
    {
        if (hudContainer == null) return;

        healthSlider = hudContainer.transform.Find("HealthBar")?.GetComponent<Slider>();
        if (healthSlider == null)
        {
            Debug.LogError("HealthBar Slider not found!");
            return;
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - (int)damage);
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        Debug.Log($"Player took {damage} damage! Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Add death logic here
    }

    private void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
}
