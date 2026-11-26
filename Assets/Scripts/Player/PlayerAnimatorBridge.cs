using System.Collections.Generic;
using UnityEngine;
using Armanya.Abilities;

namespace Armanya.Player
{
    /// <summary>
    /// Puente modular entre PlayerController2D + AbilityUser y un Animator 2D (huesos / .psb).
    /// Se añade al mismo GameObject que el jugador SIN modificar Collider2D, GroundCheck ni scripts base.
    ///
    /// Parámetros recomendados en el Animator Controller:
    /// - Float   "Speed"       (velocidad horizontal absoluta)
    /// - Float   "VelocityY"   (velocidad vertical: > 0 subiendo, < 0 cayendo)
    /// - Bool    "IsGrounded"  (true si toca suelo)
    /// - Trigger "Attack"      (se dispara al usar J / Clic Izq)
    /// - Trigger "SecretPower" (se dispara al usar K / Clic Der)
    /// </summary>
    [RequireComponent(typeof(PlayerController2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerAnimatorBridge : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Animator del rig 2D (.psb). Si se deja vacío, busca uno en este objeto o en sus hijos.")]
        [SerializeField] Animator animator;

        [Tooltip("Opcional: hijo visual del rig (.psb) si decides desactivar el giro en el padre.")]
        [SerializeField] Transform visualRoot;

        [Tooltip("Déjalo en FALSE si PlayerController2D ya voltea el objeto raíz (evita doble giro).")]
        [SerializeField] bool flipVisualRoot = false;

        [Header("Nombres de parámetros del Animator")]
        [SerializeField] string speedParam = "Speed";
        [SerializeField] string velocityYParam = "VelocityY";
        [SerializeField] string isGroundedParam = "IsGrounded";
        [SerializeField] string attackTrigger = "Attack";
        [SerializeField] string secretPowerTrigger = "SecretPower";

        [Header("Ajustes")]
        [Tooltip("Velocidad mínima para considerar que el personaje está corriendo.")]
        [SerializeField] float runThreshold = 0.1f;

        PlayerController2D controller;
        AbilityUser abilityUser;
        Rigidbody2D rb;

        int speedHash;
        int velocityYHash;
        int isGroundedHash;
        int attackHash;
        int secretPowerHash;

        readonly HashSet<int> validParams = new HashSet<int>();
        float initialVisualScaleX = 1f;

        void Awake()
        {
            controller = GetComponent<PlayerController2D>();
            abilityUser = GetComponent<AbilityUser>();
            rb = GetComponent<Rigidbody2D>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (visualRoot != null)
                initialVisualScaleX = Mathf.Abs(visualRoot.localScale.x);

            CacheAnimatorParameters();
        }

        void OnEnable()
        {
            if (abilityUser != null)
            {
                abilityUser.OnBasicUsed += HandleBasicAttack;
                abilityUser.OnSecretUsed += HandleSecretPower;
            }
        }

        void OnDisable()
        {
            if (abilityUser != null)
            {
                abilityUser.OnBasicUsed -= HandleBasicAttack;
                abilityUser.OnSecretUsed -= HandleSecretPower;
            }
        }

        void Update()
        {
            if (animator == null) return;

            float absSpeed = Mathf.Abs(rb.linearVelocity.x);
            if (absSpeed < runThreshold) absSpeed = 0f;

            if (validParams.Contains(speedHash))
                animator.SetFloat(speedHash, absSpeed);

            if (validParams.Contains(velocityYHash))
                animator.SetFloat(velocityYHash, rb.linearVelocity.y);

            if (validParams.Contains(isGroundedHash))
                animator.SetBool(isGroundedHash, controller.IsGrounded);

            if (flipVisualRoot && visualRoot != null)
            {
                Vector3 s = visualRoot.localScale;
                s.x = initialVisualScaleX * controller.FacingDirection;
                visualRoot.localScale = s;
            }
        }

        void HandleBasicAttack(AbilityData _)
        {
            if (animator != null && validParams.Contains(attackHash))
                animator.SetTrigger(attackHash);
        }

        void HandleSecretPower(AbilityData _)
        {
            if (animator != null && validParams.Contains(secretPowerHash))
                animator.SetTrigger(secretPowerHash);
        }

        void CacheAnimatorParameters()
        {
            speedHash = Animator.StringToHash(speedParam);
            velocityYHash = Animator.StringToHash(velocityYParam);
            isGroundedHash = Animator.StringToHash(isGroundedParam);
            attackHash = Animator.StringToHash(attackTrigger);
            secretPowerHash = Animator.StringToHash(secretPowerTrigger);

            validParams.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (var p in animator.parameters)
                validParams.Add(p.nameHash);
        }

        /// <summary>Útil si asignas o cambias el Animator en tiempo de ejecución.</summary>
        public void SetAnimator(Animator newAnimator)
        {
            animator = newAnimator;
            CacheAnimatorParameters();
        }
    }
}
