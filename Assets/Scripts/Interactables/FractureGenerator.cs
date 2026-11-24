using System;
using UnityEngine;
using Armanya.Abilities;

namespace Armanya.Interactables
{
    /// <summary>
    /// Generador de Fractura: empieza averiado (isRepaired = false) y pasa a reparado
    /// cuando recibe el poder secreto del héroe correcto.
    /// Necesita un Collider2D dentro del alcance del poder secreto.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FractureGenerator : MonoBehaviour, IRestorable
    {
        [Header("Configuración")]
        [SerializeField] string generatorName = "Generador";
        [Tooltip("Poder secreto que lo repara (Angus: LightningWeather, Anya: SoundIntangibility, Aleek: Telekinesis).")]
        [SerializeField] RestorationEffect requiredEffect = RestorationEffect.LightningWeather;

        [Header("Estado")]
        [SerializeField] bool isRepaired = false;

        [Header("Visual (opcional)")]
        [SerializeField] SpriteRenderer targetRenderer;
        [SerializeField] Color brokenColor = new Color(0.5f, 0.1f, 0.1f, 1f);
        [SerializeField] Color repairedColor = new Color(0.2f, 1f, 0.4f, 1f);

        public bool IsRepaired => isRepaired;
        public string GeneratorName => generatorName;
        public RestorationEffect RequiredEffect => requiredEffect;

        /// <summary>Se dispara una sola vez al repararse.</summary>
        public event Action<FractureGenerator> OnRepaired;

        void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            ApplyVisual();
        }

        public void Restore(RestorationEffect effect, float amount, float duration, GameObject source)
        {
            if (isRepaired) return;

            if (effect != requiredEffect)
            {
                Debug.Log($"[{generatorName}] Poder incorrecto: necesita {requiredEffect}, recibió {effect}.", this);
                return;
            }

            isRepaired = true;
            ApplyVisual();
            Debug.Log($"[{generatorName}] ¡REPARADO! (isRepaired = true) con {effect}.", this);
            OnRepaired?.Invoke(this);
        }

        void ApplyVisual()
        {
            if (targetRenderer != null)
                targetRenderer.color = isRepaired ? repairedColor : brokenColor;
        }

        void OnValidate()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            ApplyVisual();
        }
    }
}
