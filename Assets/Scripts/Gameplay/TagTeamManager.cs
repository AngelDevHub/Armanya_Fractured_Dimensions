using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Armanya.Player;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Tag-Team: alterna en tiempo real entre 3 héroes con las teclas 1, 2 y 3.
    /// Cada héroe es un GameObject propio (PlayerController2D + AbilityUser + PlayerHealth).
    /// Solo uno está activo; al cambiar, el nuevo aparece donde estaba el anterior,
    /// conserva la velocidad y la cámara pasa a seguirlo.
    /// </summary>
    public class TagTeamManager : MonoBehaviour
    {
        [Serializable]
        public class HeroSlot
        {
            public string heroName = "Angus";
            public GameObject hero;
        }

        [Header("Héroes (orden = teclas 1, 2, 3)")]
        [SerializeField] HeroSlot[] heroes = new HeroSlot[3];

        [Header("Cámara")]
        [SerializeField] CameraSystem.CameraFollow2D cameraFollow;

        [Header("Opciones")]
        [SerializeField] int startingHeroIndex = 0;
        [Tooltip("Tiempo mínimo entre cambios (evita spam).")]
        [SerializeField, Min(0f)] float swapCooldown = 0.3f;
        [SerializeField] bool showDebugHud = true;

        public int CurrentIndex { get; private set; } = -1;
        public GameObject CurrentHero =>
            (CurrentIndex >= 0 && CurrentIndex < heroes.Length) ? heroes[CurrentIndex].hero : null;

        /// <summary>(índice, nombre) del héroe activo.</summary>
        public event Action<int, string> OnHeroChanged;

        float nextSwapTime;

        void Start()
        {
            if (!HasAnyHero())
            {
                Debug.LogError("[TagTeam] No hay héroes asignados en el TagTeamManager.", this);
                enabled = false;
                return;
            }

            // Apaga todos y activa solo el inicial
            for (int i = 0; i < heroes.Length; i++)
                if (IsValid(i)) heroes[i].hero.SetActive(false);

            int start = IsValid(startingHeroIndex) ? startingHeroIndex : FirstValidIndex();
            Activate(start, null);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) SwitchTo(0);
            else if (kb.digit2Key.wasPressedThisFrame) SwitchTo(1);
            else if (kb.digit3Key.wasPressedThisFrame) SwitchTo(2);
        }

        public void SwitchTo(int index)
        {
            if (index == CurrentIndex) return;
            if (Time.time < nextSwapTime) return;
            if (!IsValid(index))
            {
                Debug.LogWarning($"[TagTeam] El héroe {index + 1} no está asignado.", this);
                return;
            }

            nextSwapTime = Time.time + swapCooldown;
            Activate(index, CurrentHero);
        }

        void Activate(int index, GameObject previous)
        {
            GameObject next = heroes[index].hero;

            // Traspasa posición y velocidad del héroe anterior
            if (previous != null)
            {
                next.transform.position = previous.transform.position;

                var prevRb = previous.GetComponent<Rigidbody2D>();
                var nextRb = next.GetComponent<Rigidbody2D>();
                if (prevRb != null && nextRb != null) nextRb.linearVelocity = prevRb.linearVelocity;

                previous.SetActive(false);
            }

            next.SetActive(true);
            CurrentIndex = index;

            if (cameraFollow != null) cameraFollow.SetTarget(next.transform);
            else Debug.LogWarning("[TagTeam] Falta asignar CameraFollow2D; la cámara no seguirá al nuevo héroe.", this);

            if (GameSession.Instance != null)
                GameSession.Instance.SetPlayerHealth(next.GetComponent<PlayerHealth>());

            Debug.Log($"[TagTeam] Héroe activo: {heroes[index].heroName} (tecla {index + 1})", this);
            OnHeroChanged?.Invoke(index, heroes[index].heroName);
        }

        bool IsValid(int i) => i >= 0 && i < heroes.Length && heroes[i] != null && heroes[i].hero != null;

        bool HasAnyHero() => FirstValidIndex() >= 0;

        int FirstValidIndex()
        {
            for (int i = 0; i < heroes.Length; i++) if (IsValid(i)) return i;
            return -1;
        }

        void OnGUI()
        {
            if (!showDebugHud || CurrentIndex < 0) return;
            string label = $"Héroe: {heroes[CurrentIndex].heroName}   [1] [2] [3]";
            GUI.Label(new Rect(10, 140, 400, 24), label);
        }
    }
}
