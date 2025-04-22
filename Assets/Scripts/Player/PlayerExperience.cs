using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class PlayerExperience : NetworkBehaviour
{
    public int currentLevel = 1;
    public float currentEXP = 0f;
    public float expToNextLevel = 100f;
    public GameObject floatingExpTextPrefab;
    private Slider expSlider;
    private GameObject hudContainer;
    private TextMeshProUGUI levelText;

    // Set HUD reference for the player
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

    // This is called on the server to add EXP
    [ServerRpc(RequireOwnership = false)]
    public void GainEXPServerRpc(float amount)
    {
        // Make sure only the server handles the EXP gain
        GainEXP(amount);

        // Notify all clients about the EXP update
        NotifyClientsOfExpUpdateClientRpc(currentEXP, currentLevel);
    }

    // Actual logic to increase experience and level up
    private void GainEXP(float amount)
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

    // Handle leveling up logic
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

    // Display floating text when gaining experience
    private void ShowFloatingText(string message)
    {
        if (floatingExpTextPrefab == null || hudContainer == null) return;

        GameObject textObj = Instantiate(floatingExpTextPrefab, hudContainer.transform);
        textObj.transform.localPosition = new Vector3(0, 50, 0); // Offset for visibility
        FloatingText floatingText = textObj.GetComponent<FloatingText>();
        floatingText?.SetText(message);
    }

    // ClientRPC to notify all clients to update their EXP UI
    [ClientRpc]
    private void NotifyClientsOfExpUpdateClientRpc(float updatedExp, int updatedLevel)
    {
        // Update the EXP UI for all clients
        if (expSlider != null)
        {
            expSlider.value = updatedExp;
            expSlider.maxValue = expToNextLevel;
        }

        if (levelText != null)
        {
            levelText.text = $"{updatedLevel}";
        }
    }
}
