using UnityEngine;
using Armanya.Abilities;
using Armanya.Gameplay;

namespace Armanya.Interactables
{
    /// <summary>
    /// Objeto restaurable de dos estados (Mecánica Social de Armanya):
    /// inicia Dañado y al recibir el Poder Secreto (K / Clic der) cambia a Reparado,
    /// desactivando barreras/peligros, abriendo caminos y otorgando puntos.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RestorableObject : MonoBehaviour, IRestorable
    {
        [Header("Estado actual")]
        [SerializeField] bool isRestored = false;

        [Header("Configuración de restauración")]
        [Tooltip("Efecto requerido para restaurar. Dejar en 'None' para aceptar cualquier poder de restauración.")]
        [SerializeField] RestorationEffect requiredEffect = RestorationEffect.None;
        [SerializeField] string restorationMessage = "¡Entorno Restaurado!";
        [SerializeField] int bonusPoints = 150;

        [Header("Feedback visual simple por color (SpriteRenderer)")]
        [SerializeField] SpriteRenderer targetRenderer;
        [SerializeField] Color damagedColor = new Color(0.4f, 0.2f, 0.6f, 1f); // Morado oscuro / fracturado
        [SerializeField] Color restoredColor = new Color(0.2f, 0.9f, 0.9f, 1f); // Cian brillante / restaurado

        [Header("Objetos visuales hijos (opcional)")]
        [SerializeField] GameObject damagedVisual;
        [SerializeField] GameObject restoredVisual;

        [Header("Colisión y Barreras según estado (opcional)")]
        [Tooltip("Por ejemplo: una plataforma sólida solo cuando está restaurada.")]
        [SerializeField] Collider2D solidColliderWhenRestored;
        [Tooltip("Objetos que se apagan al restaurar (ej. raíces secas, barricadas, láseres).")]
        [SerializeField] GameObject[] disableOnRestore;
        [Tooltip("Objetos que se encienden al restaurar (ej. puentes, rutas seguras, chorros de agua).")]
        [SerializeField] GameObject[] enableOnRestore;

        public bool IsRestored => isRestored;

        float popupUntil;
        string activePopupText;
        static GUIStyle popupStyle;

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
                activePopupText = $"Requiere poder: {requiredEffect}";
                popupUntil = Time.time + 2.0f;
                Debug.Log($"[{name}] Requiere el poder '{requiredEffect}', pero recibió '{effect}'.", this);
                return;
            }

            isRestored = true;
            GameSession.AddPoints(bonusPoints, restorationMessage);
            activePopupText = $"{restorationMessage} (+{bonusPoints} pts)";
            popupUntil = Time.time + 2.8f;

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

            // 4. Barreras o caminos dinámicos
            if (disableOnRestore != null)
            {
                foreach (var obj in disableOnRestore)
                    if (obj != null) obj.SetActive(!isRestored);
            }
            if (enableOnRestore != null)
            {
                foreach (var obj in enableOnRestore)
                    if (obj != null) obj.SetActive(isRestored);
            }
        }

        void OnGUI()
        {
            if (Time.time > popupUntil || string.IsNullOrEmpty(activePopupText)) return;

            if (popupStyle == null)
            {
                popupStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                popupStyle.normal.textColor = new Color(0.55f, 1f, 0.85f, 1f);
            }

            float w = 440f;
            float h = 42f;
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.16f, w, h);
            GUI.Box(r, activePopupText, popupStyle);
        }

        void OnValidate()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }
    }
}
