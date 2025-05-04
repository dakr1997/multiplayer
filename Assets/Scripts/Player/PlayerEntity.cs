using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerExperience))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerEntity : NetworkBehaviour
{
    // Component references
    public HealthComponent Health { get; private set; }
    public PlayerExperience Experience { get; private set; }
    public PlayerMovement Movement { get; private set; }
    
    private void Awake()
    {
        // Cache components
        Health = GetComponent<HealthComponent>();
        Experience = GetComponent<PlayerExperience>();
        Movement = GetComponent<PlayerMovement>();
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner && IsClient)
        {
            Debug.Log($"Player {OwnerClientId} spawned - initializing client handler");
            
            // Find or add PlayerClientHandler
            var clientHandler = GetComponent<PlayerClientHandler>();
            if (clientHandler == null)
            {
                clientHandler = gameObject.AddComponent<PlayerClientHandler>();
            }
            
            // Let the client handler do all UI and input setup
            clientHandler.Initialize(this);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        // Any cleanup code here if needed
    }
}