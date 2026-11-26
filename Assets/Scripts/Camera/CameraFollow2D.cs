using UnityEngine;
using Unity.Cinemachine;

namespace Armanya.CameraSystem
{
    /// <summary>
    /// Cámara 2D inteligente con seguimiento suave, confiner por límites 2D
    /// y efecto de Camera Shake ante impactos (compatible con Cinemachine Impulse).
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        public static CameraFollow2D Instance { get; private set; }

        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
        [Tooltip("Menor = más suave/lento. Mayor = más pegada.")]
        [SerializeField, Min(0.01f)] float smoothTime = 0.15f;

        [Header("Confiner 2D / Límites del nivel (opcional)")]
        [SerializeField] bool useBounds;
        [SerializeField] Collider2D confinerCollider;
        [SerializeField] Vector2 minPosition = new Vector2(-50f, -20f);
        [SerializeField] Vector2 maxPosition = new Vector2(50f, 20f);

        [Header("Game Feel: Camera Shake")]
        [SerializeField] float shakeMultiplier = 1f;

        Vector3 velocity;
        float fixedZ;
        float shakeTimer;
        float shakeDuration;
        float shakeIntensity;
        Vector3 lastShakeOffset;
        CinemachineImpulseSource impulseSource;

        void Awake()
        {
            Instance = this;
            fixedZ = transform.position.z;
            impulseSource = GetComponent<CinemachineImpulseSource>();

            if (confinerCollider != null)
            {
                useBounds = true;
                Bounds b = confinerCollider.bounds;
                minPosition = b.min;
                maxPosition = b.max;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // Empieza ya sobre el jugador para evitar un "barrido" inicial.
            if (target != null) transform.position = Desired();
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Quitar el desplazamiento de shake del frame anterior antes de suavizar
            Vector3 basePos = transform.position - lastShakeOffset;
            Vector3 smoothed = Vector3.SmoothDamp(basePos, Desired(), ref velocity, smoothTime);

            // Aplicar Camera Shake decreciente si hay impacto activo
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float t = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
                float currentMag = shakeIntensity * t * shakeMultiplier;
                lastShakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * currentMag,
                    Random.Range(-1f, 1f) * currentMag,
                    0f);
            }
            else
            {
                lastShakeOffset = Vector3.zero;
            }

            transform.position = smoothed + lastShakeOffset;
        }

        Vector3 Desired()
        {
            float x = target.position.x + offset.x;
            float y = target.position.y + offset.y;
            if (useBounds)
            {
                x = Mathf.Clamp(x, minPosition.x, maxPosition.x);
                y = Mathf.Clamp(y, minPosition.y, maxPosition.y);
            }
            return new Vector3(x, y, fixedZ);
        }

        /// <summary>Dispara un efecto de sacudida de cámara ante impactos o habilidades.</summary>
        public static void Shake(float intensity = 0.25f, float duration = 0.2f)
        {
            if (Instance == null) return;
            Instance.TriggerShake(intensity, duration);
        }

        public void TriggerShake(float intensity, float duration)
        {
            if (intensity <= 0f || duration <= 0f) return;
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTimer = Mathf.Max(shakeTimer, duration);

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(intensity);
            }
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        void OnDrawGizmosSelected()
        {
            if (!useBounds) return;
            Gizmos.color = Color.cyan;
            Vector3 c = (minPosition + maxPosition) * 0.5f;
            Vector3 s = maxPosition - minPosition;
            Gizmos.DrawWireCube(c, s);
        }
    }
}
