#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Armanya.Abilities;
using Armanya.CameraSystem;
using Armanya.Enemies;
using Armanya.Gameplay;
using Armanya.Interactables;
using Armanya.Player;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Herramienta principal de Armanya:
    ///   - Reconstruye los 4 niveles (héroes, monstruos, pistas, diálogos, vidas y Game Over).
    ///   - Migra los mapas de cada nivel a su Zona correspondiente de 2D Gamekit
    ///     CONSERVANDO intactos todos los personajes, monstruos, pistas, generadores y diálogos.
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

        // Mapa de 2DGamekit asignado a cada nivel (Level1 ya es Zone1)
        static readonly (string levelPath, string zonePath, string label)[] ZoneMigrations =
        {
            ("Assets/Scenes/Biomes/Level2_DistritoCaos.unity",  "Assets/2DGamekit/Scenes/Zone3.unity", "Nivel 2 (Anya - Distrito Caos -> Zone3 Templo)"),
            ("Assets/Scenes/Biomes/Level3_NexoOrbital.unity",   "Assets/2DGamekit/Scenes/Zone4.unity", "Nivel 3 (Aleek - Nexo Orbital -> Zone4 Complejo)"),
            ("Assets/Scenes/Biomes/Level4_RuinasFinales.unity", "Assets/2DGamekit/Scenes/Zone5.unity", "Nivel 4 (Tag-Team vs Ankea - Ruinas Finales -> Zone5 Arena Boss)")
        };

        const string TempMigrationScenePath = "Assets/Scenes/Biomes/_TempZoneMigration.unity";

        [MenuItem("Armanya/1. Reconstruir los 4 Niveles (Héroes, Monstruos, Vidas y GameOver)")]
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
            Debug.Log("[Armanya] ¡Listo! Los 4 niveles tienen héroe, monstruos, pistas, vidas, Game Over y diálogos.");
        }

        [MenuItem("Armanya/2. Reconstruir SOLO la Escena Abierta")]
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

        [MenuItem("Armanya/4. Cambiar Mapas a Zonas de 2DGamekit (Conservando Todo)")]
        public static void MigrateBiomesToGamekitZones()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 1. Verificar Nivel 1 (ya usa Zone1): asegurar cámara, AudioListener, vidas y GameOver
            if (File.Exists(LevelScenes[0]))
            {
                var l1 = EditorSceneManager.OpenScene(LevelScenes[0], OpenSceneMode.Single);
                ArmanyaCombatVFXBuilderTool.WireSceneHeroesAndMonsters();
                EnsureMainCameraSetup(l1, FindMainHeroInScene(l1));
                EditorSceneManager.MarkSceneDirty(l1);
                EditorSceneManager.SaveScene(l1);
                Debug.Log("[Armanya] Nivel 1 (Angus - Grieta Árida -> Zone1) verificado y actualizado.");
            }

            // 2. Migrar Niveles 2, 3 y 4 a Zone3, Zone4 y Zone5 conservando TODOS los objetos de Armanya
            foreach (var entry in ZoneMigrations)
            {
                if (!File.Exists(entry.levelPath))
                {
                    Debug.LogWarning($"[Armanya] No existe la escena origen '{entry.levelPath}'. Saltando.");
                    continue;
                }
                if (!File.Exists(entry.zonePath))
                {
                    Debug.LogError($"[Armanya] No existe la zona de 2DGamekit '{entry.zonePath}'.");
                    continue;
                }

                MigrateSingleLevel(entry.levelPath, entry.zonePath, entry.label);
            }

            // Limpiar temporal si quedó alguno
            if (File.Exists(TempMigrationScenePath))
            {
                AssetDatabase.DeleteAsset(TempMigrationScenePath);
            }

            // Reabrir Nivel 1 y actualizar Build Settings
            EditorSceneManager.OpenScene(LevelScenes[0], OpenSceneMode.Single);
            ArmanyaMenuBuilderTool.ConfigureBuildSettings();
            AssetDatabase.Refresh();
            Debug.Log("[Armanya] ¡Migración completada! Cada nivel ahora tiene su propia Zona de 2DGamekit con todos tus personajes, monstruos, pistas, generadores, vidas y Game Over intactos.");
        }

        static void MigrateSingleLevel(string levelPath, string zonePath, string label)
        {
            // Abrir la escena actual de Armanya (con nuestros personajes, monstruos, pistas, generadores, etc.)
            Scene oldScene = EditorSceneManager.OpenScene(levelPath, OpenSceneMode.Single);

            // Copiar la Zona de 2DGamekit a un archivo temporal y abrirla aditivamente
            if (File.Exists(TempMigrationScenePath))
                AssetDatabase.DeleteAsset(TempMigrationScenePath);

            if (!AssetDatabase.CopyAsset(zonePath, TempMigrationScenePath))
            {
                Debug.LogError($"[Armanya] No se pudo copiar '{zonePath}' a '{TempMigrationScenePath}'.");
                return;
            }
            AssetDatabase.Refresh();

            Scene newScene = EditorSceneManager.OpenScene(TempMigrationScenePath, OpenSceneMode.Additive);

            // 1. Encontrar punto de inicio (Ellen) en la nueva Zona y desactivar elementos de 2DGamekit que interfieren
            Vector3 newSpawnPos = Vector3.zero;
            bool foundSpawn = false;

            foreach (var root in newScene.GetRootGameObjects())
            {
                // Desactivar Ellen y tomar su posición de inicio
                var ellenChar = root.GetComponentInChildren<Gamekit2D.PlayerCharacter>(true);
                if (ellenChar != null)
                {
                    newSpawnPos = ellenChar.transform.position + new Vector3(0f, 0.8f, 0f);
                    foundSpawn = true;
                    ellenChar.gameObject.SetActive(false);
                }
                else if (root.name == "Ellen")
                {
                    newSpawnPos = root.transform.position + new Vector3(0f, 0.8f, 0f);
                    foundSpawn = true;
                    root.SetActive(false);
                }

                // Desactivar diálogos tutoriales de Ellen
                foreach (var dlg in root.GetComponentsInChildren<Gamekit2D.DialogueCanvasController>(true))
                    dlg.gameObject.SetActive(false);

                // Desactivar jefe Gunner de 2DGamekit en Zone5 para que Ankea sea el jefe único
                foreach (var boss in root.GetComponentsInChildren<MissileGolem>(true))
                    boss.gameObject.SetActive(false);

                // Desactivar barra de vida del Gunner si existe
                if (root.name.ToLowerInvariant().Contains("bosshealth"))
                    root.SetActive(false);
            }

            // 2. Identificar TODOS los objetos raíz de Armanya en oldScene
            var armanyaRoots = new List<GameObject>();
            foreach (var root in oldScene.GetRootGameObjects())
            {
                if (IsArmanyaRootObject(root))
                    armanyaRoots.Add(root);
            }

            // Posición actual del héroe principal antes de mover (para calcular el desplazamiento)
            Transform oldHero = FindMainHeroInList(armanyaRoots);
            Vector3 oldHeroPos = oldHero != null ? oldHero.position : Vector3.zero;
            Vector3 delta = foundSpawn ? (newSpawnPos - oldHeroPos) : Vector3.zero;
            delta.z = 0f;

            // 3. Mover todos los objetos de Armanya a newScene conservando TODAS sus referencias internas
            foreach (var go in armanyaRoots)
            {
                SceneManager.MoveGameObjectToScene(go, newScene);

                if (foundSpawn && ShouldTranslatePosition(go))
                {
                    go.transform.position += delta;
                }
            }

            // 4. Cerrar oldScene ANTES de reconectar referencias para que FindObject solo vea newScene
            SceneManager.SetActiveScene(newScene);
            EditorSceneManager.CloseScene(oldScene, true);

            Transform mainHero = FindMainHeroInScene(newScene);
            CameraFollow2D camFollow = EnsureMainCameraSetup(newScene, mainHero);

            // Si existe TagTeamManager (Nivel 4), conectarlo a la nueva CameraFollow2D
            foreach (var root in newScene.GetRootGameObjects())
            {
                var ttm = root.GetComponentInChildren<TagTeamManager>(true);
                if (ttm != null && camFollow != null)
                {
                    var so = new SerializedObject(ttm);
                    var prop = so.FindProperty("cameraFollow");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = camFollow;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            // 5. Guardar primero con el nombre real del nivel para que scene.name coincida (Level2/3/4)
            EditorSceneManager.SaveScene(newScene, levelPath);

            // Reconectar HealthSystem, Damageable, ArmanyaPlayerHealthBridge, habilidades J/K y HealthUI
            ArmanyaCombatVFXBuilderTool.WireSceneHeroesAndMonsters();
            EditorSceneManager.MarkSceneDirty(newScene);
            EditorSceneManager.SaveScene(newScene, levelPath);

            // Borrar el archivo temporal
            if (File.Exists(TempMigrationScenePath))
                AssetDatabase.DeleteAsset(TempMigrationScenePath);

            Debug.Log($"[Armanya] Migrado con éxito: {label} (movidos {armanyaRoots.Count} objetos de Armanya al spawn {newSpawnPos}).");
        }

        static bool IsArmanyaRootObject(GameObject go)
        {
            if (go == null) return false;

            if (go.GetComponent<PlayerController2D>() != null ||
                go.GetComponent<AbilityUser>() != null ||
                go.GetComponent<GameSession>() != null ||
                go.GetComponent<GameManager>() != null ||
                go.GetComponent<TagTeamManager>() != null ||
                go.GetComponent<FractureGeneratorManager>() != null ||
                go.GetComponent<StoryDialogueTrigger>() != null ||
                go.GetComponent<SubzonePortal>() != null ||
                go.GetComponent<DimensionCluePortal>() != null ||
                go.GetComponent<LoreClue>() != null ||
                go.GetComponent<StabilityOrb>() != null ||
                go.GetComponent<RestorableObject>() != null ||
                go.GetComponent<FractureGenerator>() != null ||
                go.GetComponent<EnemyAI2D>() != null ||
                go.GetComponent<AnkeaBossAI>() != null)
            {
                return true;
            }

            string n = go.name;
            if (n.StartsWith("Hero_") || n.StartsWith("Player_") || n.StartsWith("Generador_") ||
                n.StartsWith("LoreClue") || n.StartsWith("Pilar_") || n.StartsWith("[Armanya"))
            {
                return true;
            }

            return false;
        }

        static bool ShouldTranslatePosition(GameObject go)
        {
            // Managers puros no necesitan moverse en el espacio
            if (go.GetComponent<GameSession>() != null ||
                go.GetComponent<GameManager>() != null ||
                go.GetComponent<TagTeamManager>() != null ||
                go.GetComponent<FractureGeneratorManager>() != null ||
                go.GetComponent<StoryDialogueTrigger>() != null)
            {
                return false;
            }
            return true;
        }

        static Transform FindMainHeroInList(List<GameObject> roots)
        {
            foreach (var go in roots)
            {
                if (go != null && go.activeSelf && go.GetComponent<PlayerController2D>() != null)
                    return go.transform;
            }
            foreach (var go in roots)
            {
                if (go != null && go.GetComponent<PlayerController2D>() != null)
                    return go.transform;
            }
            return null;
        }

        static Transform FindMainHeroInScene(Scene scene)
        {
            var list = new List<GameObject>(scene.GetRootGameObjects());
            return FindMainHeroInList(list);
        }

        static CameraFollow2D EnsureMainCameraSetup(Scene scene, Transform heroTarget)
        {
            Camera mainCam = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                // Desactivar CinemachineBrain y AutoCameraSetup de 2DGamekit para que CameraFollow2D mande
                foreach (var brain in root.GetComponentsInChildren<CinemachineBrain>(true))
                    brain.enabled = false;

                foreach (var acs in root.GetComponentsInChildren<Gamekit2D.AutoCameraSetup>(true))
                    acs.autoSetupCameraFollow = false;

                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                {
                    if (cam.CompareTag("MainCamera") || cam.name.Contains("Main Camera"))
                    {
                        mainCam = cam;
                    }
                }
            }

            if (mainCam == null) return null;

            // Asegurar AudioListener activo en la Main Camera
            var listener = mainCam.GetComponent<AudioListener>();
            if (listener == null) listener = mainCam.gameObject.AddComponent<AudioListener>();
            listener.enabled = true;

            // Asegurar CameraFollow2D apuntando al héroe
            var follow = mainCam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = mainCam.gameObject.AddComponent<CameraFollow2D>();

            if (heroTarget != null)
            {
                var so = new SerializedObject(follow);
                var targetProp = so.FindProperty("target");
                if (targetProp != null) targetProp.objectReferenceValue = heroTarget;
                var offsetProp = so.FindProperty("offset");
                if (offsetProp != null) offsetProp.vector2Value = new Vector2(0f, 1.5f);
                var smoothProp = so.FindProperty("smoothTime");
                if (smoothProp != null) smoothProp.floatValue = 0.15f;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Posicionar la cámara inicialmente sobre el héroe
                mainCam.transform.position = new Vector3(heroTarget.position.x, heroTarget.position.y + 1.5f, mainCam.transform.position.z);
            }

            return follow;
        }
    }
}
#endif
