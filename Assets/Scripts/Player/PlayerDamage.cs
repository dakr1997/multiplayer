using UnityEngine;
using Unity.Netcode;

public class PlayerDamage : NetworkBehaviour
{
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float damageRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Client clicked - requesting server to deal damage.");
            RequestDealDamageServerRpc();
        }
    }

    [ServerRpc]
    private void RequestDealDamageServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("Server received damage request.");
        DealDamageToEnemies();
    }

    private void DealDamageToEnemies()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, damageRadius, enemyLayer);


        foreach (var hitCollider in hitColliders)
        {
            DamageHelper.ApplyDamage(hitCollider.gameObject, damageAmount, "PlayerAttack");
            Debug.Log("Server dealt " + damageAmount + " damage to " + hitCollider.gameObject.name);
        }
    }
}
