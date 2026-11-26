using System.Collections;
using UnityEngine;
using Armanya.Gameplay;
using Armanya.Player;

namespace Armanya.Enemies
{
    /// <summary>
    /// Columna de Fuego invocada por Ankea en el Nivel 4 (30 daño).
    /// Muestra un breve aviso (telegraph) antes de estallar y dañar al jugador.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FireColumnHazard : MonoBehaviour
    {
        [SerializeField] int damage = 30;
        [SerializeField] float warningDuration = 0.7f;
        [SerializeField] float activeDuration = 0.75f;

        SpriteRenderer sr;
        Collider2D col;
        bool activeHazard;
        bool alreadyHitPlayer;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            col.enabled = false;
        }

        void Start() => StartCoroutine(RunColumn());

        IEnumerator RunColumn()
        {
            // Fase 1: Aviso en el suelo (naranja translúcido)
            if (sr != null) sr.color = new Color(1f, 0.45f, 0.1f, 0.35f);
            yield return new WaitForSeconds(warningDuration);

            // Fase 2: Columna activa (fuego brillante + daño)
            activeHazard = true;
            col.enabled = true;
            if (sr != null) sr.color = new Color(1f, 0.25f, 0.05f, 0.92f);

            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();

            yield return new WaitForSeconds(activeDuration);

            Destroy(gameObject);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!activeHazard || alreadyHitPlayer) return;

            var playerCtrl = other.GetComponent<PlayerController2D>();
            if (playerCtrl == null) playerCtrl = other.GetComponentInParent<PlayerController2D>();
            if (playerCtrl == null) return;

            var hs = playerCtrl.GetComponent<HealthSystem>();
            if (hs != null && hs.IsInvulnerable) return;

            alreadyHitPlayer = true;
            if (hs != null) hs.TakeDamage(damage);

            var ph = playerCtrl.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage, gameObject);

            Debug.Log($"[Ankea] ¡Columna de Fuego golpeó al jugador (-{damage} HP)!", this);
        }
    }
}
