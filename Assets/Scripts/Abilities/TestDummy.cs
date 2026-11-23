using UnityEngine;

namespace Armanya.Abilities
{
    /// <summary>
    /// Objetivo de prueba (sin IA): recibe daño y restauración y lo muestra en consola.
    /// Úsalo para verificar que J y K funcionan.
    /// </summary>
    public class TestDummy : MonoBehaviour, IDamageable, IRestorable
    {
        [SerializeField] float maxHealth = 50f;
        float health;

        void Awake() => health = maxHealth;

        public void TakeDamage(float amount, GameObject source)
        {
            health -= amount;
            Debug.Log($"[Dummy] Recibió {amount} de daño. Vida: {Mathf.Max(health, 0)}/{maxHealth}", this);
            if (health <= 0f)
            {
                Debug.Log("[Dummy] Destruido.", this);
                Destroy(gameObject);
            }
        }

        public void Restore(RestorationEffect effect, float amount, float duration, GameObject source)
        {
            Debug.Log($"[Dummy] Restauración {effect} (cantidad {amount}, duración {duration}s)", this);
        }
    }
}
