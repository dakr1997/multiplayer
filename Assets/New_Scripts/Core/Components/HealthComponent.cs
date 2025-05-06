// Location: Core/Components/HealthComponent.cs
using UnityEngine;
using Unity.Netcode;
using System;
using Core.Interfaces;

namespace Core.Components
{
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
        
        // Wave multipliers
        private float healthMultiplier = 1f;
        private bool hasAppliedMultiplier = false;

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;

        // Properties
        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => maxHealth * healthMultiplier;
        public bool IsAlive => currentHealth.Value > 0;
        public float HealthPercent => currentHealth.Value / MaxHealth;
        
        /// <summary>
        /// Set health multiplier for wave difficulty scaling
        /// </summary>
        public void SetHealthMultiplier(float multiplier)
        {
            if (hasAppliedMultiplier)
            {
                // If we've already applied a multiplier, just scale current health proportionally
                float healthPercent = currentHealth.Value / (maxHealth * healthMultiplier);
                healthMultiplier = Mathf.Max(1f, multiplier);
                
                if (IsServer)
                {
                    currentHealth.Value = maxHealth * healthMultiplier * healthPercent;
                }
            }
            else
            {
                // First-time application
                healthMultiplier = Mathf.Max(1f, multiplier);
                
                if (IsServer)
                {
                    currentHealth.Value = maxHealth * healthMultiplier;
                }
                
                hasAppliedMultiplier = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Initialize with multiplier if it's been set
                if (hasAppliedMultiplier)
                {
                    currentHealth.Value = maxHealth * healthMultiplier;
                }
                else
                {
                    currentHealth.Value = maxHealth;
                }
            }

            currentHealth.OnValueChanged += HandleHealthChanged;
        }

        public void TakeDamage(float amount, string source = null)
        {
            if (!IsServer || !IsAlive) return;

            float newHealth = Mathf.Clamp(currentHealth.Value - amount, 0, MaxHealth);
            currentHealth.Value = newHealth;

            Debug.Log($"{gameObject.name} took {amount} damage from {source}. Health: {currentHealth.Value}/{MaxHealth}");

            if (newHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsServer || !IsAlive) return;

            float newHealth = Mathf.Clamp(currentHealth.Value + amount, 0, MaxHealth);
            currentHealth.Value = newHealth;

            Debug.Log($"{gameObject.name} healed for {amount}. Health: {currentHealth.Value}/{MaxHealth}");
        }

        private void HandleHealthChanged(float oldValue, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, MaxHealth);
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
                // Check if we're using pooling
                if (gameObject.TryGetComponent<PoolableNetworkObject>(out var poolable))
                {
                    // Return to pool after delay
                    poolable.ReturnToPool(destroyDelay);
                }
                else if (NetworkObject != null)
                {
                    // Standard destruction if not poolable
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
}