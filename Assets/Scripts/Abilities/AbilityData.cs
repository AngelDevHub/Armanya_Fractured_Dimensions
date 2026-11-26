using UnityEngine;

namespace Armanya.Abilities
{
    /// <summary>Forma en que se aplica una habilidad.</summary>
    public enum AbilityShape
    {
        Melee,      // Caja / arco frente al jugador (ej. Angus J)
        Projectile, // Dispara un proyectil (ej. Aleek J, Anya J)
        Area        // Cúpula / rayo / onda en área (ej. poderes K)
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
    /// Datos de UNA habilidad (ataque básico J o poder secreto K).
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

        [Header("Daño / alcance / control")]
        [Min(0f)] public float damage = 20f;
        [Tooltip("Melee: largo de la caja. Area: radio.")]
        [Min(0f)] public float range = 1.8f;
        [Tooltip("Melee: alto de la caja.")]
        [Min(0f)] public float height = 1.2f;
        [Tooltip("Fuerza de empuje horizontal al golpear (ej. Angus J).")]
        [Min(0f)] public float knockbackForce = 0f;
        [Tooltip("Segundos de aturdimiento al enemigo (ej. Aleek/Angus K = 2.5s, Anya K = 3.0s).")]
        [Min(0f)] public float stunDuration = 0f;

        [Header("Dash Intangible (ej. Anya K = 0.5s)")]
        [Min(0f)] public float dashDuration = 0f;
        [Min(0f)] public float dashSpeed = 14f;

        [Header("Proyectil (solo si shape = Projectile)")]
        public Projectile2D projectilePrefab;
        [Min(0f)] public float projectileSpeed = 14f;
        [Min(0f)] public float projectileLifetime = 2f;

        [Header("Efecto Visual (VFX)")]
        [Tooltip("Prefab visual (onda, cúpula, rayo, arco eléctrico) instanciado al usar la habilidad.")]
        public GameObject vfxPrefab;
        public Color vfxColor = Color.cyan;
        [Min(0.05f)] public float vfxLifetime = 0.45f;

        [Header("Restauración / poder secreto")]
        public RestorationEffect restoration = RestorationEffect.None;
        [Min(0f)] public float effectDuration = 3f;
        [Tooltip("Cuánto restaura (vida, energía, etc.) según el efecto.")]
        public float restorationAmount = 0f;
    }
}
