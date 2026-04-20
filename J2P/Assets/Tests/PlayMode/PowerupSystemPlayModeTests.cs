using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TankArena2D.Tests
{
    public sealed class PowerupSystemPlayModeTests
    {
        [UnityTest]
        public IEnumerator HealPowerupRestoresHealthWithoutExceedingMaximum()
        {
            GameObject playerObject = CreatePlayer(Vector2.zero);
            Component powerupController = playerObject.AddComponent(GetGameplayType("PlayerPowerupController"));

            yield return null;

            Component health = GetRequiredComponent(playerObject, "Health");
            object damageInfo = Activator.CreateInstance(
                GetGameplayType("DamageInfo"),
                50f,
                null,
                Vector2.zero,
                Vector2.zero);

            Invoke(health, "ApplyDamage", damageInfo);
            Invoke(powerupController, "Apply", ParsePowerup("Heal"));

            Assert.AreEqual(95f, GetFloat(health, "CurrentHealth"), 0.01f);

            Invoke(powerupController, "Apply", ParsePowerup("Heal"));
            Invoke(powerupController, "Apply", ParsePowerup("Heal"));

            Assert.AreEqual(GetFloat(health, "MaxHealth"), GetFloat(health, "CurrentHealth"), 0.01f);

            UnityEngine.Object.Destroy(playerObject);
        }

        [UnityTest]
        public IEnumerator TemporaryPowerupsApplyRefreshAndExpire()
        {
            GameObject playerObject = CreatePlayer(Vector2.zero);
            Component powerupController = playerObject.AddComponent(GetGameplayType("PlayerPowerupController"));

            yield return null;

            Component movement = GetRequiredComponent(playerObject, "TankMovement2D");
            Component weapon = GetRequiredComponent(playerObject, "Weapon");

            float baseSpeed = GetFloat(movement, "MoveSpeed");
            float baseCooldown = GetFloat(weapon, "Cooldown");

            Invoke(powerupController, "Apply", ParsePowerup("SpeedBoost"));
            Invoke(powerupController, "Apply", ParsePowerup("RapidFire"));

            Assert.AreEqual(baseSpeed * 1.35f, GetFloat(movement, "EffectiveMoveSpeed"), 0.01f);
            Assert.AreEqual(baseCooldown * 0.65f, GetFloat(weapon, "EffectiveCooldown"), 0.01f);

            yield return new WaitForSeconds(5f);
            Invoke(powerupController, "Apply", ParsePowerup("SpeedBoost"));

            yield return new WaitForSeconds(2f);
            Assert.AreEqual(baseSpeed * 1.35f, GetFloat(movement, "EffectiveMoveSpeed"), 0.01f);
            Assert.AreEqual(baseCooldown, GetFloat(weapon, "EffectiveCooldown"), 0.01f);

            yield return new WaitForSeconds(4.2f);
            Assert.AreEqual(baseSpeed, GetFloat(movement, "EffectiveMoveSpeed"), 0.01f);

            UnityEngine.Object.Destroy(playerObject);
        }

        [UnityTest]
        public IEnumerator SpawnManagerRespectsLimitAndAvoidsBlockedPositions()
        {
            Component bounds = CreateArenaBounds();
            GameObject playerObject = CreatePlayer(new Vector2(-8f, -5f));

            GameObject obstacleObject = new GameObject("Obstacle");
            BoxCollider2D obstacleCollider = obstacleObject.AddComponent<BoxCollider2D>();
            obstacleCollider.size = new Vector2(8f, 8f);
            obstacleObject.transform.position = Vector2.zero;

            GameObject managerObject = new GameObject("PowerupManager");
            Component manager = managerObject.AddComponent(GetGameplayType("PowerupSpawnManager"));
            SetPrivateField(manager, "arenaBounds", bounds);
            SetPrivateField(manager, "player", GetRequiredComponent(playerObject, "PlayerController"));
            SetPrivateField(manager, "spawnAttempts", 64);
            SetPrivateField(manager, "spawnCheckRadius", 0.9f);
            SetPrivateField(manager, "spawnPadding", 1.5f);
            SetPrivateField(manager, "spawnInterval", 0.05f);
            SetPrivateField(manager, "maxActivePowerups", 3);

            yield return null;
            yield return new WaitForSeconds(0.4f);

            Type pickupType = GetGameplayType("PowerupPickup");
            UnityEngine.Object[] pickups = UnityEngine.Object.FindObjectsByType(pickupType);

            Assert.AreEqual(3, GetInt(manager, "ActivePowerupCount"));
            Assert.AreEqual(3, pickups.Length);

            for (int i = 0; i < pickups.Length; i++)
            {
                Vector2 position = ((Component)pickups[i]).transform.position;
                bool isInsideBounds = (bool)Invoke(bounds, "Contains", position, 0.9f);
                Assert.IsTrue(isInsideBounds);
                Assert.IsFalse(obstacleCollider.OverlapPoint(position));
            }

            UnityEngine.Object.Destroy(managerObject);
            UnityEngine.Object.Destroy(obstacleObject);
            UnityEngine.Object.Destroy(playerObject);
            UnityEngine.Object.Destroy(((Component)bounds).gameObject);
        }

        private static Component CreateArenaBounds()
        {
            GameObject boundsObject = new GameObject("ArenaBounds");
            Component bounds = boundsObject.AddComponent(GetGameplayType("ArenaBounds"));
            Invoke(bounds, "Configure", new Vector2(30f, 20f), 0.5f);
            return bounds;
        }

        private static GameObject CreatePlayer(Vector2 position)
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.transform.position = position;
            playerObject.AddComponent<BoxCollider2D>();

            GameObject turretObject = new GameObject("Turret");
            turretObject.transform.SetParent(playerObject.transform, false);

            GameObject muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.SetParent(turretObject.transform, false);

            Component player = playerObject.AddComponent(GetGameplayType("PlayerController"));
            Component turretAim = GetRequiredComponent(playerObject, "TurretAim");
            Invoke(turretAim, "SetTurret", turretObject.transform);
            Assert.IsNotNull(player);
            return playerObject;
        }

        private static Type GetGameplayType(string name)
        {
            Type type = Type.GetType($"TankArena2D.{name}, Assembly-CSharp");
            Assert.IsNotNull(type, $"Gameplay type not found: {name}");
            return type;
        }

        private static Component GetRequiredComponent(GameObject gameObject, string typeName)
        {
            Component component = gameObject.GetComponent(GetGameplayType(typeName));
            Assert.IsNotNull(component, $"Component not found: {typeName}");
            return component;
        }

        private static object ParsePowerup(string enumName)
        {
            return Enum.Parse(GetGameplayType("PowerupType"), enumName);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field not found: {fieldName}");
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method not found: {methodName}");
            return method.Invoke(target, args);
        }

        private static float GetFloat(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Property not found: {propertyName}");
            return Convert.ToSingle(property.GetValue(target));
        }

        private static int GetInt(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Property not found: {propertyName}");
            return Convert.ToInt32(property.GetValue(target));
        }
    }
}
