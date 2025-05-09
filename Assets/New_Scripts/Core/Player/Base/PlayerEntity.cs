// Location: Core/Player/Base/PlayerEntity.cs
using UnityEngine;
using Unity.Netcode;
using Core.Components;
using Core.Entities;
using Core.Player.Components; // This already includes PlayerMovement

namespace Core.Player.Base
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PlayerExperience))]
    [RequireComponent(typeof(Core.Player.Components.PlayerMovement))] // Full namespace is Core.Player.Components.PlayerMovement
    public class PlayerEntity : GameEntity
    {
        // Component references
        public PlayerExperience Experience { get; private set; }
        public PlayerMovement Movement { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            
            // Cache components
            Experience = GetComponent<PlayerExperience>();
            Movement = GetComponent<PlayerMovement>();
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsOwner && IsClient)
            {
                // Initialize client-specific systems
                InitializeClientSystems();
            }
        }
        
        private void InitializeClientSystems()
        {
            // Get or add client handler
            var clientHandler = GetOrAddComponent<PlayerClientHandler>();
            clientHandler.Initialize(this);
        }
    }
}