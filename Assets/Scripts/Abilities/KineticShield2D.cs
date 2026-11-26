using UnityEngine;

namespace Armanya.Abilities
{
    /// <summary>
    /// Marcador para el escudo/cúpula cinética de Aleek.
    /// Cualquier EnemyProjectile2D que choque con este collider es bloqueado y destruido.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KineticShield2D : MonoBehaviour
    {
        void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }
    }
}
