using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Armanya.Gameplay;
using Armanya.Player;

namespace Armanya.Enemies
{
    public enum EnemyBehaviorMode
    {
        GroundMelee,  // Bestia de Caos, Monstruo de Tres Ojos
        FlyingRanged  // Ojo Fantasma, Centinela Tentacular
    }

    /// <summary>
    /// Controlador modular de enemigos 2D compatible con HealthSystem y estado de aturdimiento (IsStunned).
    /// Soporta Modo Terrestre Cuerpo a Cuerpo y Modo Volador/Distancia.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class EnemyAI2D : MonoBehaviour
    {
        [Header("Modo de Comportamiento")]
        [SerializeField] EnemyBehaviorMode mode = EnemyBehaviorMode.GroundMelee;

        [Header("Detección")]
        [SerializeField] float detectionRadius = 8f;

        [Header("Modo Terrestre Cuerpo a Cuerpo")]
        [SerializeField] float patrolSpeed = 2.2f;
        [SerializeField] float chaseSpeed = 4.0f;
        [Tooltip("Distancia a izquierda/derecha desde su punto inicial si no se asignan Waypoints.")]
        [SerializeField] float patrolDistance = 3.5f;
        [SerializeField] Transform patrolPointA;
        [SerializeField] Transform patrolPointB;
        [SerializeField] float meleeAttackRange = 1.6f;

        [Header("Modo Volador / Distancia")]
        [Tooltip("Altura de flotación (0 para Centinela estático en suelo).")]
        [SerializeField] float hoverAmplitude = 0.35f;
        [SerializeField] float hoverSpeed = 2.2f;
        [SerializeField] EnemyProjectile2D projectilePrefab;
        [SerializeField] Transform firePoint;
        [SerializeField] float projectileSpeed = 8f;

        [Header("Ataque")]
        [SerializeField] int attackDamage = 15;
        [SerializeField] float attackCooldown = 1.8f;

        [Header("Orientación del Sprite")]
        [Tooltip("Marca true si tu sprite original mira hacia la derecha (nuestros *_Side.png miran a la derecha).")]
        [SerializeField] bool spriteFacesRightByDefault = true;

        [Header("Navegación NavMesh y Optimización (Criterio AU)")]
        [Tooltip("Usa Unity AI Navigation (NavMeshAgent) automáticamente si hay un NavMesh horneado en la escena.")]
        [SerializeField] bool useNavMeshIfAvailable = true;
        [Tooltip("Intervalo en segundos de la corrutina de búsqueda del jugador (evita picos de CPU en Update).")]
        [SerializeField, Min(0.05f)] float perceptionInterval = 0.2f;

        HealthSystem health;
        Rigidbody2D rb;
        NavMeshAgent navAgent;
        Transform cachedPlayer;
        WaitForSeconds perceptionWait;
        Vector3 spawnPos;
        float leftBoundX;
        float rightBoundX;
        int patrolDir = 1;
        float nextAttackTime;
        float baseScaleX;

        bool CanUseNavMesh => useNavMeshIfAvailable && navAgent != null && navAgent.enabled && navAgent.isOnNavMesh;

        void Awake()
        {
            health = GetComponent<HealthSystem>();
            rb = GetComponent<Rigidbody2D>();
            navAgent = GetComponent<NavMeshAgent>();
            perceptionWait = new WaitForSeconds(perceptionInterval);
            spawnPos = transform.position;
            baseScaleX = Mathf.Abs(transform.localScale.x);

            if (navAgent != null)
            {
                navAgent.updateRotation = false;
                navAgent.updateUpAxis = false;
            }

            if (patrolPointA != null && patrolPointB != null)
            {
                leftBoundX = Mathf.Min(patrolPointA.position.x, patrolPointB.position.x);
                rightBoundX = Mathf.Max(patrolPointA.position.x, patrolPointB.position.x);
            }
            else
            {
                leftBoundX = spawnPos.x - patrolDistance;
                rightBoundX = spawnPos.x + patrolDistance;
            }

            if (rb != null)
            {
                rb.freezeRotation = true;
                if (mode == EnemyBehaviorMode.FlyingRanged)
                    rb.gravityScale = 0f;
            }
        }

        void OnEnable()
        {
            StartCoroutine(PlayerPerceptionRoutine());
        }

        IEnumerator PlayerPerceptionRoutine()
        {
            // Corrutina cada 0.2s para evitar FindFirstObjectByType por frame (optimización Profiler AU)
            while (enabled)
            {
                cachedPlayer = FindActivePlayer();
                yield return perceptionWait;
            }
        }

        void Update()
        {
            if (health == null || health.IsDead) return;

            // Si está aturdido por el poder K de un héroe, detener movimiento y ataques
            if (health.IsStunned)
            {
                StopHorizontal();
                return;
            }

            Transform player = cachedPlayer;

            if (mode == EnemyBehaviorMode.GroundMelee)
                UpdateGroundMelee(player);
            else
                UpdateFlyingRanged(player);
        }

        void UpdateGroundMelee(Transform player)
        {
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= detectionRadius)
                {
                    float dx = player.position.x - transform.position.x;
                    FaceDirection(dx >= 0f ? 1 : -1);

                    if (dist <= meleeAttackRange)
                    {
                        StopHorizontal();
                        if (Time.time >= nextAttackTime)
                        {
                            nextAttackTime = Time.time + attackCooldown;
                            DealMeleeDamage(player);
                        }
                    }
                    else
                    {
                        if (CanUseNavMesh)
                        {
                            navAgent.speed = chaseSpeed;
                            navAgent.isStopped = false;
                            navAgent.SetDestination(player.position);
                        }
                        else
                        {
                            MoveHorizontal(Mathf.Sign(dx) * chaseSpeed);
                        }
                    }
                    return;
                }
            }

