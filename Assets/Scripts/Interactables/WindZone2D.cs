using UnityEngine;
using Armanya.Player;

namespace Armanya.Interactables
{
    /// <summary>
    /// Zona de viento lateral (Nivel 1: Grieta Árida - Represa Abandonada).
    /// Aplica ráfagas de viento intermitentes que empujan al jugador físicamente
    /// y muestra partículas de polvo/viento para dar claridad visual.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WindZone2D : MonoBehaviour
    {
        [Header("Fuerza del Viento")]
        [Tooltip("Dirección y fuerza de empuje (ej. X = -14 empuja hacia la izquierda).")]
        [SerializeField] Vector2 windDirection = new Vector2(-1f, 0f);
        [SerializeField] float windForce = 12f;

        [Header("Ciclo de Ráfagas")]
        [SerializeField] bool intermittent = true;
        [SerializeField] float gustDuration = 3.0f;
        [SerializeField] float calmDuration = 2.0f;

        [Header("Visuales")]
        [SerializeField] ParticleSystem windParticles;
        [SerializeField] Color sandColor = new Color(0.95f, 0.78f, 0.45f, 0.55f);

        BoxCollider2D zoneCollider;
        float timer;
        bool isBlowing = true;

        public bool IsBlowing => isBlowing;

        void Awake()
        {
            zoneCollider = GetComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;

            if (windParticles == null)
            {
                CreateDefaultWindParticles();
            }
        }

        void Update()
        {
            if (!intermittent)
            {
                isBlowing = true;
                return;
            }

            timer += Time.deltaTime;
            float limit = isBlowing ? gustDuration : calmDuration;
            if (timer >= limit)
            {
                timer = 0f;
                isBlowing = !isBlowing;
                UpdateParticlesState();
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!isBlowing) return;

            var player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(windDirection.normalized * windForce, ForceMode2D.Force);
            }
        }

        void UpdateParticlesState()
        {
            if (windParticles == null) return;
            if (isBlowing && !windParticles.isPlaying) windParticles.Play();
            else if (!isBlowing && windParticles.isPlaying) windParticles.Stop();
        }

        void CreateDefaultWindParticles()
        {
            var go = new GameObject("WindSandVFX");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = sandColor;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 28f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(zoneCollider.size.x, zoneCollider.size.y, 0.1f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = windDirection.x * 6f;
            vel.y = windDirection.y * 2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                renderer.material = mat;
                renderer.sortingOrder = 15;
            }

            windParticles = ps;
            windParticles.Play();
        }
    }
}
