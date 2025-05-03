using UnityEngine;
using Unity.Netcode;
using System;

public class HealthComponent : NetworkBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 2f;

    // Network variable for health
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Events
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    // Properties
    public float CurrentHealth => currentHealth.Value;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth.Value > 0;
    public float HealthPercent => currentHealth.Value / maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += HandleHealthChanged;
    }

    public void TakeDamage(float amount, string source = null)
    {
        if (!IsServer || !IsAlive) return;

        float newHealth = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);
        currentHealth.Value = newHealth;

        Debug.Log($"{gameObject.name} took {amount} damage from {source}. Health: {currentHealth.Value}/{maxHealth}");

        if (newHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (!IsServer || !IsAlive) return;

        float newHealth = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
        currentHealth.Value = newHealth;

        Debug.Log($"{gameObject.name} healed for {amount}. Health: {currentHealth.Value}/{maxHealth}");
    }

    private void HandleHealthChanged(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(newValue, maxHealth);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");

        // Play death effects
        PlayDeathEffectsClientRpc();

        // Notify listeners
        OnDied?.Invoke();

        // Handle object destruction
        if (destroyOnDeath && IsServer)
        {
            if (gameObject.TryGetComponent<PoolableNetworkObject>(out var poolable))
            {
                poolable.ReturnToPool(destroyDelay);
            }
            else if (NetworkObject != null)
            {
                StartCoroutine(DestroyAfterDelay(destroyDelay));
            }
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    private void PlayDeathEffectsClientRpc()
    {
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }

        // Optionally hide renderers
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= HandleHealthChanged;
        base.OnNetworkDespawn();
    }
}