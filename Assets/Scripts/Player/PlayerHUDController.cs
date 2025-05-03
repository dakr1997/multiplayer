using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerHUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider expBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider towerHealthBar;

    [Header("Settings")]
    [SerializeField] private Vector3 floatingTextOffset = new Vector3(0, 50, 0);

    public void Initialize(PlayerHealth health, PlayerExperience exp, MainTowerHP tower)
    {
        if (health != null)
        {
            health.OnHealthChanged += UpdateHealth;
            UpdateHealth(health.CurrentHealth, health.MaxHealth);
        }

        if (exp != null)
        {
            // Use the public properties we just added
            UpdateExp(exp.CurrentExp, exp.MaxExp);
            UpdateLevel(exp.CurrentLevel);
            
            exp.OnExpChanged += (current, max, level) => 
            {
                UpdateExp(current, max);
                UpdateLevel(level);
            };
        }

        if (tower != null)
        {
            // Listen directly to NetworkVariable changes
            tower.OnHealthChanged += (current, max) => UpdateTowerHealth(current, max);
            UpdateTowerHealth(tower.CurrentHealth, tower.MaxHealth);
        }
    }

    public void UpdateHealth(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = Mathf.Clamp(current, 0, max);
        }
    }

    public void UpdateExp(float current, float max)
    {   
        if (expBar != null)
        {
            expBar.maxValue = max;
            expBar.value = Mathf.Clamp(current, 0, max);
        }
    }

    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = level.ToString();
        }
    }

    public void UpdateTowerHealth(float current, float max)
    {
        if (towerHealthBar != null)
        {
            towerHealthBar.maxValue = max;
            towerHealthBar.value = Mathf.Clamp(current, 0, max);
        }
    }
}