            // Patrulla entre dos puntos
            if (transform.position.x >= rightBoundX) patrolDir = -1;
            else if (transform.position.x <= leftBoundX) patrolDir = 1;

            FaceDirection(patrolDir);
            if (CanUseNavMesh)
            {
                navAgent.speed = patrolSpeed;
                navAgent.isStopped = false;
                float targetX = patrolDir > 0 ? rightBoundX : leftBoundX;
                navAgent.SetDestination(new Vector3(targetX, spawnPos.y, spawnPos.z));
            }
            else
            {
                MoveHorizontal(patrolDir * patrolSpeed);
            }
        }

        void UpdateFlyingRanged(Transform player)
        {
            // Flotación suave (si hoverAmplitude > 0)
            if (hoverAmplitude > 0.001f)
            {
                float y = spawnPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
            }

            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist > detectionRadius) return;

            float dx = player.position.x - transform.position.x;
            FaceDirection(dx >= 0f ? 1 : -1);

            if (Time.time >= nextAttackTime && projectilePrefab != null)
            {
                nextAttackTime = Time.time + attackCooldown;
                Vector3 origin = firePoint != null ? firePoint.position : transform.position;
                Vector2 dir = (player.position - origin).normalized;

                var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
                proj.Launch(dir, attackDamage, projectileSpeed, gameObject);
            }
        }

        void DealMeleeDamage(Transform player)
        {
            var hs = player.GetComponent<HealthSystem>();
            if (hs != null && hs.IsInvulnerable) return;

            if (hs != null) hs.TakeDamage(attackDamage);

            var ph = player.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(attackDamage, gameObject);

            Debug.Log($"[{name}] ¡Ataque cuerpo a cuerpo al jugador (-{attackDamage} HP)!", this);
        }

        void MoveHorizontal(float vx)
        {
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            else
                transform.position += new Vector3(vx * Time.deltaTime, 0f, 0f);
        }

        void StopHorizontal()
        {
            if (CanUseNavMesh)
                navAgent.isStopped = true;

            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        void FaceDirection(int dir)
        {
            float sign = spriteFacesRightByDefault ? dir : -dir;
            Vector3 s = transform.localScale;
            s.x = baseScaleX * sign;
            transform.localScale = s;
        }

        Transform FindActivePlayer()
        {
            // Encuentra siempre al héroe activo (funciona tanto en niveles 1-3 como en Tag-Team del Nivel 4)
            var ctrl = FindFirstObjectByType<PlayerController2D>();
            return ctrl != null ? ctrl.transform : null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (mode == EnemyBehaviorMode.GroundMelee)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
                Gizmos.color = Color.cyan;
                Vector3 basePos = Application.isPlaying ? spawnPos : transform.position;
                Gizmos.DrawLine(basePos + Vector3.left * patrolDistance, basePos + Vector3.right * patrolDistance);
            }
        }
    }
}
