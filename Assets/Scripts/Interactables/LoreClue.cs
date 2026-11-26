using UnityEngine;
using Armanya.Gameplay;

namespace Armanya.Interactables
{
    /// <summary>
    /// Pista de Lore coleccionable. Al tocarla con Trigger 2D, se suma al contador global (1/3, 2/3, 3/3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LoreClue : MonoBehaviour
    {
        [Header("Datos de la pista")]
        [SerializeField] string clueName = "Fragmento Dimensional";
        [TextArea(2, 4)]
        [SerializeField] string clueDescription = "Registro de la fractura dimensional de Armanya.";

        [Header("Efecto de flotación y visibilidad")]
        [SerializeField] bool floatAnimation = true;
        [SerializeField] float floatSpeed = 2.5f;
        [SerializeField] float floatHeight = 0.22f;
        [Tooltip("Escala mínima para que la pista de lore siempre sea bien visible en pantalla.")]
        [SerializeField] float minDisplayScale = 2.0f;
        [SerializeField] float pulseAmplitude = 0.12f;

        Vector3 startPos;
        Vector3 baseScale;
        bool collected;

        void Awake()
        {
            startPos = transform.position;

            // Agrandar automáticamente si el sprite/objeto estaba demasiado pequeño
            Vector3 s = transform.localScale;
            if (Mathf.Abs(s.x) < minDisplayScale || Mathf.Abs(s.y) < minDisplayScale)
            {
                float sx = Mathf.Sign(s.x == 0f ? 1f : s.x) * Mathf.Max(Mathf.Abs(s.x), minDisplayScale);
                float sy = Mathf.Sign(s.y == 0f ? 1f : s.y) * Mathf.Max(Mathf.Abs(s.y), minDisplayScale);
                transform.localScale = new Vector3(sx, sy, s.z);
            }
            baseScale = transform.localScale;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void Update()
        {
            if (!floatAnimation) return;
            transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0f);
            float pulse = 1f + Mathf.Sin(Time.time * (floatSpeed * 1.4f)) * pulseAmplitude;
            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;

            // Verificar si quien entró es el jugador
            if (other.CompareTag("Player") || other.GetComponent<Player.PlayerController2D>() != null || other.GetComponentInParent<Player.PlayerController2D>() != null)
            {
                collected = true;

                if (GameSession.Instance != null)
                {
                    GameSession.Instance.CollectLoreClue(clueName);
                }
                else
                {
                    Debug.LogWarning("[LoreClue] No se encontró GameSession en la escena para registrar la pista.");
                }

                // Ocultar y destruir
                Destroy(gameObject);
            }
        }
    }
}
