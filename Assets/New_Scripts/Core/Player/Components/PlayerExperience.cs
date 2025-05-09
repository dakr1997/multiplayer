// Location: Core/Player/Components/PlayerExperience.cs
using UnityEngine;
using Unity.Netcode;
using System;

namespace Core.Player.Components
{
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

            if (IsServer)
            {
                // Server logs only, no need to update UI
                Debug.Log($"Player XP initialized for client {OwnerClientId}");
            }
        }

        public void AddXP(int amount)
        {
            if (!IsServer) 
            {
                Debug.LogWarning($"Client {OwnerClientId} tried to add XP directly!");
                return;
            }
            
            Debug.Log($"Adding {amount} XP to player {OwnerClientId}");
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
            Debug.Log($"Player {OwnerClientId} leveled up to {currentLevel.Value}!");
        }

        private void HandleExpChanged(float oldVal, float newVal)
        {
            Debug.Log($"XP changed from {oldVal} to {newVal} for client {OwnerClientId}");
            UpdateExp();
        }

        private void HandleLevelChanged(int oldVal, int newVal)
        {
            Debug.Log($"Level changed from {oldVal} to {newVal} for client {OwnerClientId}");
            UpdateExp();
        }

        private void UpdateExp()
        {
            OnExpChanged?.Invoke(currentEXP.Value, expToNextLevel, currentLevel.Value);
        }
        
        public override void OnNetworkDespawn()
        {
            currentEXP.OnValueChanged -= HandleExpChanged;
            currentLevel.OnValueChanged -= HandleLevelChanged;
            base.OnNetworkDespawn();
        }
    }
}