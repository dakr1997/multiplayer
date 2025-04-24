using Unity.Netcode;
using UnityEngine;

public class MainTowerHP : NetworkBehaviour, IDamageable
{
    public static MainTowerHP Instance { get; private set; }
    public const float MaxHP = 100f;
    
    public NetworkVariable<float> CurrentHP = new NetworkVariable<float>(
        MaxHP,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    // Implement IDamageable.IsAlive
    public bool IsAlive => CurrentHP.Value > 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Implement IDamageable.TakeDamage
    public void TakeDamage(float amount, string source = null)
    {
        if (!IsServer) return;
        
        CurrentHP.Value = Mathf.Clamp(CurrentHP.Value - amount, 0, MaxHP);
        Debug.Log($"[Tower] Took {amount} damage from {source}. New HP: {CurrentHP.Value}");

        if (!IsAlive)
        {
            HandleTowerDestroyed();
        }
    }

    private void HandleTowerDestroyed()
    {
        Debug.Log("Tower has been destroyed!");
        // Add destruction effects/respawn logic here
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSetHPServerRpc(float amount)
    {
        CurrentHP.Value = Mathf.Clamp(amount, 0, MaxHP);
    }
}