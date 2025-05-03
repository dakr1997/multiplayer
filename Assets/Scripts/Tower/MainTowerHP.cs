using UnityEngine;
using Unity.Netcode;
using System;

public class MainTowerHP : NetworkBehaviour, IDamageable
{
    public static MainTowerHP Instance { get; private set; }
    
    [Header("Tower Settings")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private GameObject destructionEffectPrefab;
    [SerializeField] private float respawnTime = 10f;
    
    // Network variables
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        1000f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    // State
    private bool isDestroyed = false;
    
    // Events
    public event Action<float, float> OnHealthChanged;
    public event Action OnTowerDestroyed;
    
    // Properties
    public float CurrentHealth => currentHealth.Value;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth.Value > 0 && !isDestroyed;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    public override void OnNetworkSpawn()
    {
        // Register callbacks
        currentHealth.OnValueChanged += HandleHealthChanged;
        
        if (IsServer)
        {
            // Initialize health
            currentHealth.Value = maxHealth;
            isDestroyed = false;
        }
    }
    
    public void TakeDamage(float amount, string source = null)
    {
        if (!IsServer || isDestroyed) return;
        
        // Apply damage
        float newHealth = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);
        currentHealth.Value = newHealth;
        
        Debug.Log($"[Tower] Took {amount} damage from {source}. HP: {currentHealth.Value}/{maxHealth}");
        
        // Check for destruction
        if (newHealth <= 0 && !isDestroyed)
        {
            HandleTowerDestroyed();
        }
    }
    
    private void HandleTowerDestroyed()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        
        Debug.Log("Main tower has been destroyed!");
        
        // Play effects
        PlayDestructionEffectsClientRpc();
        
        // Notify listeners
        OnTowerDestroyed?.Invoke();
        
        // Start respawn timer
        if (respawnTime > 0)
        {
            StartCoroutine(RespawnTowerAfterDelay());
        }
    }
    
    private System.Collections.IEnumerator RespawnTowerAfterDelay()
    {
        yield return new WaitForSeconds(respawnTime);
        
        if (IsServer)
        {
            // Restore tower
            currentHealth.Value = maxHealth;
            isDestroyed = false;
            
            // Play effects
            PlayRespawnEffectsClientRpc();
        }
    }
    
    private void HandleHealthChanged(float oldVal, float newVal)
    {
        // Trigger event
        OnHealthChanged?.Invoke(newVal, maxHealth);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestSetHPServerRpc(float amount)
    {
        if (!IsServer) return;
        
        currentHealth.Value = Mathf.Clamp(amount, 0, maxHealth);
        
        // Check for destruction/restoration
        if (currentHealth.Value <= 0 && !isDestroyed)
        {
            HandleTowerDestroyed();
        }
        else if (currentHealth.Value > 0 && isDestroyed)
        {
            // Restore
            isDestroyed = false;
            PlayRespawnEffectsClientRpc();
        }
    }
    
    [ClientRpc]
    private void PlayDestructionEffectsClientRpc()
    {
        // Play destruction effects
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, transform.rotation);
        }
        
        // Hide tower
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
    }
    
    [ClientRpc]
    private void PlayRespawnEffectsClientRpc()
    {
        // Show tower
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = true;
        }
        
        // Play respawn effects
    }
    
    public override void OnNetworkDespawn()
    {
        // Unregister callbacks
        currentHealth.OnValueChanged -= HandleHealthChanged;
    }
}