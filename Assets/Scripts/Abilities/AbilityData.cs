using UnityEngine;

namespace Armanya.Abilities
{
    /// <summary>Forma en que se aplica una habilidad.</summary>
    public enum AbilityShape
    {
        Melee,      // Caja frente al jugador
        Projectile, // Dispara un proyectil
        Area        // Círculo alrededor del jugador
    }

    /// <summary>Efecto de restauración / poder secreto (uno por personaje, ampliable).</summary>
    public enum RestorationEffect
    {
        None,
        LightningWeather,   // Angus: rayo / clima
        SoundIntangibility, // Anya: sonido / intangibilidad
        Telekinesis         // Aleek: telequinesis
    }

    /// <summary>
    /// Datos de UNA habilidad (ataque básico o poder secreto).
    /// Crear desde: Assets > Create > Armanya > Ability Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Armanya/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [Header("General")]
        public string abilityName = "Ataque";
        public AbilityShape shape = AbilityShape.Melee;
        [Min(0f)] public float cooldown = 0.5f;
        [Tooltip("Capas que pueden recibir la habilidad (enemigos, objetos restaurables...).")]
        public LayerMask targetLayers = ~0;

        [Header("Daño / alcance")]
        [Min(0f)] public float damage = 10f;
        [Tooltip("Melee: largo de la caja. Area: radio.")]
        [Min(0f)] public float range = 1.5f;
        [Tooltip("Melee: alto de la caja.")]
        [Min(0f)] public float height = 1f;

        [Header("Proyectil (solo si shape = Projectile)")]
        public Projectile2D projectilePrefab;
        [Min(0f)] public float projectileSpeed = 12f;
        [Min(0f)] public float projectileLifetime = 2f;

        [Header("Restauración / poder secreto")]
        public RestorationEffect restoration = RestorationEffect.None;
        [Min(0f)] public float effectDuration = 3f;
        [Tooltip("Cuánto restaura (vida, energía, etc.) según el efecto.")]
        public float restorationAmount = 0f;
    }
}
