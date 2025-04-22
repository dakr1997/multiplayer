using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    public Slider healthSlider;

    // Initialization
    public void Initialize(int maxHealth)
    {
        healthSlider.maxValue = 100f; // Set the maximum value of the slider to 100 Percent
        healthSlider.value = maxHealth;
    }

    // Update health bar value
    public void SetHealthPercentage(float hp)
    {
        Debug.Log(string.Format("HEALTHBAR.CS --> SetHealth: {0}%", (int)hp));
        healthSlider.value = (int)hp;
    }
}
