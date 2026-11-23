using UnityEngine;
using UnityEngine.InputSystem;
using Armanya.Player;

namespace Armanya.Abilities
{
    /// <summary>
    /// Ataque básico (clic izq / J) y poder secreto (clic der / K), con cooldown.
    /// Se añade al mismo objeto que PlayerController2D. Los datos vienen de AbilityData,
    /// así que Angus, Anya y Aleek solo cambian los assets (o heredan y sobreescriben).
    /// </summary>
    public class AbilityUser : MonoBehaviour
    {
        [Header("Habilidades")]
        [SerializeField] protected AbilityData basicAttack;
        [SerializeField] protected AbilityData secretPower;

        [Header("Origen del ataque")]
        [Tooltip("Opcional: punto desde donde sale el ataque. Si está vacío usa el centro del jugador.")]
        [SerializeField] protected Transform attackPoint;

        [Header("Feedback de prueba")]
        [SerializeField] bool showDebugHud = true;
        [SerializeField] bool logToConsole = true;

        PlayerController2D controller;
        float basicReadyTime;
        float secretReadyTime;

        /// <summary>Eventos para animación, sonido o HUD futuros.</summary>
        public event System.Action<AbilityData> OnBasicUsed;
        public event System.Action<AbilityData> OnSecretUsed;

        protected virtual void Awake() => controller = GetComponent<PlayerController2D>();

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
                Debug.Log($"[{name}] ¡{(isSecret ? "PODER SECRETO" : "ATAQUE")}: {data.abilityName}! (cooldown {data.cooldown}s)");

            Execute(data, isSecret);
            if (isSecret) OnSecretUsed?.Invoke(data); else OnBasicUsed?.Invoke(data);
        }

        /// <summary>Punto de extensión: cada personaje puede sobreescribirlo.</summary>
        protected virtual void Execute(AbilityData data, bool isSecret)
        {
            switch (data.shape)
            {
                case AbilityShape.Melee: DoMelee(data); break;
                case AbilityShape.Area: DoArea(data); break;
                case AbilityShape.Projectile: DoProjectile(data); break;
            }

            if (isSecret && data.restoration != RestorationEffect.None)
                ApplyRestoration(data);
        }

        int Facing => controller != null ? controller.FacingDirection : 1;
        Vector2 Origin => attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;

        void DoMelee(AbilityData d)
        {
            Vector2 center = Origin + Vector2.right * Facing * (d.range * 0.5f);
            var hits = Physics2D.OverlapBoxAll(center, new Vector2(d.range, d.height), 0f, d.targetLayers);
            HitAll(hits, d);
        }

        void DoArea(AbilityData d)
        {
            var hits = Physics2D.OverlapCircleAll(Origin, d.range, d.targetLayers);
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
            p.Launch(Vector2.right * Facing, d.projectileSpeed, d.projectileLifetime, d.damage, d.targetLayers, gameObject);
        }

        void HitAll(Collider2D[] hits, AbilityData d)
        {
            foreach (var h in hits)
            {
                if (h.transform.root == transform.root) continue; // no golpearse a sí mismo
                if (d.damage > 0f && h.TryGetComponent<IDamageable>(out var dmg)) dmg.TakeDamage(d.damage, gameObject);
            }
        }

        /// <summary>Efecto de restauración en el área; cada objeto IRestorable decide qué hacer.</summary>
        protected virtual void ApplyRestoration(AbilityData d)
        {
            float radius = Mathf.Max(d.range, 0.1f);
            var hits = Physics2D.OverlapCircleAll(Origin, radius, d.targetLayers);
            foreach (var h in hits)
            {
                if (h.transform.root == transform.root) continue;
                if (h.TryGetComponent<IRestorable>(out var r))
                    r.Restore(d.restoration, d.restorationAmount, d.effectDuration, gameObject);
            }
            if (logToConsole)
                Debug.Log($"[{name}] Efecto de restauración: {d.restoration} durante {d.effectDuration}s");
        }

        // ---------- Indicador simple de prueba ----------
        void OnGUI()
        {
            if (!showDebugHud) return;
            DrawBar(10, 10, "J / Clic izq", basicAttack, basicReadyTime);
            DrawBar(10, 36, "K / Clic der", secretPower, secretReadyTime);
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
            GUI.Label(new Rect(x, y, 400, 24), text);
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
                Gizmos.DrawWireSphere(o, secretPower.range);
            }
        }
    }
}
