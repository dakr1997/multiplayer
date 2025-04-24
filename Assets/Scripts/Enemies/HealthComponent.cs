using UnityEngine;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    private bool isDead = false;

    public bool IsAlive => !isDead && currentHealth.Value > 0f;
    public event System.Action OnDied;
    public event System.Action<float> OnDamaged;

    // ✅ Add these properties
    public float CurrentHealth => currentHealth.Value;
    public float MaxHealth => maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead = false;
        }
    }

    public void TakeDamage(float amount, string source = null)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value = Mathf.Max(currentHealth.Value - amount, 0);
        OnDamaged?.Invoke(amount);

        if (currentHealth.Value <= 0f && !isDead)
        {
            isDead = true;
            OnDied?.Invoke();
        }
    }
}
