using System;
using UnityEngine;
using Armanya.Interactables;

namespace Armanya.Gameplay
{
    /// <summary>
    /// Cuenta los Generadores de Fractura reparados (0/3 ... 3/3) y avisa al completar el nivel final.
    /// </summary>
    public class FractureGeneratorManager : MonoBehaviour
    {
        [SerializeField] FractureGenerator[] generators;
        [SerializeField] bool showDebugHud = true;

        public int RepairedCount { get; private set; }
        public int TotalCount => generators != null ? generators.Length : 0;
        public bool AllRepaired => TotalCount > 0 && RepairedCount >= TotalCount;

        public event Action<int, int> OnProgress; // (reparados, total)
        public event Action OnAllRepaired;

        void Awake()
        {
            // Si no se asignaron a mano, los busca en la escena
            if (generators == null || generators.Length == 0)
                generators = FindObjectsByType<FractureGenerator>(FindObjectsSortMode.None);
        }

        void OnEnable()
        {
            if (generators == null) return;
            foreach (var g in generators) if (g != null) g.OnRepaired += HandleRepaired;
        }

        void OnDisable()
        {
            if (generators == null) return;
            foreach (var g in generators) if (g != null) g.OnRepaired -= HandleRepaired;
        }

        void Start() => Recount();

        void HandleRepaired(FractureGenerator g)
        {
            Recount();
            GameSession.AddPoints(300, "Generador de Fractura reparado");
            Debug.Log($"[Fractura] Generadores reparados: {RepairedCount}/{TotalCount}");
            OnProgress?.Invoke(RepairedCount, TotalCount);

            if (AllRepaired)
            {
                Debug.Log("[Fractura] ★ ¡Los 3 Generadores de Fractura están reparados! ★");
                OnAllRepaired?.Invoke();
            }
        }

        void Recount()
        {
            RepairedCount = 0;
            if (generators == null) return;
            foreach (var g in generators) if (g != null && g.IsRepaired) RepairedCount++;
        }

        void OnGUI()
        {
            if (!showDebugHud) return;
            string t = $"Generadores: {RepairedCount} / {TotalCount}" + (AllRepaired ? " (¡COMPLETO!)" : "");
            GUI.Label(new Rect(10, 160, 300, 24), t);
        }
    }
}
