using UnityEngine;
using Armanya.Gameplay;
using Gamekit2D;

namespace Armanya.Abilities
{
    /// <summary>
    /// Proyectil 2D con soporte para daño, empuje, aturdimiento y restauración.
    /// Detecta enemigos por Tag ("Enemy") / Layer / IDamageable y objetos IRestorable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile2D : MonoBehaviour
    {
        [Header("VFX de Impacto (2DGamekit)")]
        [SerializeField] GameObject impactVfxPrefab;

        float damage;
        float knockback;
        float stunDuration;
        RestorationEffect restoration;
        float restorationAmount;
        float effectDuration;
        LayerMask targets;
        GameObject owner;
        Vector2 moveDir;
        Color activeTint = Color.white;

        public void Launch(
            Vector2 direction,
            float speed,
            float lifetime,
            float dmg,
            LayerMask targetLayers,
            GameObject source,
            Color tint,
            float knockbackForce = 0f,
            float stunSec = 0f,
            RestorationEffect restEffect = RestorationEffect.None,
            float restAmt = 0f,
            float restDur = 0f)
        {
            moveDir = direction.normalized;
            damage = dmg;
            knockback = knockbackForce;
            stunDuration = stunSec;
            restoration = restEffect;
            restorationAmount = restAmt;
            effectDuration = restDur;
            targets = targetLayers;
            owner = source;
            activeTint = tint;

            // Teñir sprite y partículas del proyectil
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.color = tint;

            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = tint;
            }

            if (moveDir.x < 0f)
            {
                var s = transform.localScale;
                s.x = -Mathf.Abs(s.x);
                transform.localScale = s;
            }

            var rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = moveDir * speed;
            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && other.transform.root == owner.transform.root) return;

            bool layerMatch = (targets.value & (1 << other.gameObject.layer)) != 0;
            bool isEnemyTag = other.CompareTag("Enemy");
            var gamekitDamageable = other.GetComponent<Damageable>() ?? other.GetComponentInParent<Damageable>();
            bool isDestructibleProp = gamekitDamageable != null && other.GetComponentInParent<Armanya.Player.PlayerController2D>() == null;
            if (!layerMatch && !isEnemyTag && !isDestructibleProp) return;

            bool hitSomething = false;

            // Restauración si golpea un objeto restaurable o generador
            if (restoration != RestorationEffect.None && other.TryGetComponent<IRestorable>(out var restorable))
            {
                restorable.Restore(restoration, restorationAmount, effectDuration, owner);
                hitSomething = true;
            }

            // Daño + Stun + Knockback
            var health = other.GetComponent<HealthSystem>();
            if (health == null) health = other.GetComponentInParent<HealthSystem>();
            if (health != null && damage > 0f)
            {
                health.TakeDamage(Mathf.RoundToInt(damage));
                if (stunDuration > 0f) health.ApplyStun(stunDuration);
                if (knockback > 0f) health.ApplyKnockback(moveDir * knockback);
                hitSomething = true;
            }
            else if (damage > 0f && other.TryGetComponent<IDamageable>(out var dmgTarget))
            {
                dmgTarget.TakeDamage(damage, owner);
                hitSomething = true;
            }
            else if (isDestructibleProp && damage > 0f)
            {
                // Permite que los ataques de Armanya destruyan paredes/columnas/interruptores heredados de 2DGamekit
                gamekitDamageable.SetHealth(gamekitDamageable.CurrentHealth - 1);
                if (gamekitDamageable.CurrentHealth <= 0)
                {
                    gamekitDamageable.OnDie.Invoke(null, gamekitDamageable);
                    if (gamekitDamageable.disableOnDeath)
                        gamekitDamageable.gameObject.SetActive(false);
                }
                hitSomething = true;
            }

            // Destruirse al impactar contra enemigos o paredes sólidas (no contra triggers de pistas/orbes)
            if (hitSomething || !other.isTrigger)
            {
                SpawnImpactVFX();
                Destroy(gameObject);
            }
        }

        void SpawnImpactVFX()
        {
            if (impactVfxPrefab == null) return;
            var vfx = Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
            foreach (var sr in vfx.GetComponentsInChildren<SpriteRenderer>(true))
                sr.color = activeTint;
            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.startColor = activeTint;
            }
            Destroy(vfx, 1.5f);
        }
    }
}
