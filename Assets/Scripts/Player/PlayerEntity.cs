using UnityEngine;
using Unity.Netcode;

public class PlayerEntity : NetworkBehaviour
{
    public PlayerHealth HealthComponent { get; private set; }
    public PlayerExperience ExperienceComponent { get; private set; }
    public PlayerMovement MovementComponent { get; private set; }
    private void Awake()
    {
        HealthComponent = GetComponent<PlayerHealth>();
        ExperienceComponent = GetComponent<PlayerExperience>();
        MovementComponent = GetComponent<PlayerMovement>();
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
