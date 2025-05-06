// Location: Core/Interfaces/IPoolable.cs
namespace Core.Interfaces
{
    /// <summary>
    /// Interface for objects that can be pooled
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Set the pool this object belongs to
        /// </summary>
        void SetPool(NetworkObjectPool pool, Unity.Netcode.NetworkObject prefab);
        
        /// <summary>
        /// Called when the object is taken from the pool
        /// </summary>
        void OnSpawn();
        
        /// <summary>
        /// Called when the object is returned to the pool
        /// </summary>
        void OnDespawn();
        
        /// <summary>
        /// Return this object to the pool
        /// </summary>
        void ReturnToPool(float delay = 0f);
    }
}