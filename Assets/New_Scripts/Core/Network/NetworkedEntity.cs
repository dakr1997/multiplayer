// File: Scripts/Core/Network/NetworkedEntity.cs
using UnityEngine;
using Unity.Netcode;
using System;
using Core.Interfaces;

/// <summary>
/// Base class for all networked game entities.
/// Provides common functionality for health, damage, and network synchronization.
/// </summary>
public abstract class NetworkedEntity : NetworkBehaviour, IDamageable
{
    [Header("Entity Settings")]
    [SerializeField] protected float _maxHealth = 100f;
    [SerializeField] protected GameObject _deathEffectPrefab;
    
    // Network variable for health with consistent permissions
    protected NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    // Events with consistent pattern
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;
    
    // IDamageable implementation
    public float CurrentHealth => _currentHealth.Value;
    public float MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth.Value > 0;
    
    // Consistent network lifecycle hooks
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            InitializeServer();
        }
        
        if (IsClient)
        {
            InitializeClient();
        }
        
        // Register this once for everyone
        _currentHealth.OnValueChanged += HandleHealthChanged;
    }
    
    // Clear lifecycle separation
    protected virtual void InitializeServer()
    {
        _currentHealth.Value = _maxHealth;
    }
    
    protected virtual void InitializeClient()
    {
        // Client-specific initialization
    }
    
    public override void OnNetworkDespawn()
    {
        // Clean up event subscription
        _currentHealth.OnValueChanged -= HandleHealthChanged;
        base.OnNetworkDespawn();
    }
    
    // Update pattern with clear separation
    protected virtual void Update()
    {
        if (IsServer) ServerUpdate();
        if (IsClient) ClientUpdate();
    }
    
    protected virtual void ServerUpdate() { }
    
    protected virtual void ClientUpdate() { }
    
    // IDamageable implementation
    public virtual void TakeDamage(float amount, string source = null)
    {
        if (!IsServer || !IsAlive) return;
        
        float newHealth = Mathf.Clamp(_currentHealth.Value - amount, 0, _maxHealth);
        _currentHealth.Value = newHealth;
        
        Debug.Log($"{gameObject.name} took {amount} damage from {source}. Health: {_currentHealth.Value}/{_maxHealth}");
        
        if (newHealth <= 0)
        {
            Die();
        }
    }
    
    public virtual void Heal(float amount)
    {
        if (!IsServer || !IsAlive) return;
        
        float newHealth = Mathf.Clamp(_currentHealth.Value + amount, 0, _maxHealth);
        _currentHealth.Value = newHealth;
    }
    
    // Event handlers
    protected virtual void HandleHealthChanged(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(newValue, _maxHealth);
    }
    
    // Death handling
    protected virtual void Die()
    {
        if (!IsServer) return;
        
        Debug.Log($"{gameObject.name} died.");
        
        // Trigger event first to allow listeners to react
        OnDied?.Invoke();
        
        // Visual effects for all clients
        PlayDeathEffectsClientRpc();
        
        // Handle object cleanup
        HandleDeathCleanup();
    }
    
    // Override in derived classes for specific cleanup
    protected virtual void HandleDeathCleanup()
    {
        // Default implementation: destroy after delay
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            StartCoroutine(DestroyAfterDelay(1.0f));
        }
    }
    
    protected System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
    
    [ClientRpc]
    protected virtual void PlayDeathEffectsClientRpc()
    {
        if (_deathEffectPrefab != null)
        {
            Instantiate(_deathEffectPrefab, transform.position, transform.rotation);
        }
    }
}