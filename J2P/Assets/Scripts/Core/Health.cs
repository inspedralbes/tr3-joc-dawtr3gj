using System;
using UnityEngine;

namespace TankArena2D
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath;
        [SerializeField, Min(0f)] private float destroyDelay;

        public event Action<Health, DamageInfo> Damaged;
        public event Action<Health, DamageInfo> Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void Configure(float newMaxHealth, bool shouldDestroyOnDeath, float newDestroyDelay = 0f)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);
            destroyOnDeath = shouldDestroyOnDeath;
            destroyDelay = Mathf.Max(0f, newDestroyDelay);
            CurrentHealth = Mathf.Clamp(CurrentHealth <= 0f ? maxHealth : CurrentHealth, 0f, maxHealth);
        }

        public void ResetToFull()
        {
            CurrentHealth = maxHealth;
            IsDead = false;
        }

        public void Revive(float healthAmount = -1f)
        {
            IsDead = false;
            CurrentHealth = healthAmount < 0f
                ? maxHealth
                : Mathf.Clamp(healthAmount, 1f, maxHealth);
        }

        public void SetState(float currentHealth, bool isDead)
        {
            IsDead = isDead;
            CurrentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            if (!IsDead && CurrentHealth <= 0f)
            {
                CurrentHealth = Mathf.Min(maxHealth, 1f);
            }
        }

        public bool ApplyDamage(DamageInfo damage)
        {
            if (IsDead || damage.Amount <= 0f)
            {
                return false;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);
            Damaged?.Invoke(this, damage);

            if (CurrentHealth <= 0f)
            {
                HandleDeath(damage);
            }

            return true;
        }

        private void HandleDeath(DamageInfo damage)
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            CurrentHealth = 0f;
            Died?.Invoke(this, damage);

            if (destroyOnDeath)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }
    }
}
