// Location: Core/Player/UI/PlayerHUDController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Components;
using Core.Towers.MainTower;

namespace Core.Player.UI
{
    public class PlayerHUDController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider expBar;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider towerHealthBar;

        [Header("Settings")]
        [SerializeField] private Vector3 floatingTextOffset = new Vector3(0, 50, 0);

        public void Initialize(HealthComponent health = null, Components.PlayerExperience exp = null, MainTowerHP tower = null)
        {
            if (health != null)
            {
                health.OnHealthChanged += UpdateHealth;
                UpdateHealth(health.CurrentHealth, health.MaxHealth);
            }

            if (exp != null)
            {
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
                // Use the tower instance provided
                tower.OnHealthChanged += (current, max) => UpdateTowerHealth(current, max);
                UpdateTowerHealth(tower.CurrentHealth, tower.MaxHealth);
            }
            else if (MainTowerHP.Instance != null)
            {
                // Try to find the tower instance through singleton
                MainTowerHP.Instance.OnHealthChanged += (current, max) => UpdateTowerHealth(current, max);
                UpdateTowerHealth(MainTowerHP.Instance.CurrentHealth, MainTowerHP.Instance.MaxHealth);
            }
            else
            {
                Debug.LogWarning("[PlayerHUDController] No tower found or provided!");
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
}