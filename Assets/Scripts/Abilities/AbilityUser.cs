using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Armanya.Gameplay;
using Armanya.Player;

namespace Armanya.Abilities
{
    /// <summary>
    /// Ataque básico (clic izq / J) y poder secreto (clic der / K), con cooldown y VFX reales desde FirePoint.
    /// Soporta proyectiles, golpes con knockback, cúpulas/rayos en área con stun, dash intangible (Anya) y restauración.
    /// </summary>
    public class AbilityUser : MonoBehaviour
    {
        [Header("Habilidades")]
        [SerializeField] protected AbilityData basicAttack;
        [SerializeField] protected AbilityData secretPower;

        [Header("Origen del ataque (FirePoint)")]
        [Tooltip("Hijo vacío frente al jugador desde donde salen proyectiles y efectos.")]
        [SerializeField] protected Transform attackPoint;

        [Header("Feedback de prueba")]
        [SerializeField] bool showDebugHud = true;
        [SerializeField] bool logToConsole = true;

        PlayerController2D controller;
        Rigidbody2D rb;
        HealthSystem healthSystem;
        SpriteRenderer[] cachedRenderers;
        float basicReadyTime;
        float secretReadyTime;

        public event System.Action<AbilityData> OnBasicUsed;
        public event System.Action<AbilityData> OnSecretUsed;

        protected virtual void Awake()
        {
            controller = GetComponent<PlayerController2D>();
            rb = GetComponent<Rigidbody2D>();
            healthSystem = GetComponent<HealthSystem>();
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        protected virtual void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            bool basicPressed = kb.jKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            bool secretPressed = kb.kKey.wasPressedThisFrame || (mouse != null && mouse.rightButton.wasPressedThisFrame);

            if (basicPressed) TryUse(basicAttack, ref basicReadyTime, false);
            if (secretPressed) TryUse(secretPower, ref secretReadyTime, true);
        }

        void TryUse(AbilityData data, ref float readyTime, bool isSecret)
        {
            if (data == null)
            {
                if (logToConsole) Debug.LogWarning($"[{name}] No hay AbilityData asignada para {(isSecret ? "K" : "J")}.");
                return;
            }

            if (Time.time < readyTime)
            {
                if (logToConsole)
                    Debug.Log($"[{name}] {data.abilityName} en cooldown: {readyTime - Time.time:F1}s restantes");
                return;
            }

            readyTime = Time.time + data.cooldown;
            if (logToConsole)
                Debug.Log($"[{name}] ¡{(isSecret ? "PODER SECRETO" : "ATAQUE")}: {data.abilityName}! (daño {data.damage}, cooldown {data.cooldown}s)");

            Execute(data, isSecret);
            if (isSecret) OnSecretUsed?.Invoke(data); else OnBasicUsed?.Invoke(data);
        }

        protected virtual void Execute(AbilityData data, bool isSecret)
        {
            // Dash intangible (ej. Anya K: 0.5s)
            if (data.dashDuration > 0f)
                StartCoroutine(PerformIntangibleDash(data));

            switch (data.shape)
            {
                case AbilityShape.Melee: DoMelee(data); break;
                case AbilityShape.Area: DoArea(data); break;
                case AbilityShape.Projectile: DoProjectile(data); break;
            }

            if (isSecret && data.restoration != RestorationEffect.None)
                ApplyRestoration(data);

            // Game Feel: Camera Shake al activar Poder Secreto (K)
            if (isSecret)
                Armanya.CameraSystem.CameraFollow2D.Shake(0.22f, 0.20f);
        }

        int Facing => controller != null ? controller.FacingDirection : 1;
        Vector2 Origin => attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;

        void DoMelee(AbilityData d)
        {
            Vector2 center = Origin + Vector2.right * Facing * (d.range * 0.5f);
            SpawnVFX(d, center, 0f);
            var hits = Physics2D.OverlapBoxAll(center, new Vector2(d.range, d.height), 0f, d.targetLayers);
            HitAll(hits, d);
        }

        void DoArea(AbilityData d)
        {
            // Si tiene dash (onda frontal de Anya), desplazar el centro ligeramente al frente
            Vector2 center = d.dashDuration > 0f
                ? Origin + Vector2.right * Facing * (d.range * 0.5f)
                : (Vector2)transform.position;

            SpawnVFX(d, center, d.range);
            var hits = Physics2D.OverlapCircleAll(center, d.range, d.targetLayers);
            HitAll(hits, d);
        }

