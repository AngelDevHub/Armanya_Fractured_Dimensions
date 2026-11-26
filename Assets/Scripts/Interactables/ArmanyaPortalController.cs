using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Armanya.Gameplay;
using Armanya.Player;

namespace Armanya.Interactables
{
    /// <summary>
    /// Portal de transición dimensional con visual de 2DGamekit (Teleporter / TransitionStart)
    /// conectado directamente al GameManager de Armanya:
    ///   - Guarda el progreso de la pista del nivel en GameManager.
    ///   - Si ya están las 3/3 pistas, abre el salto directo a Level4_RuinasFinales.
    ///   - Si faltan pistas, carga la siguiente dimensión o vuelve al selector (Menu_Inicio).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ArmanyaPortalController : MonoBehaviour
    {
        [Header("Conexión con GameManager")]
        [SerializeField] LoreClueId dimensionClue = LoreClueId.BitacoraHidrica;
        [Tooltip("Escena siguiente opcional (si se deja vacío, avanza al siguiente nivel o vuelve a Menu_Inicio).")]
        [SerializeField] string nextSceneName = "";
        [Tooltip("Si es true, requiere recolectar al menos 1 pista de lore en el nivel antes de activar el portal.")]
        [SerializeField] bool requireClueInLevel = false;
        [Tooltip("Si es true, el jugador presiona [S] o [E] frente al portal; si es false, viaja al tocarlo.")]
        [SerializeField] bool requireInteractKey = true;

        bool playerInside;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            AutoConfigureByScene();
        }

        void AutoConfigureByScene()
        {
            string sn = SceneManager.GetActiveScene().name.ToLowerInvariant();
            if (sn.Contains("grieta") || sn.Contains("level1"))
            {
                dimensionClue = LoreClueId.BitacoraHidrica;
                if (string.IsNullOrEmpty(nextSceneName)) nextSceneName = "Level2_DistritoCaos";
            }
            else if (sn.Contains("caos") || sn.Contains("level2"))
            {
                dimensionClue = LoreClueId.ManifiestoRoto;
                if (string.IsNullOrEmpty(nextSceneName)) nextSceneName = "Level3_NexoOrbital";
            }
            else if (sn.Contains("nexo") || sn.Contains("level3"))
            {
                dimensionClue = LoreClueId.ConsolaCentral;
                if (string.IsNullOrEmpty(nextSceneName)) nextSceneName = "Level4_RuinasFinales";
            }
        }

        bool IsPlayer(Collider2D other) =>
            other.CompareTag("Player") ||
            other.GetComponent<PlayerController2D>() != null ||
            other.GetComponentInParent<PlayerController2D>() != null;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            playerInside = true;

            if (!requireInteractKey)
                TryActivatePortal();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
                playerInside = false;
        }

        void Update()
        {
            if (!playerInside || !requireInteractKey) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.sKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            {
                TryActivatePortal();
            }
        }

        void TryActivatePortal()
        {
            if (requireClueInLevel && GameSession.Instance != null && GameSession.Instance.CurrentClues <= 0)
            {
                GameSession.ShowPickupNotification(
                    "Recolecta una Pista de Lore para abrir el portal",
                    new Color(1f, 0.75f, 0.25f, 1f),
                    2.2f);
                return;
            }

            // Asegurar GameManager persistente
            if (GameManager.Instance == null)
            {
                var gmGo = new GameObject("GameManager");
                gmGo.AddComponent<GameManager>();
            }

            var gm = GameManager.Instance;
            gm.CollectClue(dimensionClue);
            playerInside = false;

            if (gm.AllCluesCollected)
            {
                gm.LoadFinalRuins();
            }
            else if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                gm.LoadMenu();
            }
        }

        void OnGUI()
        {
            if (!playerInside || !requireInteractKey) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.8f)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            if (screenPos.z < 0f) return;

            bool locked = requireClueInLevel && GameSession.Instance != null && GameSession.Instance.CurrentClues <= 0;
            string msg = locked
                ? "Portal bloqueado (busca una Pista de Lore)"
                : "Presiona [S] para cruzar el Portal";

            float w = locked ? 310f : 250f;
            float h = 32f;
            float x = Mathf.Clamp(screenPos.x - w * 0.5f, 10f, Screen.width - w - 10f);
            float y = Mathf.Clamp(Screen.height - screenPos.y - h, 10f, Screen.height - h - 10f);

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = locked ? new Color(1f, 0.55f, 0.4f, 1f) : new Color(0.45f, 0.95f, 1f, 1f) }
            };

            GUI.Box(new Rect(x, y, w, h), msg, style);
        }
    }
}
