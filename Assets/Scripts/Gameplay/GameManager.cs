using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Armanya.Gameplay
{
    public enum HeroId
    {
        Angus,
        Anya,
        Aleek
    }

    public enum LoreClueId
    {
        BitacoraHidrica, // Angus - Grieta Árida
        ManifiestoRoto,  // Anya  - Distrito Caos
        ConsolaCentral   // Aleek - Nexo Orbital
    }

    /// <summary>
    /// Singleton persistente (DontDestroyOnLoad) que guarda el progreso global
    /// de las 3 Pistas de Lore (0/3 a 3/3), el personaje seleccionado y controla el flujo de escenas.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Nombres exactos de escenas (Build Settings)")]
        [SerializeField] string menuSceneName = "Menu_Inicio";
        [SerializeField] string grietaAridaScene = "Level1_GrietaArida";
        [SerializeField] string distritoCaosScene = "Level2_DistritoCaos";
        [SerializeField] string nexoOrbitalScene = "Level3_NexoOrbital";
        [SerializeField] string ruinasFinalesScene = "Level4_RuinasFinales";

        [Header("Estado global (solo lectura en ejecución)")]
        [SerializeField] HeroId selectedHero = HeroId.Angus;
        [SerializeField] bool hasBitacoraHidrica;
        [SerializeField] bool hasManifiestoRoto;
        [SerializeField] bool hasConsolaCentral;
        [SerializeField] int score;

        public HeroId SelectedHero => selectedHero;
        public bool HasBitacoraHidrica => hasBitacoraHidrica;
        public bool HasManifiestoRoto => hasManifiestoRoto;
        public bool HasConsolaCentral => hasConsolaCentral;
        public int Score => score;

        public int CollectedCluesCount =>
            (hasBitacoraHidrica ? 1 : 0) +
            (hasManifiestoRoto ? 1 : 0) +
            (hasConsolaCentral ? 1 : 0);

        public bool AllCluesCollected => CollectedCluesCount >= 3;

        public event Action OnProgressChanged;
        public event Action<int> OnScoreChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Registra una de las 3 pistas globales de lore y notifica a la UI.</summary>
        public void CollectClue(LoreClueId clue)
        {
            bool changed = false;
            switch (clue)
            {
                case LoreClueId.BitacoraHidrica:
                    if (!hasBitacoraHidrica) { hasBitacoraHidrica = true; changed = true; }
                    break;
                case LoreClueId.ManifiestoRoto:
                    if (!hasManifiestoRoto) { hasManifiestoRoto = true; changed = true; }
                    break;
                case LoreClueId.ConsolaCentral:
                    if (!hasConsolaCentral) { hasConsolaCentral = true; changed = true; }
                    break;
            }

            if (changed)
            {
                Debug.Log($"[GameManager] Pista guardada: {GetClueDisplayName(clue)} ({CollectedCluesCount}/3)");
                OnProgressChanged?.Invoke();
            }
        }

        public bool HasClue(LoreClueId clue) => clue switch
        {
            LoreClueId.BitacoraHidrica => hasBitacoraHidrica,
            LoreClueId.ManifiestoRoto => hasManifiestoRoto,
            LoreClueId.ConsolaCentral => hasConsolaCentral,
            _ => false
        };

        public static string GetClueDisplayName(LoreClueId clue) => clue switch
        {
            LoreClueId.BitacoraHidrica => "Bitácora Hídrica (Angus)",
            LoreClueId.ManifiestoRoto => "Manifiesto Roto (Anya)",
            LoreClueId.ConsolaCentral => "Consola Central (Aleek)",
            _ => clue.ToString()
        };

        /// <summary>Selecciona héroe y carga su dimensión correspondiente.</summary>
        public void SelectHeroAndPlay(HeroId hero)
        {
            selectedHero = hero;
            string targetScene = hero switch
            {
                HeroId.Angus => grietaAridaScene,
                HeroId.Anya => distritoCaosScene,
                HeroId.Aleek => nexoOrbitalScene,
                _ => grietaAridaScene
            };
            LoadSceneSafe(targetScene);
        }

        public void LoadFinalRuins()
        {
            if (!AllCluesCollected)
            {
                Debug.LogWarning($"[GameManager] Portal de Convergencia bloqueado: faltan pistas ({CollectedCluesCount}/3).");
                return;
            }
            LoadSceneSafe(ruinasFinalesScene);
        }

        public void LoadMenu() => LoadSceneSafe(menuSceneName);

        public void AddScore(int amount, string reason = "")
        {
            if (amount <= 0) return;
            score += amount;
            string detail = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
            Debug.Log($"[Puntaje] +{amount} pts{detail} -> Total: {score}");
            OnScoreChanged?.Invoke(score);
        }

        public void ResetProgress()
        {
            hasBitacoraHidrica = false;
            hasManifiestoRoto = false;
            hasConsolaCentral = false;
            score = 0;
            Debug.Log("[GameManager] Progreso global y puntaje reiniciados (0/3, 0 pts).");
            OnProgressChanged?.Invoke();
            OnScoreChanged?.Invoke(score);
        }

        void LoadSceneSafe(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"[GameManager] La escena '{sceneName}' no está agregada en File > Build Profiles > Scene List.");
            }
        }
    }
}
