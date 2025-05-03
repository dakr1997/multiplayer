using UnityEngine;
using Unity.Netcode;
using System.Collections;

public abstract class PoolableNetworkObject : NetworkBehaviour, IPoolable
{
    private NetworkObjectPool pool;
    private NetworkObject prefab;
    private Coroutine returnToPoolCoroutine;

    public void SetPool(NetworkObjectPool pool, NetworkObject prefab)
    {
        this.pool = pool;
        this.prefab = prefab;
    }

    public virtual void OnSpawn()
    {
        // Reset state when taken from pool
        if (returnToPoolCoroutine != null)
        {
            StopCoroutine(returnToPoolCoroutine);
            returnToPoolCoroutine = null;
        }
    }

    public virtual void OnDespawn()
    {
        // Clean up when returned to pool
    }

    public void ReturnToPool(float delay = 0f)
    {
        if (delay <= 0f)
        {
            ReturnToPoolImmediate();
        }
        else if (IsServer && gameObject.activeInHierarchy)
        {
            if (returnToPoolCoroutine != null)
            {
                StopCoroutine(returnToPoolCoroutine);
            }
            returnToPoolCoroutine = StartCoroutine(ReturnToPoolDelayed(delay));
        }
    }

    private void ReturnToPoolImmediate()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(false);
        }
    }

    private IEnumerator ReturnToPoolDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPoolImmediate();
        returnToPoolCoroutine = null;
    }

    public override void OnNetworkDespawn()
    {
        if (pool != null)
        {
            pool.Release(prefab, NetworkObject);
        }
        
        base.OnNetworkDespawn();
    }
}