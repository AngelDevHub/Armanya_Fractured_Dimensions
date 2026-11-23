using UnityEngine;

namespace Armanya.Player
{
    /// <summary>
    /// Datos de movimiento de un personaje jugable.
    /// Crea un asset por personaje (Angus, Anya, Aleek) desde:
    /// Assets > Create > Armanya > Character Profile
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "Armanya/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        [Header("Identidad")]
        public string characterName = "Angus";

        [Header("Movimiento")]
        [Min(0f)] public float moveSpeed = 7f;
        [Min(0f)] public float jumpForce = 12f;

        [Header("Suavizado horizontal (unidades/seg^2)")]
        [Min(0f)] public float acceleration = 60f;
        [Min(0f)] public float deceleration = 80f;

        [Header("Salto")]
        [Tooltip("Tiempo extra tras dejar una plataforma en el que aún se puede saltar.")]
        [Min(0f)] public float coyoteTime = 0.1f;
        [Tooltip("Tiempo que se recuerda un salto pulsado justo antes de aterrizar.")]
        [Min(0f)] public float jumpBufferTime = 0.1f;
        [Tooltip("Multiplicador de gravedad al soltar el salto antes (salto variable).")]
        [Min(1f)] public float lowJumpMultiplier = 2f;
        [Tooltip("Multiplicador de gravedad al caer.")]
        [Min(1f)] public float fallMultiplier = 1.8f;
    }
}
