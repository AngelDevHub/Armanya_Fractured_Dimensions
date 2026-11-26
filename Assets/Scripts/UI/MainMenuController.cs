using UnityEngine;
using UnityEngine.UI;
using Armanya.Gameplay;

namespace Armanya.UI
{
    /// <summary>
    /// Controla la escena Menu_Inicio:
    /// - 3 botones de héroe (Angus -> Grieta Árida, Anya -> Distrito Caos, Aleek -> Nexo Orbital)
    /// - Botón Portal de Convergencia (Ruinas Finales), desbloqueado automáticamente al llegar a 3/3 pistas
    /// - Indicador global de pistas (0/3 a 3/3)
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Botones de Selección de Personaje")]
        [SerializeField] Button angusButton;
        [SerializeField] Button anyaButton;
        [SerializeField] Button aleekButton;

        [Header("Portal de Convergencia (Ruinas Finales)")]
        [SerializeField] Button convergencePortalButton;
        [SerializeField] Text convergenceButtonLabel;

        [Header("Textos de Progreso de Lore")]
        [SerializeField] Text loreProgressText;
        [SerializeField] Text angusClueStatusText;
        [SerializeField] Text anyaClueStatusText;
        [SerializeField] Text aleekClueStatusText;

        [Header("Opcional: Reiniciar progreso")]
        [SerializeField] Button resetProgressButton;

        void Start()
        {
            EnsureGameManagerExists();

            if (angusButton != null) angusButton.onClick.AddListener(OnSelectAngus);
            if (anyaButton != null) anyaButton.onClick.AddListener(OnSelectAnya);
            if (aleekButton != null) aleekButton.onClick.AddListener(OnSelectAleek);
            if (convergencePortalButton != null) convergencePortalButton.onClick.AddListener(OnEnterConvergencePortal);
            if (resetProgressButton != null) resetProgressButton.onClick.AddListener(OnResetProgress);

            if (GameManager.Instance != null)
                GameManager.Instance.OnProgressChanged += RefreshUI;

            RefreshUI();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnProgressChanged -= RefreshUI;
        }

        void EnsureGameManagerExists()
        {
            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
        }

        public void RefreshUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            int count = gm.CollectedCluesCount;
            bool unlocked = gm.AllCluesCollected;

            if (loreProgressText != null)
            {
                loreProgressText.text = unlocked
                    ? $"Pistas de Lore: {count} / 3  —  ¡PORTAL DE CONVERGENCIA DESBLOQUEADO!"
                    : $"Pistas de Lore: {count} / 3";
            }

            if (angusClueStatusText != null)
                angusClueStatusText.text = gm.HasBitacoraHidrica ? "[✓] Bitácora Hídrica" : "[ ] Bitácora Hídrica";

            if (anyaClueStatusText != null)
                anyaClueStatusText.text = gm.HasManifiestoRoto ? "[✓] Manifiesto Roto" : "[ ] Manifiesto Roto";

            if (aleekClueStatusText != null)
                aleekClueStatusText.text = gm.HasConsolaCentral ? "[✓] Consola Central" : "[ ] Consola Central";

            if (convergencePortalButton != null)
                convergencePortalButton.interactable = unlocked;

            if (convergenceButtonLabel != null)
            {
                convergenceButtonLabel.text = unlocked
                    ? "ENTRAR AL PORTAL DE CONVERGENCIA\n(Nivel 4: Ruinas Finales)"
                    : $"PORTAL DE CONVERGENCIA BLOQUEADO\n(Recolecta las 3 Pistas: {count}/3)";
            }
        }

        public void OnSelectAngus() => GameManager.Instance?.SelectHeroAndPlay(HeroId.Angus);
        public void OnSelectAnya() => GameManager.Instance?.SelectHeroAndPlay(HeroId.Anya);
        public void OnSelectAleek() => GameManager.Instance?.SelectHeroAndPlay(HeroId.Aleek);
        public void OnEnterConvergencePortal() => GameManager.Instance?.LoadFinalRuins();
        public void OnResetProgress() => GameManager.Instance?.ResetProgress();
    }
}
