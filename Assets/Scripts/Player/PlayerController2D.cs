using UnityEngine;
using UnityEngine.InputSystem;

namespace Armanya.Player
{
    /// <summary>
    /// Controlador 2D base: movimiento horizontal (A/D o flechas) y salto (Espacio).
    /// Usa Rigidbody2D + GroundCheck. Pensado para ser heredado por Angus, Anya y Aleek:
    /// los valores vienen de un CharacterProfile y los métodos clave son virtuales.
    /// (El proyecto usa el nuevo Input System, por eso se lee Keyboard.current.)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] protected CharacterProfile profile;

        [Header("Detección de suelo")]
        [Tooltip("Hijo vacío colocado en los pies del personaje.")]
        [SerializeField] protected Transform groundCheck;
        [SerializeField] protected float groundCheckRadius = 0.15f;
        [Tooltip("Capas que cuentan como suelo.")]
        [SerializeField] protected LayerMask groundLayers;

        protected Rigidbody2D rb;

        /// <summary>Dirección horizontal actual: -1, 0 o 1.</summary>
        public float MoveInput { get; protected set; }
        public bool IsGrounded { get; protected set; }
        /// <summary>1 = mirando a la derecha, -1 = izquierda.</summary>
        public int FacingDirection { get; protected set; } = 1;

        float coyoteCounter;
        float jumpBufferCounter;
        bool jumpHeld;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true; // evita que la cápsula ruede
        }

        protected virtual void Update()
        {
            if (profile == null) return;

            ReadInput();
            CheckGround();
            HandleTimers();
            HandleFacing();
        }

        protected virtual void FixedUpdate()
        {
            if (profile == null) return;

            ApplyHorizontalMovement();
            TryJump();
            ApplyBetterGravity();
        }

        // ---------- Entrada ----------
        protected virtual void ReadInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            MoveInput = h;

            if (kb.spaceKey.wasPressedThisFrame) jumpBufferCounter = profile.jumpBufferTime;
            jumpHeld = kb.spaceKey.isPressed;
        }

        // ---------- Suelo ----------
        protected virtual void CheckGround()
        {
            IsGrounded = groundCheck != null &&
                         Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayers);
        }

        protected virtual void HandleTimers()
        {
            coyoteCounter = IsGrounded ? profile.coyoteTime : coyoteCounter - Time.deltaTime;
            jumpBufferCounter -= Time.deltaTime;
        }

        protected virtual void HandleFacing()
        {
            if (MoveInput > 0.01f) FacingDirection = 1;
            else if (MoveInput < -0.01f) FacingDirection = -1;

            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * FacingDirection;
            transform.localScale = s;
        }

        // ---------- Física ----------
        protected virtual void ApplyHorizontalMovement()
        {
            float target = MoveInput * profile.moveSpeed;
            float rate = Mathf.Abs(target) > 0.01f ? profile.acceleration : profile.deceleration;
            float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        protected virtual void TryJump()
        {
            if (jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, profile.jumpForce);
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
                OnJump();
            }
        }

        /// <summary>Salto más corto si se suelta Espacio y caída más pesada.</summary>
        protected virtual void ApplyBetterGravity()
        {
            float g = Physics2D.gravity.y * rb.gravityScale * Time.fixedDeltaTime;
            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity += Vector2.up * g * (profile.fallMultiplier - 1f);
            else if (rb.linearVelocity.y > 0f && !jumpHeld)
                rb.linearVelocity += Vector2.up * g * (profile.lowJumpMultiplier - 1f);
        }

        /// <summary>Gancho para que Angus/Anya/Aleek añadan sonido, animación o habilidades.</summary>
        protected virtual void OnJump() { }

        void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
