using System;
using System.Collections.Generic;
using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(PlayerController), typeof(Health), typeof(TankMovement2D))]
    [RequireComponent(typeof(Weapon))]
    public sealed class PlayerPowerupController : MonoBehaviour
    {
        private readonly Dictionary<PowerupType, float> expirationByType = new Dictionary<PowerupType, float>();

        private Health health;
        private TankMovement2D movement;
        private Weapon weapon;

        public event Action<PowerupType> PowerupApplied;

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<TankMovement2D>();
            weapon = GetComponent<Weapon>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        private void Update()
        {
            if (expirationByType.Count == 0)
            {
                return;
            }

            float now = Time.time;
            bool multipliersChanged = false;
            List<PowerupType> expiredTypes = null;

            foreach (KeyValuePair<PowerupType, float> entry in expirationByType)
            {
                if (now < entry.Value)
                {
                    continue;
                }

                if (expiredTypes == null)
                {
                    expiredTypes = new List<PowerupType>();
                }

                expiredTypes.Add(entry.Key);
            }

            if (expiredTypes == null)
            {
                return;
            }

            for (int i = 0; i < expiredTypes.Count; i++)
            {
                if (expirationByType.Remove(expiredTypes[i]))
                {
                    multipliersChanged = true;
                }
            }

            if (multipliersChanged)
            {
                RefreshMultipliers();
            }
        }

        public void Apply(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.Heal:
                    health?.Heal(25f);
                    break;
                case PowerupType.SpeedBoost:
                    expirationByType[PowerupType.SpeedBoost] = Time.time + 6f;
                    RefreshMultipliers();
                    break;
                case PowerupType.RapidFire:
                    expirationByType[PowerupType.RapidFire] = Time.time + 6f;
                    RefreshMultipliers();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            PowerupApplied?.Invoke(type);
        }

        private void HandleDeath(Health _, DamageInfo __)
        {
            expirationByType.Clear();
            RefreshMultipliers();
        }

        private void RefreshMultipliers()
        {
            float speedMultiplier = expirationByType.ContainsKey(PowerupType.SpeedBoost) ? 1.35f : 1f;
            float cooldownMultiplier = expirationByType.ContainsKey(PowerupType.RapidFire) ? 0.65f : 1f;

            movement?.SetExternalSpeedMultiplier(speedMultiplier);
            weapon?.SetExternalCooldownMultiplier(cooldownMultiplier);
        }
    }
}
