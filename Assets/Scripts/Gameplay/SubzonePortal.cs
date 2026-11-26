using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Armanya.Player;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Teletransportador / puerta entre sub-zonas del mismo nivel (para extender el mapa y duración).
    /// Permite al jugador pararse frente a él y presionar 'S' o 'E' para viajar a otra sección del mapa.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SubzonePortal : MonoBehaviour
    {
        [Header("Destino")]
        [Tooltip("Si se asigna un nombre de escena, cargará esa escena al interactuar (ej. Zone2).")]
        [SerializeField] string targetSceneName = "";
        [Tooltip("Punto de llegada en el mapa actual (si no se especifica escena diferente).")]
        [SerializeField] Transform targetDestination;

        [Header("Configuración")]
        [SerializeField] string portalName = "Entrada a las Cavernas";
        [Tooltip("Tecla de interacción ('S' o 'E').")]
        [SerializeField] bool useSKey = true;
        [SerializeField] bool useEKey = true;

        [Header("Feedback visual")]
        [SerializeField] string hintMessage = "Presiona [S] para entrar";

        bool playerInside;
        PlayerController2D cachedPlayer;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController2D>() ?? other.GetComponentInParent<PlayerController2D>();
            if (pc != null)
            {
                playerInside = true;
                cachedPlayer = pc;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController2D>() ?? other.GetComponentInParent<PlayerController2D>();
            if (pc != null && pc == cachedPlayer)
            {
                playerInside = false;
                cachedPlayer = null;
            }
        }

        void Update()
        {
            if (!playerInside || cachedPlayer == null) return;
            if (string.IsNullOrEmpty(targetSceneName) && targetDestination == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            bool interact = (useSKey && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) ||
                            (useEKey && kb.eKey.wasPressedThisFrame);

            if (interact)
            {
                TeleportPlayer();
            }
        }

        void TeleportPlayer()
        {
            if (cachedPlayer == null) return;

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log($"[Portal] Cargando nueva zona '{targetSceneName}' a través de '{portalName}'...", this);
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
                playerInside = false;
                return;
            }

            if (targetDestination != null)
            {
                cachedPlayer.transform.position = targetDestination.position;

                var rb = cachedPlayer.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;

                Debug.Log($"[Portal] Jugador viajó a través de '{portalName}' hacia {targetDestination.position}.", this);
                playerInside = false;
            }
        }

        void OnGUI()
        {
            if (!playerInside || cachedPlayer == null) return;
            if (string.IsNullOrEmpty(targetSceneName) && targetDestination == null) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            if (screenPos.z < 0f) return;

            float w = 220f;
            float h = 32f;
            float x = screenPos.x - w * 0.5f;
            float y = Screen.height - screenPos.y - h;

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.95f, 0.4f, 1f) }
            };

            GUI.Box(new Rect(x, y, w, h), hintMessage, style);
        }

        void OnDrawGizmos()
        {
            if (targetDestination != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetDestination.position);
                Gizmos.DrawWireSphere(targetDestination.position, 0.5f);
            }
        }
    }
}

