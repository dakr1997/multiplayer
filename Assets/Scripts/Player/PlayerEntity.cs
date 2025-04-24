using UnityEngine;
using Unity.Netcode;

public class PlayerEntity : NetworkBehaviour
{
    public PlayerHealth HealthComponent { get; private set; }
    public PlayerExperience ExperienceComponent { get; private set; }
    public PlayerMovement MovementComponent { get; private set; }
    public TowerHealth TowerHealthComponent { get; private set; } // Reference to the tower health component
    private void Awake()
    {
        HealthComponent = GetComponent<PlayerHealth>();
        ExperienceComponent = GetComponent<PlayerExperience>();
        MovementComponent = GetComponent<PlayerMovement>();
        TowerHealthComponent = GetComponent<TowerHealth>(); // Ensure this is the same as the TowerHealth for syncing health
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && IsClient)
        {
            Debug.Log("Player spawned on client: " + OwnerClientId);
            var handler = GetComponent<PlayerClientHandler>();
            handler?.Initialize(this);  // Initialize the handler to hook up the player
        }
    }


    

    // You can also request tower info at any time from the server
}
