public interface IDamageable
{
    bool IsInvincible { get; }
    float MaxHealth { get; }
    void TakeDamage(float amount);
}
