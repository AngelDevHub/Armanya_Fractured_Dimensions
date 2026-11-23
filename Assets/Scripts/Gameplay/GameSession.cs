using System;
using UnityEngine;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Controla el progreso del nivel: pistas de lore y HUD de prueba.
    /// Singleton simple y accesible para cualquier recolectable.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Pistas de Lore")]
        [SerializeField] int totalCluesInLevel = 3;
        [SerializeField] bool showDebugHud = true;

        public int CurrentClues { get; private set; }
        public int TotalClues => totalCluesInLevel;

        public event Action<int, int> OnClueCollected; // (current, total)

        Player.PlayerHealth cachedPlayerHealth;

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
        }

        /// <summary>Añade una pista de lore al contador del nivel.</summary>
        public void CollectLoreClue(string clueTitle = "")
        {
            CurrentClues = Mathf.Min(totalCluesInLevel, CurrentClues + 1);
            string info = string.IsNullOrEmpty(clueTitle) ? "" : $" (\"{clueTitle}\")";
            Debug.Log($"[Lore] ¡Pista de Lore recogida!{info} -> Progreso: {CurrentClues}/{totalCluesInLevel}");

            OnClueCollected?.Invoke(CurrentClues, totalCluesInLevel);

            if (CurrentClues >= totalCluesInLevel)
            {
                Debug.Log("[Lore] ★ ¡Todas las pistas de lore de esta dimensión han sido recuperadas! ★");
            }
        }

        // ---------- HUD de prueba rápido en pantalla ----------
        void OnGUI()
        {
            if (!showDebugHud) return;

            // Caja de fondo
            GUI.Box(new Rect(10, 68, 230, 60), "Armanya: Estado");

            // Barra de vida
            if (cachedPlayerHealth != null)
            {
                float hp = cachedPlayerHealth.CurrentHealth;
                float max = cachedPlayerHealth.MaxHealth;
                GUI.Label(new Rect(20, 88, 210, 20), $"HP: {hp:F0} / {max:F0}");
            }
            else
            {
                GUI.Label(new Rect(20, 88, 210, 20), "HP: (sin PlayerHealth)");
            }

            // Contador de pistas
            string loreText = $"Pistas de Lore: {CurrentClues} / {totalCluesInLevel}";
            if (CurrentClues >= totalCluesInLevel) loreText += " (¡COMPLETO!)";
            GUI.Label(new Rect(20, 105, 210, 20), loreText);
        }
    }
}
