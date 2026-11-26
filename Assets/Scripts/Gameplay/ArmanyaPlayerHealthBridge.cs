using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gamekit2D;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Conecta la vida de Armanya (HealthSystem) con el sistema de vidas/corazones de 2D Gamekit (Damageable y HealthUI),
    /// y gestiona la pantalla de Game Over de 2D Gamekit al morir.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class ArmanyaPlayerHealthBridge : MonoBehaviour
    {
        [Header("Configuración de Vidas (2D Gamekit)")]
        [SerializeField] int totalHearts = 5;

        [Header("Game Over")]
        [SerializeField] float gameOverDelay = 2.5f;

        HealthSystem healthSystem;
        Damageable damageable;
        HealthUI healthUI;
        bool isGameOver = false;

        void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();

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

            // Desvanecer entrada si venimos de un reload / GameOver
            if (ScreenFader.Instance != null && ScreenFader.IsFading)
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

            float ratio = (float)healthSystem.CurrentHealth / Mathf.Max(1, healthSystem.MaxHealth);
            int targetHearts = Mathf.CeilToInt(ratio * totalHearts);
            if (healthSystem.CurrentHealth <= 0) targetHearts = 0;

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
            // Trampas o peligros de 2D Gamekit (ácido, pinchos)
            if (healthSystem != null && !healthSystem.IsDead)
            {
                int hpPerHeart = Mathf.CeilToInt((float)healthSystem.MaxHealth / totalHearts);
                healthSystem.TakeDamage(hpPerHeart * (damager != null ? damager.damage : 1));
            }
        }

        void OnDamageableGainedHealth(int amount, Damageable dmg)
        {
            if (healthSystem != null && !healthSystem.IsDead)
            {
                int hpPerHeart = Mathf.CeilToInt((float)healthSystem.MaxHealth / totalHearts);
                healthSystem.Heal(hpPerHeart * amount);
            }
            if (healthUI != null)
            {
                healthUI.ChangeHitPointUI(damageable);
            }
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

            // 3. Pausa para mostrar la animación / texto de Game Over (permite reiniciar con tecla)
            float timer = 0f;
            while (timer < gameOverDelay)
            {
                timer += Time.unscaledDeltaTime;
                if (timer > 0.8f && (Input.anyKeyDown || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                    break;
                yield return null;
            }

            // 4. Reiniciar escena actual (vidas, monstruos y objetos restaurados)
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
