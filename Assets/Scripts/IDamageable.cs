public interface IDamageable
{
    bool IsInvincible { get; }
    void TakeDamage(float amount);
}
