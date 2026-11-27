using UnityEngine;
using UnityEngine.SceneManagement;
using Armanya.Gameplay;
using Armanya.Interactables;
using Armanya.Player;

namespace Armanya.Enemies
{
    /// <summary>
    /// Ataques periódicos de Ankea en el Nivel 4 (Ruinas Finales):
    /// - Dispara Orbes Térmicos (20 daño)
    /// - Invoca Columnas de Fuego (30 daño) cerca del jugador
    /// Se detiene automáticamente en cuanto los 3 Generadores de Fractura tengan isRepaired = true.
    /// </summary>
    public class AnkeaBossAI : MonoBehaviour
    {
        [Header("Referencias de Nivel 4")]
        [SerializeField] FractureGeneratorManager generatorManager;

        [Header("Ataque 1: Orbes Térmicos (20 daño)")]
        [SerializeField] EnemyProjectile2D thermalOrbPrefab;
        [SerializeField] Transform firePoint;
        [SerializeField] int thermalOrbDamage = 20;
        [SerializeField] float thermalOrbSpeed = 9f;
        [SerializeField] float orbInterval = 3.2f;

        [Header("Ataque 2: Columnas de Fuego (30 daño)")]
        [SerializeField] FireColumnHazard fireColumnPrefab;
        [SerializeField] float fireColumnInterval = 5.5f;

        [Header("Flotación")]
        [SerializeField] float hoverAmplitude = 0.4f;
        [SerializeField] float hoverSpeed = 1.8f;

        Vector3 startPos;
        float nextOrbTime;
        float nextColumnTime;
        bool neutralized;
        Transform cachedPlayer;
        float nextPlayerScanTime;
        GUIStyle victoryBoxStyle;

        void Start()
        {
            startPos = transform.position;
            if (generatorManager == null)
                generatorManager = FindFirstObjectByType<FractureGeneratorManager>();

            nextOrbTime = Time.time + 1.5f;
            nextColumnTime = Time.time + 3.0f;
        }

        void Update()
        {
            if (neutralized) return;

            // Verificar si los 3 Generadores de Fractura ya están reparados
            if (AreAllGeneratorsRepaired())
            {
                neutralized = true;
                Debug.Log("[Ankea] ★ ¡Los 3 Generadores de Fractura han sido reparados! Ankea detiene sus ataques. ★", this);
                return;
            }

            // Flotación de jefe
            transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude, 0f);

            // Cacheo eficiente del jugador (evita FindFirstObjectByType cada frame para AU 4.1 Profiler)
            if (cachedPlayer == null || !cachedPlayer.gameObject.activeInHierarchy || Time.time >= nextPlayerScanTime)
            {
                nextPlayerScanTime = Time.time + 0.3f;
                var pc = FindFirstObjectByType<PlayerController2D>();
                cachedPlayer = pc != null ? pc.transform : null;
            }
            if (cachedPlayer == null) return;

            // Mirar hacia el jugador
            float dx = cachedPlayer.position.x - transform.position.x;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
            transform.localScale = s;

            // 1. Disparar Orbe Térmico (20 daño)
            if (Time.time >= nextOrbTime && thermalOrbPrefab != null)
            {
                nextOrbTime = Time.time + orbInterval;
                Vector3 origin = firePoint != null ? firePoint.position : transform.position;
                Vector2 dir = (cachedPlayer.position - origin).normalized;

                var orb = Instantiate(thermalOrbPrefab, origin, Quaternion.identity);
                orb.Launch(dir, thermalOrbDamage, thermalOrbSpeed, gameObject);
            }

            // 2. Invocar Columna de Fuego (30 daño) en la posición horizontal del jugador
            if (Time.time >= nextColumnTime && fireColumnPrefab != null)
            {
                nextColumnTime = Time.time + fireColumnInterval;
                Vector3 spawnAt = new Vector3(cachedPlayer.position.x, cachedPlayer.position.y + 0.5f, 0f);
                Instantiate(fireColumnPrefab, spawnAt, Quaternion.identity);
            }
        }

        void OnGUI()
        {
            if (!neutralized) return;

            if (victoryBoxStyle == null)
            {
                victoryBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    padding = new RectOffset(22, 22, 18, 18)
                };
                victoryBoxStyle.normal.textColor = new Color(0.75f, 1f, 0.9f, 1f);
            }

            int finalScore = GameSession.Instance != null ? GameSession.Instance.CurrentScore : 0;

            float w = Mathf.Min(660f, Screen.width - 40f);
            float h = 230f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.28f;

            GUI.Box(
                new Rect(x, y, w, h),
                "★ BRECHA ESTABILIZADA — ¡HAS GANADO! ★\n\n" +
                "Los 3 Generadores de Fractura han sido reparados.\n" +
                "Angus, Anya y Aleek demostraron que ninguna crisis se supera con apatía ni violencia,\n" +
                "sino con empatía colectiva y responsabilidad compartida.\n\n" +
                $"Puntaje Final: {finalScore} pts",
                victoryBoxStyle
            );

            float btnW = 200f;
            float btnH = 42f;
            float btnY = y + h - btnH - 16f;
            float gap = 24f;

            if (GUI.Button(new Rect(x + (w * 0.5f) - btnW - (gap * 0.5f), btnY, btnW, btnH), "Jugar de Nuevo"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Level1_GrietaArida");
            }

            if (GUI.Button(new Rect(x + (w * 0.5f) + (gap * 0.5f), btnY, btnW, btnH), "Volver al Menú"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Menu_Inicio");
            }
        }

        bool AreAllGeneratorsRepaired()
        {
            if (generatorManager != null)
                return generatorManager.AllRepaired;

            var gens = FindObjectsByType<FractureGenerator>(FindObjectsSortMode.None);
            if (gens.Length == 0) return false;
            foreach (var g in gens)
                if (!g.IsRepaired) return false;
            return true;
        }
    }
}
