using System;
using UnityEngine;
using Armanya.Abilities;

namespace Armanya.Player
{
    /// <summary>
    /// Gestiona la vida del jugador, daño y curación (ej. Orbes de Estabilidad).
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Vida")]
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float startingHealth = 70f; // Empieza con 70 para probar los orbes de +15 HP

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action OnDeath;

        void Awake()
        {
            CurrentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        }

        void Start()
        {
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float amount, GameObject source)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Debug.Log($"[{name}] Recibió {amount} daño. HP: {CurrentHealth:F0}/{maxHealth}", this);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (IsDead)
            {
                Debug.Log($"[{name}] Ha caído sin vida.", this);
                OnDeath?.Invoke();
            }
        }

        /// <summary>Cura vida respetando el máximo (usado por los Orbes de Estabilidad).</summary>
        public bool Heal(float amount)
        {
            if (IsDead || amount <= 0f) return false;
            if (CurrentHealth >= maxHealth)
            {
                Debug.Log($"[{name}] Ya tiene la vida al máximo ({maxHealth}).", this);
                return false;
            }

            float prev = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            float gained = CurrentHealth - prev;

            Debug.Log($"[{name}] Curado +{gained:F0} HP. Vida actual: {CurrentHealth:F0}/{maxHealth}", this);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            return true;
        }
    }
}
