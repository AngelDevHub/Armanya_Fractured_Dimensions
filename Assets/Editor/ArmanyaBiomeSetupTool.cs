#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Armanya.Enemies;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Reconstruye los 4 niveles reutilizando EXACTAMENTE los pasos que ya funcionaban
    /// (no crea héroes ni componentes desde cero):
    ///   10 Poblar enemigos del bioma  -> 2 Sprites de personajes -> 8 Conectar J/K + HealthSystem
    ///   -> 11 Iconos pistas/orbes     -> 12 Pilar/Generadores (+ quita Dummy) -> 13 Diálogos de historia
    /// Cada escena conserva su héroe (Angus N1, Anya N2, Aleek N3, Tag-Team N4).
    /// </summary>
    public static class ArmanyaBiomeSetupTool
    {
        static readonly string[] LevelScenes =
        {
            "Assets/Scenes/Biomes/Level1_GrietaArida.unity",
            "Assets/Scenes/Biomes/Level2_DistritoCaos.unity",
            "Assets/Scenes/Biomes/Level3_NexoOrbital.unity",
            "Assets/Scenes/Biomes/Level4_RuinasFinales.unity"
        };

        [MenuItem("Armanya/15. Reconstruir los 4 Niveles (Héroe + Monstruos + Pistas + Diálogos)")]
        public static void RebuildAllLevels()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (string path in LevelScenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                RebuildActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Armanya] Nivel reconstruido y guardado: {scene.name}");
            }

            EditorSceneManager.OpenScene(LevelScenes[0], OpenSceneMode.Single);
            ArmanyaMenuBuilderTool.ConfigureBuildSettings();
            Debug.Log("[Armanya] ¡Listo! Los 4 niveles tienen héroe, monstruos, pistas y diálogos.");
        }

        [MenuItem("Armanya/16. Reconstruir SOLO la Escena Abierta")]
        public static void RebuildActiveScene()
        {
            // Evitar duplicados si se ejecuta dos veces
            var old = new List<GameObject>();
            foreach (var e in Object.FindObjectsByType<EnemyAI2D>(FindObjectsInactive.Include)) old.Add(e.gameObject);
            foreach (var b in Object.FindObjectsByType<AnkeaBossAI>(FindObjectsInactive.Include)) old.Add(b.gameObject);
            foreach (var go in old) if (go != null) Object.DestroyImmediate(go);

            ArmanyaEnemyBuilderTool.PopulateEnemiesForCurrentBiome();      // 10
            ArmanyaSpriteSetupTool.AssignSpritesInActiveScene();           // 2
            ArmanyaCombatVFXBuilderTool.WireSceneHeroesAndMonsters();      // 8
            ArmanyaSpriteSetupTool.Apply2DGamekitPickupSprites();          // 11
            ArmanyaSpriteSetupTool.ApplyPillarAndGeneratorSprites();       // 12
            ArmanyaSpriteSetupTool.SetupStoryDialoguesInScene();           // 13
        }
    }
}
#endif
