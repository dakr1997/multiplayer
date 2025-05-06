using UnityEngine;
using Unity.Netcode;
using Core.Components;
using Core.Entities;

namespace Player.Base
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PlayerExperience))]
    [RequireComponent(typeof(PlayerMovement))]
    
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