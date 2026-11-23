using UnityEngine;
using Armanya.Player;

namespace Armanya.Interactables
{
    /// <summary>
    /// Orbe de Estabilidad Dimensional. Al entrar en contacto con Trigger 2D,
    /// otorga +15 HP al jugador (respetando la vida máxima) y se consume.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StabilityOrb : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] float healAmount = 15f;
        [Tooltip("Si es true, solo se recoge si el jugador tiene menos de la vida máxima.")]
        [SerializeField] bool onlyCollectIfDamaged = true;

        [Header("Efecto de flotación")]
        [SerializeField] float floatSpeed = 3f;
        [SerializeField] float floatHeight = 0.12f;

        Vector3 startPos;
        bool consumed;

        void Awake()
        {
            startPos = transform.position;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void Update()
        {
            transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed) return;

            // Buscar PlayerHealth en el objeto que colisionó o en su raíz
            var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            if (onlyCollectIfDamaged && health.CurrentHealth >= health.MaxHealth)
            {
                // Ya tiene la vida llena, no gastar el orbe
                return;
            }

            consumed = true;
            bool healed = health.Heal(healAmount);

            if (healed)
            {
                Debug.Log($"[Orbe] Orbe de Estabilidad Dimensional consumido (+{healAmount} HP).", this);
                Destroy(gameObject);
            }
            else
            {
                consumed = false;
            }
        }
    }
}
