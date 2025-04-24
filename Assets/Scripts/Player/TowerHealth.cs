using UnityEngine;
using Unity.Netcode;
using System;

public class TowerHealth : NetworkBehaviour
{
    public event Action<float, float> OnHealthChanged;

    private NetworkVariable<float> currentHP = new NetworkVariable<float>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> maxHP = new NetworkVariable<int>(
        100,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    [SerializeField] private float regenRate = 5f; // HP per second
    [SerializeField] private int damageAmount = 10; // Damage per keypress

    public float CurrentHP => currentHP.Value;
    public float MaxHP => maxHP.Value;

    public override void OnNetworkSpawn()
    {
        currentHP.OnValueChanged += HandleHPChanged;

        if (IsServer)
        {
            Debug.Log($"[TowerHealth] Initialized HP for Tower {OwnerClientId}");
            currentHP.Value = maxHP.Value;
        }

        UpdateHP();
    }

    private void Update()
    {
        if (IsServer)
        {
            // Regenerate HP over time
            if (currentHP.Value < maxHP.Value)
            {
                float regenAmount = regenRate * Time.deltaTime;
                currentHP.Value = Mathf.Min(currentHP.Value + regenAmount, maxHP.Value);
            }
        }
    }

    [ServerRpc]
    private void RequestDamageServerRpc(int amount)
    {
        Debug.Log($"[TowerHealth] Server received damage request: -{amount} HP from client {OwnerClientId}");
        DamageTower(amount);
    }

    private void DamageTower(int amount)
    {
        if (!IsServer) return;

        currentHP.Value = Mathf.Max(currentHP.Value - amount, 0);
        Debug.Log($"[TowerHealth] Damaged tower by {amount}. New HP: {currentHP.Value}");
    }
    

    public void AddHP(int amount)
    {
        if (!IsServer) 
        {
            Debug.LogWarning($"[TowerHealth] Client {OwnerClientId} attempted to add HP.");
            return;
        }

        currentHP.Value = Mathf.Min(currentHP.Value + amount, maxHP.Value);
        Debug.Log($"[TowerHealth] Added {amount} HP. New HP: {currentHP.Value}");
    }

    public void SetHP(int value)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TowerHealth] Only the server can set HP directly.");
            return;
        }

        currentHP.Value = Mathf.Clamp(value, 0, maxHP.Value);

        // Optional: Trigger the update event immediately on the server
        UpdateHP();

        Debug.Log($"[TowerHealth] HP set to {currentHP.Value}");
    }

    private void HandleHPChanged(float oldVal, float newVal)
    {
        Debug.Log($"[TowerHealth] HP changed from {oldVal} to {newVal} for client {OwnerClientId}");
        UpdateHP();
    }

    private void UpdateHP()
    {
        OnHealthChanged?.Invoke(currentHP.Value, maxHP.Value);
    }
}
