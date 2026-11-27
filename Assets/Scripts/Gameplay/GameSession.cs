using System;
using UnityEngine;
using Gamekit2D;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Controla el progreso del nivel: pistas de lore (enlazadas al KeyUI superior derecho),
    /// puntaje y notificaciones visibles al recoger vida o pistas.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Pistas de Lore")]
        [SerializeField] int totalCluesInLevel = 3;
        [SerializeField] bool showScoreHud = true;

        public int CurrentClues { get; private set; }
        public int TotalClues => totalCluesInLevel;
        public int CurrentScore => GameManager.Instance != null ? GameManager.Instance.Score : localScore;

        public event Action<int, int> OnClueCollected; // (current, total)
        public event Action<int> OnScoreUpdated;

        Player.PlayerHealth cachedPlayerHealth;
        int localScore;

        // Notificación flotante visible al recoger vida / pistas
        string popupText;
        Color popupColor = Color.white;
        float popupTimer;
        float popupDuration = 2.2f;

        /// <summary>Suma puntos al marcador global (o local si se prueba la escena directamente).</summary>
        public static void AddPoints(int points, string reason = "")
        {
            if (points <= 0) return;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(points, reason);
            }
            else if (Instance != null)
            {
                Instance.localScore += points;
                string detail = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
                Debug.Log($"[Puntaje] +{points} pts{detail} -> Total: {Instance.localScore}");
            }

            if (Instance != null)
                Instance.OnScoreUpdated?.Invoke(Instance.CurrentScore);
        }

        /// <summary>Muestra un aviso visual llamativo en pantalla al recoger vida, orbes o pistas.</summary>
        public static void ShowPickupNotification(string message, Color color, float duration = 2.2f)
        {
            if (Instance == null) return;
            Instance.popupText = message;
            Instance.popupColor = color;
            Instance.popupDuration = duration;
            Instance.popupTimer = duration;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            cachedPlayerHealth = FindFirstObjectByType<Player.PlayerHealth>();
            EnsureCameraFollowsPlayer();
            DisableLegacyEllenDialogues();
            SyncKeyUI();
        }

        void EnsureCameraFollowsPlayer()
        {
            Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) return;

            var follow = cam.GetComponent<CameraSystem.CameraFollow2D>();
            if (follow == null)
            {
                follow = cam.gameObject.AddComponent<CameraSystem.CameraFollow2D>();
            }

            var player = FindFirstObjectByType<Player.PlayerController2D>();
            if (player != null)
            {
                follow.SetTarget(player.transform);
            }

            // Garantizar que siempre exista exactamente 1 AudioListener activo en la escena
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            AudioListener activeListener = null;
            foreach (var l in listeners)
            {
                if (l == null) continue;
                if (activeListener == null && l.enabled && l.gameObject.activeInHierarchy)
                {
                    activeListener = l;
                }
                else if (l != activeListener)
                {
                    l.enabled = false;
                }
            }

            if (activeListener == null)
            {
                var camListener = cam.GetComponent<AudioListener>();
                if (camListener == null) camListener = cam.gameObject.AddComponent<AudioListener>();
                camListener.enabled = true;
            }
        }

        void DisableLegacyEllenDialogues()
        {
            // Apagar definitivamente todos los cuadros de diálogo viejos de Ellen (2DGamekit)
            var oldDialogs = FindObjectsByType<DialogueCanvasController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var d in oldDialogs)
            {
                if (d != null) d.gameObject.SetActive(false);
            }

            // Apagar todos los InfoPost heredados de 2DGamekit para que no vuelvan a abrir mensajes de Ellen al pasar
            var interactables = FindObjectsByType<InteractOnTrigger2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var trigger in interactables)
            {
                if (trigger == null) continue;
                if (trigger.gameObject.name.IndexOf("InfoPost", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    trigger.gameObject.SetActive(false);
                }
            }
        }

        void Update()
        {
            if (popupTimer > 0f)
                popupTimer -= Time.unscaledDeltaTime;
        }

        /// <summary>Cambia qué héroe está activo (lo llama el TagTeamManager al alternar).</summary>
        public void SetPlayerHealth(Player.PlayerHealth health) => cachedPlayerHealth = health;

        /// <summary>Añade una pista de lore al contador del nivel, marca el icono superior derecho y otorga puntos.</summary>
        public void CollectLoreClue(string clueTitle = "")
        {
            CurrentClues = Mathf.Min(totalCluesInLevel, CurrentClues + 1);
            AddPoints(250, string.IsNullOrEmpty(clueTitle) ? "Pista de Lore" : clueTitle);
            SyncKeyUI();

            string label = string.IsNullOrEmpty(clueTitle) ? "Pista de Lore" : clueTitle;
            ShowPickupNotification($"★ {label} ({CurrentClues}/{totalCluesInLevel}) ★", new Color(1f, 0.9f, 0.25f, 1f), 2.5f);

            string info = string.IsNullOrEmpty(clueTitle) ? "" : $" (\"{clueTitle}\")";
            Debug.Log($"[Lore] ¡Pista de Lore recogida!{info} -> Progreso: {CurrentClues}/{totalCluesInLevel}");

            OnClueCollected?.Invoke(CurrentClues, totalCluesInLevel);

            if (CurrentClues >= totalCluesInLevel)
            {
                Debug.Log("[Lore] ★ ¡Todas las pistas de lore de esta dimensión han sido recuperadas! ★");
            }
        }

        void SyncKeyUI()
        {
            if (KeyUI.Instance != null)
            {
                KeyUI.Instance.SetCollectedCount(CurrentClues);
            }
        }

        // ---------- HUD limpio en esquina superior derecha + aviso de recolección ----------
        void OnGUI()
        {
            // 1. Puntaje discreto en la esquina superior derecha (justo debajo de los iconos de pistas)
            if (showScoreHud)
            {
                var scoreStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(1f, 0.95f, 0.65f, 1f) }
                };
                GUI.Box(new Rect(Screen.width - 195, 62, 180, 28), $"Puntaje: {CurrentScore} pts  ", scoreStyle);
            }

            // 2. Aviso visual llamativo al recoger vida, orbes o pistas
            if (popupTimer > 0f && !string.IsNullOrEmpty(popupText))
            {
                float alpha = Mathf.Clamp01(popupTimer / Mathf.Min(0.6f, popupDuration));
                float rise = (1f - Mathf.Clamp01(popupTimer / popupDuration)) * 28f;

                var prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);

                var boxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = popupColor }
                };

                float w = 380f;
                float h = 44f;
                float x = (Screen.width - w) * 0.5f;
                float y = 95f - rise;
                GUI.Box(new Rect(x, y, w, h), popupText, boxStyle);

                GUI.color = prevColor;
            }
        }
    }
}
