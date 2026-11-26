using UnityEngine;
using UnityEngine.InputSystem;
using Armanya.Gameplay;
using Armanya.Player;

namespace Armanya.Interactables
{
    /// <summary>
    /// Colócalo en la Pista de Lore principal (o portal final) de cada una de las 3 dimensiones:
    /// - Grieta Árida  -> BitacoraHidrica
    /// - Distrito Caos -> ManifiestoRoto
    /// - Nexo Orbital  -> ConsolaCentral
    ///
    /// Al tocarlo el jugador, guarda el progreso en GameManager (DontDestroyOnLoad) y muestra
    /// un aviso/portal en pantalla para volver al Selector (Menu_Inicio) o ir directo a Ruinas Finales si ya son 3/3.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DimensionCluePortal : MonoBehaviour
    {
        [Header("Pista de esta dimensión")]
        [SerializeField] LoreClueId clueId = LoreClueId.BitacoraHidrica;

        [Header("Visual opcional a ocultar al recogerla")]
        [SerializeField] SpriteRenderer visualRenderer;

        bool triggered;
        bool showTransitionPanel;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            if (visualRenderer == null) visualRenderer = GetComponent<SpriteRenderer>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            if (!IsPlayer(other)) return;

            triggered = true;

            // Asegura GameManager aunque se pruebe la escena directamente desde el Editor
            if (GameManager.Instance == null)
            {
                var gmGo = new GameObject("GameManager");
                gmGo.AddComponent<GameManager>();
            }

            GameManager.Instance.CollectClue(clueId);

            if (visualRenderer != null) visualRenderer.enabled = false;
            showTransitionPanel = true;
        }

        void Update()
        {
            if (!showTransitionPanel) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // Atajo rápido con Enter o E
            if (kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
            {
                GoNext();
            }
        }

        void GoNext()
        {
            showTransitionPanel = false;
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.AllCluesCollected)
                gm.LoadFinalRuins();
            else
                gm.LoadMenu();
        }

        bool IsPlayer(Collider2D other) =>
            other.CompareTag("Player") ||
            other.GetComponent<PlayerController2D>() != null ||
            other.GetComponentInParent<PlayerController2D>() != null;

        void OnGUI()
        {
            if (!showTransitionPanel || GameManager.Instance == null) return;

            var gm = GameManager.Instance;
            bool allDone = gm.AllCluesCollected;
            string clueTitle = GameManager.GetClueDisplayName(clueId);

            float w = 440f;
            float h = 180f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "★ PORTAL DE TRANSICIÓN DIMENSIONAL ★");

            string body = allDone
                ? $"¡Recolectaste: {clueTitle}!\n\nProgreso global: {gm.CollectedCluesCount} / 3 (¡COMPLETO!)\nEl Portal de Convergencia está abierto."
                : $"¡Recolectaste: {clueTitle}!\n\nProgreso global guardado: {gm.CollectedCluesCount} / 3\nRegresa al selector para explorar otra dimensión.";

            GUI.Label(new Rect(x + 20, y + 32, w - 40, 75), body);

            if (allDone)
            {
                if (GUI.Button(new Rect(x + 20, y + 120, 210, 40), "Ir a Ruinas Finales (Enter)"))
                    gm.LoadFinalRuins();

                if (GUI.Button(new Rect(x + 245, y + 120, 175, 40), "Volver al Selector"))
                    gm.LoadMenu();
            }
            else
            {
                if (GUI.Button(new Rect(x + 95, y + 120, 250, 40), "Volver al Selector (Enter)"))
                    gm.LoadMenu();
            }
        }
    }
}
