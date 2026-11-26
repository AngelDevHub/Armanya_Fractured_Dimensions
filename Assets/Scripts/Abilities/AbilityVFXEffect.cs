using UnityEngine;

namespace Armanya.Abilities
{
    /// <summary>
    /// Animación procedural ligera para prefabs VFX (ondas, cúpulas, arcos eléctricos, rayos):
    /// expande la escala y desvanece la transparencia antes de autodestruirse.
    /// </summary>
    public class AbilityVFXEffect : MonoBehaviour
    {
        [SerializeField] float lifetime = 0.45f;
        [SerializeField] Vector3 startScale = new Vector3(0.4f, 0.4f, 1f);
        [SerializeField] Vector3 endScale = new Vector3(2.2f, 2.2f, 1f);
        [SerializeField] bool fadeOut = true;

        SpriteRenderer[] renderers;
        Color[] baseColors;
        float elapsed;

        public void Initialize(Color tint, float duration, float targetRadius = 0f, int facing = 1)
        {
            if (duration > 0f) lifetime = duration;

            if (targetRadius > 0f)
            {
                float diameter = targetRadius * 2f;
                endScale = new Vector3(diameter, diameter, 1f);
                startScale = endScale * 0.25f;
            }

            if (facing < 0)
            {
                startScale.x = -Mathf.Abs(startScale.x);
                endScale.x = -Mathf.Abs(endScale.x);
            }

            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].color = tint;
                baseColors[i] = tint;
            }

            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = tint;
                ps.Play();
            }

            transform.localScale = startScale;
        }

        void Awake()
        {
            if (renderers == null)
            {
                renderers = GetComponentsInChildren<SpriteRenderer>();
                baseColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                    baseColors[i] = renderers[i].color;
            }
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));

            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            if (fadeOut && renderers != null)
            {
                float alpha = 1f - t;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    Color c = baseColors[i];
                    c.a *= alpha;
                    renderers[i].color = c;
                }
            }

            if (elapsed >= lifetime)
                Destroy(gameObject);
        }
    }
}
