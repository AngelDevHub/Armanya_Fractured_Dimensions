using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Armanya.Abilities;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Sistema de vida reutilizable para jugadores y monstruos.
    /// Incluye parpadeo al recibir daño, barra de vida UI (Slider),
    /// aturdimiento (stun), empuje (knockback) y 30% de probabilidad de soltar un Orbe (+15 HP) al morir.
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Vida")]
        [SerializeField] int maxHealth = 100;
        [SerializeField] int currentHealth = 100;
        [SerializeField] bool startAtMaxHealth = true;

        [Header("UI Barra de Vida (opcional)")]
        [SerializeField] Slider healthSlider;

        [Header("Feedback de Daño (Parpadeo y VFX 2DGamekit)")]
        [SerializeField] SpriteRenderer[] renderersToFlash;
        [SerializeField] Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] float flashDuration = 0.12f;
        [SerializeField] int flashCount = 2;
        [SerializeField] GameObject hitVfxPrefab;
        [SerializeField] GameObject deathVfxPrefab;

        [Header("Estado de Aturdimiento (Stun)")]
        [SerializeField] Color stunTintColor = new Color(0.75f, 0.85f, 1f, 1f);

        [Header("Muerte y Drop de Orbe de Estabilidad")]
        [SerializeField] bool destroyOnDeath = true;
        [SerializeField] bool dropOrbOnDeath = true;
        [Range(0f, 1f)]
        [SerializeField] float orbDropChance = 0.30f; // 30% de probabilidad
        [SerializeField] GameObject stabilityOrbPrefab;

        [Header("Eventos")]
        public UnityEvent onDeathEvent;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }
        public bool IsInvulnerable { get; set; }
        public bool IsStunned => Time.time < stunUntilTime;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        Color[] originalColors;
        Coroutine flashRoutine;
        float stunUntilTime;
        Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (startAtMaxHealth)
                currentHealth = maxHealth;
            else
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            if (renderersToFlash == null || renderersToFlash.Length == 0)
                renderersToFlash = GetComponentsInChildren<SpriteRenderer>();

            CacheOriginalColors();
            UpdateSlider();
        }

        void Update()
        {
            if (IsStunned)
            {
                // Mantener tinte sutil mientras está aturdido (si no está parpadeando)
                if (flashRoutine == null)
                    SetRenderersColor(stunTintColor);
            }
            else if (flashRoutine == null && stunUntilTime > 0f && Time.time >= stunUntilTime)
            {
                stunUntilTime = 0f;
                RestoreOriginalColors();
            }
        }

        /// <summary>Resta vida (int), ejecuta parpadeo, actualiza Slider y evalúa muerte.</summary>
        public void TakeDamage(int amount)
        {
            if (IsDead || IsInvulnerable || amount <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"[{name}] Recibió {amount} daño. Vida: {currentHealth}/{maxHealth}", this);

            UpdateSlider();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            TriggerDamageFlash();
            SpawnVFX(hitVfxPrefab, 1.5f);

            // Game Feel: Camera Shake ante impactos (más intenso si golpean al jugador)
            Armanya.CameraSystem.CameraFollow2D.Shake(
                destroyOnDeath ? 0.10f : 0.28f,
                destroyOnDeath ? 0.12f : 0.22f);

            if (currentHealth <= 0)
                Die();
        }

        /// <summary>Implementación de IDamageable para compatibilidad con proyectiles y áreas.</summary>
        public void TakeDamage(float amount, GameObject source)
        {
            TakeDamage(Mathf.RoundToInt(amount));
        }

        /// <summary>Reconfigura la vida máxima en ejecución (ej. 5 corazones x 100 HP = 500 HP).</summary>
        public void ConfigureMaxHealth(int newMaxHealth, bool refill = true)
        {
            maxHealth = Mathf.Max(1, newMaxHealth);
            if (refill)
                currentHealth = maxHealth;
            else
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            UpdateSlider();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>Cura vida respetando el máximo y dispara destello verde visible.</summary>
        public bool Heal(int amount)
        {
            if (IsDead || amount <= 0) return false;
            if (currentHealth >= maxHealth) return false;

            int prev = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"[{name}] Curado +{currentHealth - prev} HP. Vida: {currentHealth}/{maxHealth}", this);

            UpdateSlider();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            TriggerHealFlash();
            return true;
        }

        void TriggerHealFlash()
        {
            if (!gameObject.activeInHierarchy || renderersToFlash == null || renderersToFlash.Length == 0) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(ColorFlashCoroutine(new Color(0.25f, 1f, 0.45f, 1f), 3, 0.10f));
        }

        IEnumerator ColorFlashCoroutine(Color color, int count, float duration)
        {
            for (int i = 0; i < count; i++)
            {
                SetRenderersColor(color);
                yield return new WaitForSeconds(duration);
                RestoreOriginalColors();
                yield return new WaitForSeconds(duration * 0.5f);
            }
            flashRoutine = null;
        }

        /// <summary>Aplica aturdimiento temporal (ej. poderes K de 2.5s o 3.0s).</summary>
        public void ApplyStun(float duration)
        {
            if (IsDead || duration <= 0f) return;
            stunUntilTime = Mathf.Max(stunUntilTime, Time.time + duration);
            Debug.Log($"[{name}] ¡Aturdido durante {duration:F1}s!", this);
        }

        /// <summary>Aplica empuje físico (ej. golpe eléctrico de Angus).</summary>
        public void ApplyKnockback(Vector2 impulse)
        {
            if (IsDead || impulse.sqrMagnitude <= 0.001f) return;

            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.AddForce(impulse, ForceMode2D.Impulse);
            }
            else
            {
                // Si el monstruo aún no tiene Rigidbody2D dinámico, desplaza ligeramente su posición
                transform.position += (Vector3)(impulse.normalized * Mathf.Min(impulse.magnitude * 0.08f, 0.75f));
            }
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Debug.Log($"[{name}] Ha sido derrotado.", this);

            // Otorgar puntaje al derrotar enemigos o jefes (los jugadores tienen destroyOnDeath = false)
            if (destroyOnDeath)
            {
                int reward = name.ToLowerInvariant().Contains("ankea") ? 1000 : 100;
                GameSession.AddPoints(reward, $"Derrotar a {name}");
            }

            TryDropStabilityOrb();
            SpawnVFX(deathVfxPrefab, 2.0f);

            OnDeath?.Invoke();
            onDeathEvent?.Invoke();

            if (destroyOnDeath)
                Destroy(gameObject);
        }

        void TryDropStabilityOrb()
        {
            if (!dropOrbOnDeath || stabilityOrbPrefab == null) return;

            float roll = UnityEngine.Random.value;
            if (roll <= orbDropChance)
            {
                Instantiate(stabilityOrbPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
                Debug.Log($"[{name}] ¡Soltó un Orbe de Estabilidad Dimensional (+15 HP)!", this);
            }
        }

        void SpawnVFX(GameObject prefab, float lifetime)
        {
            if (prefab == null) return;
            var vfx = Instantiate(prefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
            Destroy(vfx, lifetime);
        }

        void UpdateSlider()
        {
            if (healthSlider == null) return;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        void TriggerDamageFlash()
        {
            if (!gameObject.activeInHierarchy || renderersToFlash == null || renderersToFlash.Length == 0) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashCoroutine());
        }

        IEnumerator FlashCoroutine()
        {
            for (int i = 0; i < flashCount; i++)
            {
                SetRenderersColor(damageFlashColor);
                yield return new WaitForSeconds(flashDuration);
                RestoreOriginalColors();
                yield return new WaitForSeconds(flashDuration * 0.5f);
            }
            flashRoutine = null;
        }

        void CacheOriginalColors()
        {
            if (renderersToFlash == null) return;
            originalColors = new Color[renderersToFlash.Length];
            for (int i = 0; i < renderersToFlash.Length; i++)
                originalColors[i] = renderersToFlash[i] != null ? renderersToFlash[i].color : Color.white;
        }

        void SetRenderersColor(Color c)
        {
            if (renderersToFlash == null) return;
            for (int i = 0; i < renderersToFlash.Length; i++)
                if (renderersToFlash[i] != null) renderersToFlash[i].color = c;
        }

        void RestoreOriginalColors()
        {
            if (renderersToFlash == null || originalColors == null) return;
            for (int i = 0; i < renderersToFlash.Length; i++)
                if (renderersToFlash[i] != null && i < originalColors.Length)
                    renderersToFlash[i].color = originalColors[i];
        }
    }
}
