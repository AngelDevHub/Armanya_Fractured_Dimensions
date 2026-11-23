using UnityEngine;
using Armanya.Abilities;

namespace Armanya.Interactables
{
    /// <summary>
    /// Objeto restaurable de dos estados: inicia Dañado y al recibir la acción
    /// de restauración (K / Clic der) cambia a Reparado.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RestorableObject : MonoBehaviour, IRestorable
    {
        [Header("Estado actual")]
        [SerializeField] bool isRestored = false;

        [Header("Configuración de restauración")]
        [Tooltip("Efecto requerido para restaurar. Dejar en 'None' para aceptar cualquier poder de restauración.")]
        [SerializeField] RestorationEffect requiredEffect = RestorationEffect.None;

        [Header("Feedback visual simple por color (SpriteRenderer)")]
        [SerializeField] SpriteRenderer targetRenderer;
        [SerializeField] Color damagedColor = new Color(0.4f, 0.2f, 0.6f, 1f); // Morado oscuro / fracturado
        [SerializeField] Color restoredColor = new Color(0.2f, 0.9f, 0.9f, 1f); // Cian brillante / restaurado

        [Header("Objetos visuales hijos (opcional)")]
        [SerializeField] GameObject damagedVisual;
        [SerializeField] GameObject restoredVisual;

        [Header("Colisión según estado (opcional)")]
        [Tooltip("Por ejemplo: una plataforma sólida solo cuando está restaurada.")]
        [SerializeField] Collider2D solidColliderWhenRestored;

        public bool IsRestored => isRestored;

        void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }

        /// <summary>
        /// Implementación de IRestorable (llamada por AbilityUser al presionar K).
        /// </summary>
        public void Restore(RestorationEffect effect, float amount, float duration, GameObject source)
        {
            if (isRestored)
            {
                Debug.Log($"[{name}] Ya se encuentra completamente restaurado.", this);
                return;
            }

            // Si exige un efecto específico (ej. solo LightningWeather) y no coincide, no se restaura
            if (requiredEffect != RestorationEffect.None && requiredEffect != effect)
            {
                Debug.Log($"[{name}] Requiere el poder '{requiredEffect}', pero recibió '{effect}'.", this);
                return;
            }

            isRestored = true;
            Debug.Log($"[{name}] ¡RESTAURADO CON ÉXITO! Pasó de estado Dañado a Reparado usando {effect}.", this);

            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            // 1. Cambio de color en SpriteRenderer
            if (targetRenderer != null)
            {
                targetRenderer.color = isRestored ? restoredColor : damagedColor;
            }

            // 2. Activación/desactivación de hijos visuales si están asignados
            if (damagedVisual != null) damagedVisual.SetActive(!isRestored);
            if (restoredVisual != null) restoredVisual.SetActive(isRestored);

            // 3. Activar/desactivar colisión si se configuró
            if (solidColliderWhenRestored != null)
            {
                solidColliderWhenRestored.enabled = isRestored;
            }
        }

        void OnValidate()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }
    }
}
