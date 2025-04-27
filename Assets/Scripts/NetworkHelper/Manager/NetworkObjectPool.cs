using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using System.Collections.Generic;
public class NetworkObjectPool : NetworkBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public NetworkObject prefab;
        public int defaultCapacity = 10;
        public int maxSize = 100;
    }

    [SerializeField] private PoolConfig[] poolConfigs;
    private Dictionary<NetworkObject, IObjectPool<NetworkObject>> pools;

    private void Awake()
    {
        pools = new Dictionary<NetworkObject, IObjectPool<NetworkObject>>();
        
        foreach (var config in poolConfigs)
        {
            var pool = new ObjectPool<NetworkObject>(
                () => CreatePooledObject(config.prefab),
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                true,
                config.defaultCapacity,
                config.maxSize);

            pools.Add(config.prefab, pool);
        }
    }

    private NetworkObject CreatePooledObject(NetworkObject prefab)
    {
        var instance = Instantiate(prefab);
        instance.GetComponent<PoolableNetworkObject>()?.SetPool(this, prefab);
        return instance;
    }

    private void OnTakeFromPool(NetworkObject networkObject)
    {
        networkObject.gameObject.SetActive(true);
    }

    private void OnReturnedToPool(NetworkObject networkObject)
    {
        networkObject.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(NetworkObject networkObject)
    {
        Destroy(networkObject.gameObject);
    }

    public NetworkObject Get(NetworkObject prefab)
    {
        if (pools.TryGetValue(prefab, out var pool))
        {
            return pool.Get();
        }
        return null;
    }

    public void Release(NetworkObject prefab, NetworkObject instance)
    {
        if (pools.TryGetValue(prefab, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Destroy(instance.gameObject);
        }
    }
}