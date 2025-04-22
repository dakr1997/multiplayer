using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float maxHealth = 100f; // Default value, can be overridden
    private float curHealth;
    private float healthPercentage;
    private GameObject healthBarPrefab;
    private PlayerHealthBar healthBarInstance;
    // Update is called once per frame




    // General Functions

    private void Awake()
    {
    curHealth = maxHealth;
    }

    void Start()
    {
        InitHealthBar(); // Initialize health bar
    }

    void Update()
    {
        Debug.Log($"HEALTH.CS --> Current Health: {curHealth}");
    }




    // ##### Custom Functions #####

    private void InitHealthBar()
    {
        if (healthBarPrefab != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            GameObject healthBarObject = Instantiate(healthBarPrefab, canvas.transform);
            healthBarInstance = healthBarObject.GetComponent<PlayerHealthBar>();

            healthBarInstance.Initialize((int)maxHealth); // Set max health
            UpdateHealthBar(); // Display initial full health
        }
    }
    private void UpdateHealthBar()
    {
        healthPercentage = (curHealth / maxHealth) * 100f;
        healthBarInstance?.SetHealthPercentage(healthPercentage);
    }

    public void TakeDamage(float damage)
    {
        curHealth -= damage;
        if (curHealth < 0)
        {
            curHealth = 0;
        }
        UpdateHealthBar(); // Update health bar after taking damage
    }


}