        void DoProjectile(AbilityData d)
        {
            if (d.projectilePrefab == null)
            {
                Debug.LogWarning($"[{name}] {d.abilityName}: falta Projectile Prefab.");
                return;
            }
            var p = Instantiate(d.projectilePrefab, Origin, Quaternion.identity);
            p.Launch(
                Vector2.right * Facing,
                d.projectileSpeed,
                d.projectileLifetime,
                d.damage,
                d.targetLayers,
                gameObject,
                d.vfxColor,
                d.knockbackForce,
                d.stunDuration,
                d.restoration,
                d.restorationAmount,
                d.effectDuration);
        }

        void SpawnVFX(AbilityData d, Vector2 position, float radius)
        {
            if (d.vfxPrefab == null) return;
            var vfxObj = Instantiate(d.vfxPrefab, position, Quaternion.identity);
            var effect = vfxObj.GetComponent<AbilityVFXEffect>();
            if (effect != null)
            {
                effect.Initialize(d.vfxColor, d.vfxLifetime, radius, Facing);
            }
            else
            {
                Destroy(vfxObj, d.vfxLifetime);
            }
        }

        IEnumerator PerformIntangibleDash(AbilityData d)
        {
            if (healthSystem != null) healthSystem.IsInvulnerable = true;
            SetAlpha(0.45f);

            float timer = 0f;
            float dir = Facing;
            while (timer < d.dashDuration)
            {
                if (rb != null)
                    rb.linearVelocity = new Vector2(dir * d.dashSpeed, 0f);
                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            SetAlpha(1f);
            if (healthSystem != null) healthSystem.IsInvulnerable = false;
        }

        void SetAlpha(float a)
        {
            if (cachedRenderers == null) return;
            foreach (var sr in cachedRenderers)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = a;
                sr.color = c;
            }
        }

        void HitAll(Collider2D[] hits, AbilityData d)
        {
            foreach (var h in hits)
            {
                if (h.transform.root == transform.root) continue;

                // Priorizar HealthSystem para daño + stun + knockback
                var targetHealth = h.GetComponent<HealthSystem>();
                if (targetHealth == null) targetHealth = h.GetComponentInParent<HealthSystem>();
                if (targetHealth != null)
                {
                    if (d.damage > 0f) targetHealth.TakeDamage(Mathf.RoundToInt(d.damage));
                    if (d.stunDuration > 0f) targetHealth.ApplyStun(d.stunDuration);
                    if (d.knockbackForce > 0f)
                    {
                        Vector2 dir = ((Vector2)h.transform.position - (Vector2)transform.position).normalized;
                        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right * Facing;
                        targetHealth.ApplyKnockback(dir * d.knockbackForce);
                    }
                }
                else if (d.damage > 0f && h.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(d.damage, gameObject);
                }
            }
        }

        protected virtual void ApplyRestoration(AbilityData d)
        {
            float radius = Mathf.Max(d.range, 0.1f);
            var hits = Physics2D.OverlapCircleAll(transform.position, radius, d.targetLayers);
            foreach (var h in hits)
            {
                if (h.transform.root == transform.root) continue;
                if (h.TryGetComponent<IRestorable>(out var r))
                    r.Restore(d.restoration, d.restorationAmount, d.effectDuration, gameObject);
            }
            if (logToConsole)
                Debug.Log($"[{name}] Efecto de restauración: {d.restoration} durante {d.effectDuration}s");
        }

        void OnGUI()
        {
            if (!showDebugHud) return;
            // Ubicado en la esquina inferior izquierda para no estorbar los corazones ni la vista
            float yBase = Screen.height - 56f;
            DrawBar(12, yBase,      "[J]", basicAttack, basicReadyTime);
            DrawBar(12, yBase + 24, "[K]", secretPower, secretReadyTime);
        }

        void DrawBar(float x, float y, string label, AbilityData d, float readyTime)
        {
            string text;
            if (d == null) text = $"{label}: (sin asignar)";
            else
            {
                float left = Mathf.Max(0f, readyTime - Time.time);
                text = left > 0f ? $"{label}: {d.abilityName} - {left:F1}s" : $"{label}: {d.abilityName} - LISTO";
            }
            GUI.Label(new Rect(x, y, 420, 24), text);
        }

        void OnDrawGizmosSelected()
        {
            Vector2 o = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            int f = controller != null ? controller.FacingDirection : 1;
            if (basicAttack != null && basicAttack.shape == AbilityShape.Melee)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(o + Vector2.right * f * basicAttack.range * 0.5f,
                                    new Vector3(basicAttack.range, basicAttack.height, 0));
            }
            if (secretPower != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, secretPower.range);
            }
        }
    }
}
