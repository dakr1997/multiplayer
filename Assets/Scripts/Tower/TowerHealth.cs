using UnityEngine;
using Unity.Netcode;
using System;

public class TowerHealth : NetworkBehaviour, IDamageable
{
    public float maxHealth = 100f;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server);

    public event Action<float, float> OnHealthChanged;

    public bool IsAlive { get; private set; } = true;

    [Header("Debug Tick Damage")]
    public bool enableTickDamage = true;
    public float tickDamageInterval = 2f;
    public float tickDamageAmount = 5f;
    private float tickTimer;

    private void Awake()
    {
        currentHealth.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        // Trigger UI update for clients when they connect
        NotifyHealthChange(currentHealth.Value, maxHealth);
    }

    private void Update()
    {
        if (!IsServer || !IsAlive || !enableTickDamage) return;

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickDamageInterval)
        {
            tickTimer = 0f;
            TakeDamage(tickDamageAmount);
        }
    }

    public void TakeDamage(float damage, string source = null)
    {
        if (!IsServer || !IsAlive) return;

        currentHealth.Value -= damage;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);
        Debug.Log($"[Tower] Took {damage} damage. Health: {currentHealth.Value}/{maxHealth}");

        if (currentHealth.Value <= 0f)
        {
            HandleTowerDestruction();
        }
    }

    private void HandleHealthChanged(float oldVal, float newVal)
    {
        NotifyHealthChange(newVal, maxHealth);
    }

    private void NotifyHealthChange(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    private void HandleTowerDestruction()
    {
        IsAlive = false;
        Debug.Log("[Tower] Destroyed.");
    }
}
