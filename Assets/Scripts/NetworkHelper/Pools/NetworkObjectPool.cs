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
    private Dictionary<NetworkObject, ObjectPool<NetworkObject>> pools;

    // Stats tracking
    private int takenFromPool = 0;
    private int returnedToPool = 0;

    // Singleton pattern
    public static NetworkObjectPool Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializePools();
    }

    private void InitializePools()
    {
        pools = new Dictionary<NetworkObject, ObjectPool<NetworkObject>>();

        foreach (var config in poolConfigs)
        {
            if (config.prefab == null)
            {
                Debug.LogError("Null prefab in pool config!");
                continue;
            }

            var pool = new ObjectPool<NetworkObject>(
                createFunc: () => CreatePooledObject(config.prefab),
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: config.defaultCapacity,
                maxSize: config.maxSize
            );

            pools.Add(config.prefab, pool);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Prewarm pools on server only
            foreach (var config in poolConfigs)
            {
                if (config.prewarmCount > 0 && pools.TryGetValue(config.prefab, out var pool))
                {
                    PrewarmPool(config.prefab, pool, config.prewarmCount);
                }
            }
        }
    }

    private void PrewarmPool(NetworkObject prefab, ObjectPool<NetworkObject> pool, int count)
    {
        var prewarmObjects = new List<NetworkObject>(count);
        
        for (int i = 0; i < count; i++)
        {
            var obj = pool.Get();
            prewarmObjects.Add(obj);
        }

        foreach (var obj in prewarmObjects)
        {
            pool.Release(obj);
        }
        
        Debug.Log($"Prewarmed {count} instances of {prefab.name}");
    }

    private NetworkObject CreatePooledObject(NetworkObject prefab)
    {
        var instance = Instantiate(prefab);
        
        if (instance.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.SetPool(this, prefab);
        }
        
        return instance;
    }

    private void OnTakeFromPool(NetworkObject obj)
    {
        obj.gameObject.SetActive(true);
        
        if (obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnSpawn();
        }
        
        takenFromPool++;
    }

    private void OnReturnedToPool(NetworkObject obj)
    {
        obj.gameObject.SetActive(false);
        
        if (obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnDespawn();
        }
        
        returnedToPool++;
    }

    private void OnDestroyPoolObject(NetworkObject obj)
    {
        Destroy(obj.gameObject);
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

    // Debug helper
    [ContextMenu("Print Pool Stats")]
    public void PrintStats()
    {
        Debug.Log($"NetworkObjectPool Stats:\n" +
                  $"- Taken from pool: {takenFromPool}\n" +
                  $"- Returned to pool: {returnedToPool}");
    }
}