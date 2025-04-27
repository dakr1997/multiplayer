using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using System.Collections.Generic;
using System.Collections;
public abstract class PoolableNetworkObject : NetworkBehaviour
{
    private NetworkObjectPool pool;
    private NetworkObject prefab;

    public void SetPool(NetworkObjectPool pool, NetworkObject prefab)
    {
        this.pool = pool;
        this.prefab = prefab;
    }

    public override void OnNetworkDespawn()
    {
        if (pool != null)
        {
            pool.Release(prefab, NetworkObject);
        }
        
        base.OnNetworkDespawn();
    }

    protected void ReturnToPool(float delay = 0f)
    {
        if (delay <= 0f)
        {
            NetworkObject.Despawn();
        }
        else
        {
            StartCoroutine(DelayedDespawn(delay));
        }
    }

    private IEnumerator DelayedDespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkObject.Despawn();
    }
}