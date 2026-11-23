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

        [Header("Efecto de flotación suave")]
        [SerializeField] bool floatAnimation = true;
        [SerializeField] float floatSpeed = 2.5f;
        [SerializeField] float floatHeight = 0.15f;

        Vector3 startPos;
        bool collected;

        void Awake()
        {
            startPos = transform.position;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void Update()
        {
            if (!floatAnimation) return;
            transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0f);
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
