using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerExperience : NetworkBehaviour
{
    public event Action<float, float, int> OnExpChanged;
    
    [SerializeField] private float expToNextLevel = 100f;
    [SerializeField] private float levelUpMultiplier = 1.25f;

    public float CurrentExp => currentEXP.Value;
    public float MaxExp => expToNextLevel;
    public int CurrentLevel => currentLevel.Value;
    
    private NetworkVariable<float> currentEXP = new NetworkVariable<float>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> currentLevel = new NetworkVariable<int>(
        1,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        currentEXP.OnValueChanged += HandleExpChanged;
        currentLevel.OnValueChanged += HandleLevelChanged;
        UpdateExp();
    }

    public override void OnNetworkDespawn()
    {
        currentEXP.OnValueChanged -= HandleExpChanged;
        currentLevel.OnValueChanged -= HandleLevelChanged;
    }

    public void AddXP(int amount)
    {
        if (!IsServer) return;
        
        currentEXP.Value += amount;
        
        if (currentEXP.Value >= expToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentEXP.Value -= expToNextLevel;
        expToNextLevel *= levelUpMultiplier;
        currentLevel.Value++;
    }

    private void HandleExpChanged(float oldVal, float newVal)
    {
        UpdateExp();
    }

    private void HandleLevelChanged(int oldVal, int newVal)
    {
        UpdateExp();
    }

    private void UpdateExp()
    {
        OnExpChanged?.Invoke(currentEXP.Value, expToNextLevel, currentLevel.Value);
        Debug.Log($"EXP Updated - Client {OwnerClientId}: {currentEXP.Value}/{expToNextLevel} LVL:{currentLevel.Value}");
    }
}