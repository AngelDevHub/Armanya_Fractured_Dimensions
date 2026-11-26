using UnityEngine;
using Armanya.Gameplay;
using Armanya.Player;

namespace Armanya.Interactables
{
    /// <summary>
    /// Orbe de Estabilidad Dimensional. Al entrar en contacto con Trigger 2D,
    /// otorga +15 HP al jugador (compatible con HealthSystem y PlayerHealth) y se consume.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StabilityOrb : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] int healAmount = 15;
        [Tooltip("Si es true, solo se recoge si el jugador tiene menos de la vida máxima.")]
        [SerializeField] bool onlyCollectIfDamaged = true;

        [Header("Efecto de flotación")]
        [SerializeField] float floatSpeed = 3f;
        [SerializeField] float floatHeight = 0.12f;

        Vector3 startPos;
        bool consumed;

        void Awake()
        {
            startPos = transform.position;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void Update()
        {
            transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed) return;

            // Detectar jugador por PlayerController2D, PlayerCharacter, AbilityUser o Tag Player
            bool isPlayer = other.GetComponent<PlayerController2D>() != null ||
                            other.GetComponentInParent<PlayerController2D>() != null ||
                            other.GetComponent<Gamekit2D.PlayerCharacter>() != null ||
                            other.GetComponentInParent<Gamekit2D.PlayerCharacter>() != null ||
                            other.GetComponent<Armanya.Abilities.AbilityUser>() != null ||
                            other.GetComponentInParent<Armanya.Abilities.AbilityUser>() != null ||
                            other.CompareTag("Player");

            if (!isPlayer) return;

            // 1. Intentar curar HealthSystem (nuevo sistema unificado)
            var healthSys = other.GetComponent<HealthSystem>() ?? other.GetComponentInParent<HealthSystem>();
            var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            var damageable = other.GetComponent<Gamekit2D.Damageable>() ?? other.GetComponentInParent<Gamekit2D.Damageable>();

            bool healedAny = false;

            if (healthSys != null)
            {
                if (!onlyCollectIfDamaged || healthSys.CurrentHealth < healthSys.MaxHealth)
                    healedAny |= healthSys.Heal(Mathf.Max(healAmount, 100));
            }
            else if (damageable != null && damageable.CurrentHealth < damageable.startingHealth)
            {
                damageable.GainHealth(1);
                healedAny = true;
            }

            if (playerHealth != null)
            {
                if (!onlyCollectIfDamaged || playerHealth.CurrentHealth < playerHealth.MaxHealth)
                    healedAny |= playerHealth.Heal(healAmount);
            }

            if (healedAny || !onlyCollectIfDamaged)
            {
                consumed = true;
                GameSession.AddPoints(50, "Orbe de Estabilidad");
                GameSession.ShowPickupNotification(
                    "+1 VIDA (Orbe de Estabilidad)",
                    new Color(0.25f, 1f, 0.55f, 1f),
                    2.0f);
                Debug.Log($"[Orbe] Orbe de Estabilidad Dimensional consumido (+{healAmount} HP).", this);
                Destroy(gameObject);
            }
        }
    }
}
