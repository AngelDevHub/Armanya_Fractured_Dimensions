using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Gamekit2D
{
    public class HealthPickup : MonoBehaviour
    {
        public int healthAmount = 1;
        public UnityEvent OnGivingHealth;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Detectar jugador (PlayerCharacter, PlayerController2D o Tag Player)
            var damageable = other.GetComponent<Damageable>() ?? other.GetComponentInParent<Damageable>();
            var healthSys = other.GetComponent<Armanya.Gameplay.HealthSystem>() ?? other.GetComponentInParent<Armanya.Gameplay.HealthSystem>();
            var playerCtrl = other.GetComponent<Armanya.Player.PlayerController2D>() ?? other.GetComponentInParent<Armanya.Player.PlayerController2D>();
            var playerChar = other.GetComponent<PlayerCharacter>() ?? other.GetComponentInParent<PlayerCharacter>();

            if (playerChar == null && PlayerCharacter.PlayerInstance != null && other.gameObject == PlayerCharacter.PlayerInstance.gameObject)
                playerChar = PlayerCharacter.PlayerInstance;

            if (damageable == null && playerChar != null)
                damageable = playerChar.damageable;

            if (damageable == null && healthSys == null && playerCtrl == null && playerChar == null && !other.CompareTag("Player"))
                return;

            bool canHealDamageable = damageable != null && damageable.CurrentHealth < damageable.startingHealth;
            bool canHealHealthSys = healthSys != null && healthSys.CurrentHealth < healthSys.MaxHealth;

            if (canHealDamageable || canHealHealthSys)
            {
                if (damageable != null && canHealDamageable)
                {
                    // ArmanyaPlayerHealthBridge sincroniza automáticamente +100 HP por corazón en HealthSystem
                    damageable.GainHealth(Mathf.Min(healthAmount, damageable.startingHealth - damageable.CurrentHealth));
                }
                else if (healthSys != null && canHealHealthSys)
                {
                    // Si el corazón actual solo estaba parcialmente dañado, rellenarlo al 100%
                    healthSys.Heal(healthAmount * 100);
                }

                // Feedback visual llamativo al recoger vida
                Armanya.Gameplay.GameSession.ShowPickupNotification(
                    $"+{healthAmount} VIDA RESTAURADA (100%)",
                    new Color(0.25f, 1f, 0.45f, 1f),
                    2.2f);
                Armanya.Gameplay.GameSession.AddPoints(50, "Cristal de Vida");

                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                OnGivingHealth.Invoke();
            }
        }
    }
}