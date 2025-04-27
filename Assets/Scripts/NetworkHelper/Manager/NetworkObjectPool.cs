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
        [Tooltip("Pre-spawn this many objects when the game starts")]
        public int prewarmCount = 0;
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

            // Prewarm the pool
            if (config.prewarmCount > 0 && IsServer)
            {
                PrewarmPool(pool, config.prewarmCount, config.prefab);
            }
        }
    }

    private void PrewarmPool(IObjectPool<NetworkObject> pool, int count, NetworkObject prefab)
    {
        var prewarmObjects = new List<NetworkObject>();
        for (int i = 0; i < count; i++)
        {
            var obj = pool.Get();
            prewarmObjects.Add(obj);
        }
        
        // Return them all to pool
        foreach (var obj in prewarmObjects)
        {
            pool.Release(obj);
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
        
        Debug.LogError($"No pool found for prefab: {prefab.name}");
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
            Debug.LogWarning($"No pool found for prefab: {prefab.name}, destroying instance");
            Destroy(instance.gameObject);
        }
    }
}