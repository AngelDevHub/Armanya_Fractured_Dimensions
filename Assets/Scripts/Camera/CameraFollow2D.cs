using UnityEngine;

namespace Armanya.CameraSystem
{
    /// <summary>
    /// Cámara 2D que sigue suavemente a un objetivo, con límites opcionales del nivel.
    /// Colócalo en la Main Camera (ortográfica).
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
        [Tooltip("Menor = más suave/lento. Mayor = más pegada.")]
        [SerializeField, Min(0.01f)] float smoothTime = 0.15f;

        [Header("Límites del nivel (opcional)")]
        [SerializeField] bool useBounds;
        [SerializeField] Vector2 minPosition = new Vector2(-50f, -20f);
        [SerializeField] Vector2 maxPosition = new Vector2(50f, 20f);

        Vector3 velocity;
        float fixedZ;

        void Awake() => fixedZ = transform.position.z;

        void Start()
        {
            // Empieza ya sobre el jugador para evitar un "barrido" inicial.
            if (target != null) transform.position = Desired();
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
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
