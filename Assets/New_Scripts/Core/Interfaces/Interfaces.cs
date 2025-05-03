// Replace your current IDamageable interface
using Unity.Netcode;
public interface IDamageable
{
    void TakeDamage(float amount, string source = null);
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsAlive { get; }
}

// Implement this interface instead of inheriting from PoolableNetworkObject
public interface IPoolable
{
    void SetPool(NetworkObjectPool pool, NetworkObject prefab);
    void OnSpawn();
    void OnDespawn();
    void ReturnToPool(float delay = 0f);
}
