using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerExperience), typeof(PlayerMovement))]
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
        if (IsOwner)
        {
            var handler = GetComponent<PlayerClientHandler>();
            handler?.Initialize(this);
        }
    }
}
