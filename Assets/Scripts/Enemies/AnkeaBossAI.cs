using UnityEngine;
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

            var player = FindFirstObjectByType<PlayerController2D>();
            if (player == null) return;

            // Mirar hacia el jugador
            float dx = player.transform.position.x - transform.position.x;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
            transform.localScale = s;

            // 1. Disparar Orbe Térmico (20 daño)
            if (Time.time >= nextOrbTime && thermalOrbPrefab != null)
            {
                nextOrbTime = Time.time + orbInterval;
                Vector3 origin = firePoint != null ? firePoint.position : transform.position;
                Vector2 dir = (player.transform.position - origin).normalized;

                var orb = Instantiate(thermalOrbPrefab, origin, Quaternion.identity);
                orb.Launch(dir, thermalOrbDamage, thermalOrbSpeed, gameObject);
            }

            // 2. Invocar Columna de Fuego (30 daño) en la posición horizontal del jugador
            if (Time.time >= nextColumnTime && fireColumnPrefab != null)
            {
                nextColumnTime = Time.time + fireColumnInterval;
                Vector3 spawnAt = new Vector3(player.transform.position.x, player.transform.position.y + 0.5f, 0f);
                Instantiate(fireColumnPrefab, spawnAt, Quaternion.identity);
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
