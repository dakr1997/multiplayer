using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PlayerExperience : MonoBehaviour
{
    public int currentLevel = 1;
    public float currentEXP = 0f;
    public float expToNextLevel = 100f;
    public GameObject floatinExpTextPrefab;
    private Slider expSlider;
    private GameObject hudContainer;
    private TextMeshProUGUI levelText;
    public void SetHUDReference(GameObject hud)
    {
        hudContainer = hud;
        InitializeExpBar();
        InitializeLevelText();
    }

    private void InitializeExpBar()
    {
        if (hudContainer == null) return;

        expSlider = hudContainer.transform.Find("ExperienceBar")?.GetComponent<Slider>();
        if (expSlider == null)
        {
            Debug.LogError("EXPBar Slider not found!");
            return;
        }

        expSlider.maxValue = expToNextLevel;
        expSlider.value = currentEXP;
    }

    private void InitializeLevelText()
    {
        if (hudContainer == null) return;

        levelText = hudContainer.transform.Find("PlayerLevel")?.GetComponent<TextMeshProUGUI>();
        if (levelText == null)
        {
            Debug.LogError("PlayerLevel TextMeshProUGUI not found!");
            return;
        }

        levelText.text = $"{currentLevel}";
    }

    public void GainEXP(float amount)
    {
        currentEXP += amount;
        ShowFloatingText($"+{amount} EXP");

        Debug.Log($"Gained {amount} EXP. Total: {currentEXP}/{expToNextLevel}");

        if (currentEXP >= expToNextLevel)
        {
            LevelUp();
        }

        if (expSlider != null)
        {
            expSlider.value = currentEXP;
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        currentEXP -= expToNextLevel;
        expToNextLevel *= 1.25f;

        Debug.Log($"Leveled Up! New Level: {currentLevel}");

        if (expSlider != null)
        {
            expSlider.maxValue = expToNextLevel;
            expSlider.value = currentEXP;
        }

        if (levelText != null)
        {
            levelText.text = $"{currentLevel}";
        }
    }

        private void ShowFloatingText(string message)
    {
        if (floatinExpTextPrefab == null || hudContainer == null) return;

        GameObject textObj = Instantiate(floatinExpTextPrefab, hudContainer.transform);
        textObj.transform.localPosition = new Vector3(0, 50, 0); // Offset for visibility
        FloatingText floatingText = textObj.GetComponent<FloatingText>();
        floatingText?.SetText(message);
    }
}
