using UnityEngine;

namespace Armanya.Abilities
{
    /// <summary>Proyectil simple en línea recta. Daña IDamageable y se destruye al impactar.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile2D : MonoBehaviour
    {
        float damage;
        LayerMask targets;
        GameObject owner;

        public void Launch(Vector2 direction, float speed, float lifetime, float dmg, LayerMask targetLayers, GameObject source)
        {
            damage = dmg;
            targets = targetLayers;
            owner = source;

            var rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && other.transform.root == owner.transform.root) return;
            if ((targets.value & (1 << other.gameObject.layer)) == 0) return;

            if (other.TryGetComponent<IDamageable>(out var d)) d.TakeDamage(damage, owner);
            Destroy(gameObject);
        }
    }
}
