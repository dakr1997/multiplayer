using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
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

    protected async void ReturnToPool(float delay = 0f)
    {
        if (delay > 0f)
        {
            await Task.Delay((int)(delay * 1000)); // delay expects milliseconds
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(false);
        }
    }

    private IEnumerator DelayedDespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkObject.Despawn(false);
    }
}