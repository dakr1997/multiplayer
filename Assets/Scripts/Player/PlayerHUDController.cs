using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerHUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider expBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider towerHealthBar;
    [SerializeField] private Transform floatingTextRoot;
    [SerializeField] private FloatingText floatingTextPrefab;

    [Header("Settings")]
    [SerializeField] private Vector3 floatingTextOffset = new Vector3(0, 50, 0);
    [SerializeField] private int initialPoolSize = 5;

    private Queue<FloatingText> textPool = new Queue<FloatingText>();

    private void Awake()
    {
        if (floatingTextPrefab != null && floatingTextRoot != null)
        {
            InitializeTextPool();
        }
    }

    public void Initialize(PlayerHealth health, PlayerExperience exp, TowerHealth tower)
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
            tower.OnHealthChanged += UpdateTowerHealth;
            UpdateTowerHealth(tower.CurrentHealth, tower.MaxHealth);
        }
    }

    private void InitializeTextPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewTextObject();
        }
    }

    private FloatingText CreateNewTextObject()
    {
        FloatingText text = Instantiate(floatingTextPrefab, floatingTextRoot);
        text.gameObject.SetActive(false);
        text.OnTextComplete += () => ReturnTextToPool(text);
        textPool.Enqueue(text);
        return text;
    }

    private void ReturnTextToPool(FloatingText text)
    {
        text.gameObject.SetActive(false);
        textPool.Enqueue(text);
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

    public void ShowFloatingText(string message)
    {
        if (floatingTextPrefab == null || floatingTextRoot == null) return;

        FloatingText text = GetPooledText();
        text.transform.localPosition = floatingTextOffset;
        text.SetText(message);
        text.gameObject.SetActive(true);
    }

    private FloatingText GetPooledText()
    {
        if (textPool.Count == 0)
        {
            CreateNewTextObject();
        }

        FloatingText text = textPool.Dequeue();
        return text ?? CreateNewTextObject();
    }

    private void OnDestroy()
    {
        foreach (var text in textPool)
        {
            if (text != null) Destroy(text.gameObject);
        }
        textPool.Clear();
    }
}