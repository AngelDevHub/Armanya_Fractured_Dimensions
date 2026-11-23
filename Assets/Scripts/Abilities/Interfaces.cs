using UnityEngine;

namespace Armanya.Abilities
{
    /// <summary>Todo lo que pueda recibir daño (enemigos, objetos rompibles...).</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject source);
    }

    /// <summary>Todo lo que reaccione a un poder de restauración (dimensiones, puzzles...).</summary>
    public interface IRestorable
    {
        void Restore(RestorationEffect effect, float amount, float duration, GameObject source);
    }
}
