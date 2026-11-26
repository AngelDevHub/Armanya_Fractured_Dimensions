using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gamekit2D;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Conecta la vida de Armanya (HealthSystem) con los corazones de 2D Gamekit (Damageable y HealthUI):
    ///   - Cada corazón equivale a 100% de vida (100 HP por corazón; 5 corazones = 500 HP).
    ///   - Al perder todos los corazones activa la pantalla oficial de Game Over de 2D Gamekit
    ///     con botones interactivos: "Volver a Intentar" y "Salir del Nivel".
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class ArmanyaPlayerHealthBridge : MonoBehaviour
    {
        [Header("Configuración de Vidas (Cada corazón = 100% / 100 HP)")]
        [SerializeField] int totalHearts = 5;
        [SerializeField] int hpPerHeart = 100;

        [Header("Game Over")]
        [SerializeField] string menuSceneName = "Menu_Inicio";

        HealthSystem healthSystem;
        Damageable damageable;
        HealthUI healthUI;
        bool isGameOver;
        bool showGameOverButtons;
        bool syncingFromDamageable;

        public int HpPerHeart => hpPerHeart;

        void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                // Cada corazón vale 100 HP (ej. 5 corazones = 500 HP)
                healthSystem.ConfigureMaxHealth(totalHearts * hpPerHeart, true);
            }

            // Asegurar Damageable de 2D Gamekit en el jugador
            damageable = GetComponent<Damageable>();
            if (damageable == null)
            {
                damageable = gameObject.AddComponent<Damageable>();
            }

            damageable.startingHealth = totalHearts;
            damageable.invulnerableAfterDamage = true;
            damageable.invulnerabilityDuration = 1.0f;
            damageable.disableOnDeath = false;
        }

        void Start()
        {
            ConnectUI();

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += OnHealthSystemChanged;
                healthSystem.OnDeath += OnPlayerDied;
            }

            if (damageable != null)
            {
                damageable.OnTakeDamage.AddListener(OnDamageableTookDamage);
                damageable.OnDie.AddListener(OnDamageableDied);
                damageable.OnGainHealth.AddListener(OnDamageableGainedHealth);
            }

            SyncHearts();

            // Ocultar diálogos tutoriales viejos de 2DGamekit (Ellen en inglés) si hubiera alguno activo
            var oldDialogs = Object.FindObjectsByType<DialogueCanvasController>(FindObjectsInactive.Include);
            foreach (var d in oldDialogs)
            {
                if (d != null) d.gameObject.SetActive(false);
            }

            // Desvanecer entrada si venimos de un reload / GameOver
            if (ScreenFader.Instance != null)
            {
                StartCoroutine(ScreenFader.FadeSceneIn());
            }
        }

        void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= OnHealthSystemChanged;
                healthSystem.OnDeath -= OnPlayerDied;
            }
        }

        public void ConnectUI()
        {
            if (healthUI == null)
            {
                healthUI = Object.FindFirstObjectByType<HealthUI>();
            }

            if (healthUI != null && damageable != null)
            {
                healthUI.representedDamageable = damageable;
                healthUI.ChangeHitPointUI(damageable);
            }
        }

        void OnHealthSystemChanged(int current, int max)
        {
            SyncHearts();
        }

        void SyncHearts()
        {
            if (damageable == null || healthSystem == null) return;

            int targetHearts = healthSystem.CurrentHealth <= 0
                ? 0
                : Mathf.Clamp(Mathf.CeilToInt((float)healthSystem.CurrentHealth / hpPerHeart), 0, totalHearts);

            if (damageable.CurrentHealth != targetHearts)
            {
                damageable.SetHealth(targetHearts);
            }

            if (healthUI != null)
            {
                healthUI.ChangeHitPointUI(damageable);
            }
        }

        void OnDamageableTookDamage(Damager damager, Damageable dmg)
        {
            // Trampas o peligros de 2D Gamekit (ácido, pinchos): quitan 1 corazón completo (100 HP)
            if (healthSystem != null && !healthSystem.IsDead)
            {
                int heartsLost = damager != null ? Mathf.Max(1, damager.damage) : 1;
                healthSystem.TakeDamage(hpPerHeart * heartsLost);
            }
        }

        void OnDamageableGainedHealth(int amount, Damageable dmg)
        {
            if (syncingFromDamageable) return;
            syncingFromDamageable = true;

            if (healthSystem != null && !healthSystem.IsDead)
            {
                healthSystem.Heal(hpPerHeart * amount);
            }
            if (healthUI != null)
            {
                healthUI.ChangeHitPointUI(damageable);
            }

            syncingFromDamageable = false;
        }

        void OnDamageableDied(Damager damager, Damageable dmg)
        {
            TriggerGameOver();
        }

        void OnPlayerDied()
        {
            TriggerGameOver();
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;

            StartCoroutine(GameOverRoutine());
        }

        IEnumerator GameOverRoutine()
        {
            Debug.Log("[Armanya] ¡GAME OVER! Mostrando pantalla de Game Over de 2D Gamekit...", this);

            // 1. Desactivar control del jugador
            var ctrl = GetComponent<Armanya.Player.PlayerController2D>();
            if (ctrl != null) ctrl.enabled = false;
            var user = GetComponent<Armanya.Abilities.AbilityUser>();
            if (user != null) user.enabled = false;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // 2. Fundido con pantalla GAME OVER de 2D Gamekit
            if (ScreenFader.Instance != null)
            {
                yield return StartCoroutine(ScreenFader.FadeSceneOut(ScreenFader.FadeType.GameOver));
            }

            // 3. Mostrar botones interactivos para Reintentar o Salir del Nivel
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            showGameOverButtons = true;
        }

        void OnGUI()
        {
            if (!showGameOverButtons) return;

            GUI.depth = -100;

            float btnW = 240f;
            float btnH = 52f;
            float spacing = 24f;
            float totalW = btnW * 2f + spacing;
            float startX = (Screen.width - totalW) * 0.5f;
            float posY = Screen.height * 0.68f;

            var retryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            var exitStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.78f, 0.78f, 1f) }
            };

            if (GUI.Button(new Rect(startX, posY, btnW, btnH), "Volver a Intentar", retryStyle))
            {
                showGameOverButtons = false;
                var activeScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(activeScene.buildIndex);
            }

            if (GUI.Button(new Rect(startX + btnW + spacing, posY, btnW, btnH), "Salir del Nivel", exitStyle))
            {
                showGameOverButtons = false;
                if (GameManager.Instance != null)
                    GameManager.Instance.LoadMenu();
                else if (Application.CanStreamedLevelBeLoaded(menuSceneName))
                    SceneManager.LoadScene(menuSceneName);
                else
                    SceneManager.LoadScene(0);
            }
        }
    }
}
