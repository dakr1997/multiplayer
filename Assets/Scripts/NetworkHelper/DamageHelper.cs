using UnityEngine;
using Unity.Netcode;
public static class DamageHelper
{
    /// <summary>
    /// Applies damage to any object that implements IDamageable. Only works on the server.
    /// </summary>
    public static void ApplyDamage(GameObject target, float amount, string source = null)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("DamageHelper called on client! Damage must be handled by the server.");
            return;
        }

        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(amount, source);
        }
        else
        {
            Debug.LogWarning($"Target {target.name} does not implement IDamageable.");
        }
    }
}
