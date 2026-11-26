using UnityEngine;
using Armanya.Abilities;
using Armanya.Gameplay;
using Armanya.Player;

namespace Armanya.Enemies
{
    /// <summary>
    /// Proyectil enemigo (Ojo Fantasma, Centinela Tentacular, Orbes Térmicos de Ankea).
    /// Baja vida al jugador y puede ser bloqueado por el Escudo/Cúpula Cinética de Aleek (KineticShield2D).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyProjectile2D : MonoBehaviour
    {
        [SerializeField] int damage = 15;
        [SerializeField] float speed = 8f;
        [SerializeField] float lifetime = 4f;

        GameObject owner;
        Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Launch(Vector2 direction, int customDamage, float customSpeed, GameObject source)
        {
            owner = source;
            if (customDamage > 0) damage = customDamage;
            if (customSpeed > 0f) speed = customSpeed;

            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.left;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * speed;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && other.transform.root == owner.transform.root) return;

            // Bloqueado por el Escudo / Cúpula Cinética de Aleek
            if (other.GetComponent<KineticShield2D>() != null || other.GetComponentInParent<KineticShield2D>() != null)
            {
                Debug.Log($"[{name}] ¡Proyectil enemigo bloqueado por el Escudo Cinético de Aleek!", this);
                Destroy(gameObject);
                return;
            }

            // Impacto contra el jugador
            var playerCtrl = other.GetComponent<PlayerController2D>();
            if (playerCtrl == null) playerCtrl = other.GetComponentInParent<PlayerController2D>();
            if (playerCtrl != null)
            {
                var hs = playerCtrl.GetComponent<HealthSystem>();
                if (hs != null && hs.IsInvulnerable)
                {
                    // Anya en dash intangible: el proyectil la atraviesa sin dañarla
                    return;
                }

                if (hs != null) hs.TakeDamage(damage);

                var ph = playerCtrl.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(damage, owner);

                Destroy(gameObject);
                return;
            }

            // Destruirse al chocar con paredes o suelo sólido (ignorar triggers de enemigos/pistas)
            if (!other.isTrigger && !other.CompareTag("Enemy"))
                Destroy(gameObject);
        }
    }
}
