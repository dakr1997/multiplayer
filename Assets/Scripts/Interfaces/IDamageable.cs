public interface IDamageable
{
    void TakeDamage(float amount, string source = null);
    bool IsAlive { get; }
}
