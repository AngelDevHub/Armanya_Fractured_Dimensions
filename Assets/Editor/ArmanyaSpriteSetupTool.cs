#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Armanya.Abilities;
using Armanya.Enemies;
using Armanya.Gameplay;
using Armanya.Interactables;
using Armanya.Player;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Herramienta de Editor para configurar los sprites 2D recortados de Armanya
    /// y asignarlos a personajes y monstruos SIN tocar Rigidbody2D, Collider2D ni scripts de gameplay.
    /// </summary>
    public static class ArmanyaSpriteSetupTool
    {
        const string PlayersFolder = "Assets/Art/Sprites/Players";
        const string MonstersFolder = "Assets/Art/Sprites/Monsters";
        const string MonsterPrefabsFolder = "Assets/Prefabs/Monsters";

        // Sorting Layer "Default" recibe la GlobalLight2D del 2D Game Kit; el Order alto los pone al frente del escenario
        const string CharacterSortingLayer = "Default";
        const int PlayerSortingOrder = 10;
        const int MonsterSortingOrder = 9;

        // Altura objetivo en unidades de Unity para empatar con el CapsuleCollider2D (alto = 2.0) sin alterar localScale
        static readonly Dictionary<string, float> TargetWorldHeights = new Dictionary<string, float>
        {
            { "Aleek_Side", 2.05f },
            { "Angus_Side", 2.15f },
            { "Anya_Side", 1.95f },
            { "Ankea_Side", 1.65f },
            { "OjoFantasma_Side", 2.10f },
            { "BestiaDeCaos_Side", 2.25f },
            { "MonstruoTresOjos_Side", 1.90f },
            { "CentinelaTentacular_Side", 1.75f }
        };

        // [MenuItem("Armanya/1. Configurar Sprites 2D (Import Settings)")]
        public static void ConfigureAllSprites()
        {
            int count = 0;
            count += ConfigureFolder(PlayersFolder);
            count += ConfigureFolder(MonstersFolder);
            AssetDatabase.Refresh();
            Debug.Log($"[Armanya] Se configuraron {count} sprites 2D (Pivote, PPU y compresión WebGL listos).");
        }

        static int ConfigureFolder(string folder)
        {
            if (!Directory.Exists(folder)) return 0;

            string[] files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            int count = 0;

            foreach (string rawPath in files)
            {
                string assetPath = rawPath.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                bool isSide = fileName.EndsWith("_Side");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = 512;

                // Pivote centrado (0.5, 0.5) para empatar con CapsuleCollider2D (offset 0,0) y GroundCheck en y = -1
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(settings);

                // Ajustar PPU según la altura real de la textura para que mida ~2 unidades en escena sin tocar localScale
                if (isSide && TargetWorldHeights.TryGetValue(fileName, out float targetHeight))
                {
                    importer.GetSourceTextureWidthAndHeight(out _, out int texHeight);
                    if (texHeight > 0)
                    {
                        importer.spritePixelsPerUnit = Mathf.Round(texHeight / targetHeight);
                    }
                }
                else
                {
                    importer.spritePixelsPerUnit = 100f; // Retratos UI
                }

                importer.SaveAndReimport();
                count++;
            }

            return count;
        }

        // [MenuItem("Armanya/2. Asignar Sprites a Personajes en Escena Actual")]
        public static void AssignSpritesInActiveScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            int updated = 0;
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

            foreach (var go in allObjects)
            {
                if (go == null) continue;
                string spritePath = ResolveSpritePathForObject(go, scene.name);
                if (string.IsNullOrEmpty(spritePath)) continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Armanya] No se encontró el Sprite en '{spritePath}'. Ejecuta primero el paso 1.");
                    continue;
                }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                bool isMonster = spritePath.Contains("/Monsters/");

                Undo.RecordObject(sr, "Asignar Sprite de Armanya");
                sr.sprite = sprite;
                sr.color = Color.white; // Quitar el tinte de la cápsula temporal
                sr.sortingLayerName = CharacterSortingLayer;
                sr.sortingOrder = isMonster ? MonsterSortingOrder : PlayerSortingOrder;
                EditorUtility.SetDirty(sr);

                updated++;
                Debug.Log($"[Armanya] Asignado '{sprite.name}' a '{go.name}' (Sorting: {sr.sortingLayerName} / {sr.sortingOrder}).", go);
            }

            if (updated > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Armanya] ¡Listo! {updated} objeto(s) actualizado(s) en '{scene.name}' sin tocar colisiones ni scripts. Guarda la escena (Ctrl+S).");
            }
            else
            {
                Debug.LogWarning("[Armanya] No se encontraron objetos Hero_Angus, Hero_Anya, Hero_Aleek, Player_Temp o monstruos con SpriteRenderer en esta escena.");
            }
        }

        static string ResolveSpritePathForObject(GameObject go, string sceneName)
        {
            // Nunca tocar generadores, objetos restaurables, pistas ni orbes
            if (go.GetComponent<FractureGenerator>() != null ||
                go.GetComponent<RestorableObject>() != null ||
                go.GetComponent<LoreClue>() != null ||
                go.GetComponent<StabilityOrb>() != null)
            {
                return null;
            }

            string n = go.name.ToLowerInvariant();
            if (n.Contains("generador") || n.Contains("generator") || n.Contains("manager"))
                return null;

            // Héroes por nombre directo
            if (n.Contains("angus")) return $"{PlayersFolder}/Angus_Side.png";
            if (n.Contains("anya")) return $"{PlayersFolder}/Anya_Side.png";
            if (n.Contains("aleek")) return $"{PlayersFolder}/Aleek_Side.png";
            if (n.Contains("ankea")) return $"{PlayersFolder}/Ankea_Side.png";

            // Player_Temp en niveles 1, 2, 3 o Armanya_Zone1: detectar por CharacterProfile o nombre de escena
            if (n.Contains("player_temp") || go.GetComponent<PlayerController2D>() != null)
            {
                var ctrl = go.GetComponent<PlayerController2D>();
                if (ctrl != null)
                {
                    var so = new SerializedObject(ctrl);
                    var profProp = so.FindProperty("profile");
                    if (profProp != null && profProp.objectReferenceValue is CharacterProfile prof)
                    {
                        string cn = (prof.characterName ?? "").ToLowerInvariant();
                        if (cn.Contains("anya")) return $"{PlayersFolder}/Anya_Side.png";
                        if (cn.Contains("aleek")) return $"{PlayersFolder}/Aleek_Side.png";
                        if (cn.Contains("angus")) return $"{PlayersFolder}/Angus_Side.png";
                    }
                }

                string sn = sceneName.ToLowerInvariant();
                if (sn.Contains("distritocaos") || sn.Contains("level2")) return $"{PlayersFolder}/Anya_Side.png";
                if (sn.Contains("nexoorbital") || sn.Contains("level3")) return $"{PlayersFolder}/Aleek_Side.png";
                return $"{PlayersFolder}/Angus_Side.png";
            }

            // Monstruos por nombre
            if (n.Contains("ojofantasma") || n.Contains("ojo_fantasma") || n.Contains("ojo fantasma"))
                return $"{MonstersFolder}/OjoFantasma_Side.png";
            if (n.Contains("bestia"))
                return $"{MonstersFolder}/BestiaDeCaos_Side.png";
            if (n.Contains("tresojos") || n.Contains("tres_ojos") || n.Contains("tres ojos"))
                return $"{MonstersFolder}/MonstruoTresOjos_Side.png";
            if (n.Contains("centinela") || n.Contains("tentacular"))
                return $"{MonstersFolder}/CentinelaTentacular_Side.png";

            return null;
        }

        // [MenuItem("Armanya/3. Crear Prefabs Base de Monstruos")]
        public static void CreateMonsterPrefabs()
        {
            if (!Directory.Exists(MonsterPrefabsFolder))
                Directory.CreateDirectory(MonsterPrefabsFolder);

            string[] monsterNames =
            {
                "OjoFantasma",
                "BestiaDeCaos",
                "MonstruoTresOjos",
                "CentinelaTentacular"
            };

            int created = 0;
            foreach (string mName in monsterNames)
            {
                string spritePath = $"{MonstersFolder}/{mName}_Side.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Armanya] Falta sprite '{spritePath}'. Ejecuta primero '1. Configurar Sprites 2D'.");
                    continue;
                }

                string prefabPath = $"{MonsterPrefabsFolder}/{mName}.prefab";
                var temp = new GameObject(mName);

                var sr = temp.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = Color.white;
                sr.sortingLayerName = CharacterSortingLayer;
                sr.sortingOrder = MonsterSortingOrder;

                var col = temp.AddComponent<BoxCollider2D>();
                col.size = sprite.bounds.size * 0.8f;

                // TestDummy para poder probar daño (J) y restauración (K) sin IA todavía
                temp.AddComponent<TestDummy>();

                PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
                Object.DestroyImmediate(temp);
                created++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Armanya] Se crearon {created} prefabs de monstruos en '{MonsterPrefabsFolder}'.");
        }

        // [MenuItem("Armanya/4. Restaurar Cuadro (Square) en Generadores")]
        public static void RestoreGeneratorSquares()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            Sprite squareSprite = null;
            foreach (string guid in AssetDatabase.FindAssets("Square t:Sprite"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null && s.name == "Square")
                {
                    squareSprite = s;
                    break;
                }
            }

            var generators = Object.FindObjectsByType<FractureGenerator>(FindObjectsInactive.Include);
            int fixedCount = 0;
            foreach (var gen in generators)
            {
                var sr = gen.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                Undo.RecordObject(sr, "Restaurar Sprite de Generador");
                sr.sprite = squareSprite;
                sr.color = new Color(0.5f, 0.1f, 0.1f, 1f);
                sr.sortingOrder = 2;
                EditorUtility.SetDirty(sr);
                fixedCount++;
            }

            if (fixedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Armanya] Se restauró el sprite Square en {fixedCount} generador(es). Guarda la escena (Ctrl+S).");
            }
        }

        // [MenuItem("Armanya/11. Cambiar Iconos de Pistas de Lore y Orbes (2DGamekit)")]
        public static void Apply2DGamekitPickupSprites()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Cargar sub-sprite KeyIcon_Collected (reliquia/cristal brillante del 2D Game Kit)
            Sprite loreSprite = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/2DGamekit/Art/Sprites/UI/HUD/KeyIcon.png"))
            {
                if (obj is Sprite s && s.name == "KeyIcon_Collected")
                {
                    loreSprite = s;
                    break;
                }
            }

            Sprite orbSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/2DGamekit/Art/Sprites/VFX/Interactables/HealingGlobe.png");

            int updated = 0;

            // 1. Pistas de Lore (Bitácora Hídrica, Manifiesto Roto, Consola Central, LoreClue_2, LoreClue_3...)
            if (loreSprite != null)
            {
                var clues = Object.FindObjectsByType<LoreClue>(FindObjectsInactive.Include);
                foreach (var c in clues)
                {
                    var sr = c.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;
                    Undo.RecordObject(sr, "Cambiar Icono Pista de Lore");
                    sr.sprite = loreSprite;
                    sr.color = Color.white;
                    sr.sortingLayerName = CharacterSortingLayer;
                    sr.sortingOrder = 8;
                    EditorUtility.SetDirty(sr);
                    updated++;
                }

                var portals = Object.FindObjectsByType<DimensionCluePortal>(FindObjectsInactive.Include);
                foreach (var p in portals)
                {
                    var sr = p.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;
                    Undo.RecordObject(sr, "Cambiar Icono Portal de Pista");
                    sr.sprite = loreSprite;
                    sr.color = Color.white;
                    sr.sortingLayerName = CharacterSortingLayer;
                    sr.sortingOrder = 8;
                    EditorUtility.SetDirty(sr);
                    updated++;
                }
            }

            // 2. Orbes de Estabilidad (+15 HP) con HealingGlobe.png
            if (orbSprite != null)
            {
                var orbs = Object.FindObjectsByType<StabilityOrb>(FindObjectsInactive.Include);
                foreach (var o in orbs)
                {
                    var sr = o.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;
                    Undo.RecordObject(sr, "Cambiar Icono Orbe de Estabilidad");
                    sr.sprite = orbSprite;
                    sr.color = new Color(0.2f, 1f, 0.55f, 1f);
                    sr.sortingLayerName = CharacterSortingLayer;
                    sr.sortingOrder = 8;
                    EditorUtility.SetDirty(sr);
                    updated++;
                }
            }

            if (updated > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Armanya] Se actualizaron {updated} icono(s) de Pistas de Lore y Orbes con sprites de 2DGamekit. Guarda (Ctrl+S).");
            }
            else
            {
                Debug.LogWarning("[Armanya] No se encontraron Pistas de Lore u Orbes en esta escena.");
            }
        }

        // [MenuItem("Armanya/12. Cambiar Sprites a Pilares y Generadores + Eliminar Cubo Dummy")]
        public static void ApplyPillarAndGeneratorSprites()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // 1. Eliminar el cubo blanco Dummy si existe en la escena
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int dummyDeleted = 0;
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                if (go.name.Equals("Dummy", System.StringComparison.OrdinalIgnoreCase) ||
                    (go.GetComponent<TestDummy>() != null && go.GetComponent<EnemyAI2D>() == null))
                {
                    Undo.DestroyObjectImmediate(go);
                    dummyDeleted++;
                }
            }
            if (dummyDeleted > 0)
            {
                Debug.Log($"[Armanya] Se eliminó el cubo blanco 'Dummy' ({dummyDeleted} objeto(s)) ya que los monstruos reales y Ankea ya están integrados.");
            }

            // 2. Cargar sprites de Switch (Switch_0 = averiado, Switch_1 = reparado) para los generadores
            Sprite switch0 = null;
            Sprite switch1 = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/2DGamekit/Art/Sprites/Interactables/Switch.png"))
            {
                if (obj is Sprite s)
                {
                    if (s.name == "Switch_0") switch0 = s;
                    else if (s.name == "Switch_1") switch1 = s;
                }
            }

            // Cargar AlienSculpture01 para el Pilar Dimensional
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/2DGamekit/Art/Sprites/Environment/AlienSculpture01.png");

            int genCount = 0;
            int pillarCount = 0;

            // 3. Configurar Generadores de Fractura
            var generators = Object.FindObjectsByType<FractureGenerator>(FindObjectsInactive.Include);
            foreach (var gen in generators)
            {
                var sr = gen.GetComponent<SpriteRenderer>();
                if (sr == null) sr = gen.gameObject.AddComponent<SpriteRenderer>();

                var so = new SerializedObject(gen);
                var brokenSpriteProp = so.FindProperty("brokenSprite");
                var repairedSpriteProp = so.FindProperty("repairedSprite");
                var brokenColorProp = so.FindProperty("brokenColor");
                var repairedColorProp = so.FindProperty("repairedColor");
                var rendererProp = so.FindProperty("targetRenderer");

                if (rendererProp != null) rendererProp.objectReferenceValue = sr;
                if (brokenSpriteProp != null && switch0 != null) brokenSpriteProp.objectReferenceValue = switch0;
                if (repairedSpriteProp != null && switch1 != null) repairedSpriteProp.objectReferenceValue = switch1;

                // Color según el héroe correspondiente
                string gName = gen.name.ToLowerInvariant();
                Color normalColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                Color activeColor = Color.white;

                if (gName.Contains("angus"))
                {
                    normalColor = new Color(0.7f, 0.55f, 0.2f, 1f);
                    activeColor = new Color(1f, 0.95f, 0.3f, 1f); // Dorado eléctrico
                }
                else if (gName.Contains("anya"))
                {
                    normalColor = new Color(0.55f, 0.25f, 0.65f, 1f);
                    activeColor = new Color(0.95f, 0.4f, 1f, 1f); // Violeta/Magenta
                }
                else if (gName.Contains("aleek"))
                {
                    normalColor = new Color(0.25f, 0.5f, 0.65f, 1f);
                    activeColor = new Color(0.3f, 1f, 1f, 1f); // Cian neón
                }

                if (brokenColorProp != null) brokenColorProp.colorValue = normalColor;
                if (repairedColorProp != null) repairedColorProp.colorValue = activeColor;
                so.ApplyModifiedProperties();

                Undo.RecordObject(sr, "Configurar Sprite de Generador");
                sr.sprite = gen.IsRepaired ? (switch1 != null ? switch1 : switch0) : switch0;
                sr.color = gen.IsRepaired ? activeColor : normalColor;
                sr.sortingLayerName = CharacterSortingLayer;
                sr.sortingOrder = 5;

                // Ajustar BoxCollider2D
                var box = gen.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    Undo.RecordObject(box, "Ajustar Collider Generador");
                    box.isTrigger = true;
                    box.size = new Vector2(1.2f, 1.4f);
                    box.offset = new Vector2(0f, 0.6f);
                }

                EditorUtility.SetDirty(gen);
                EditorUtility.SetDirty(sr);
                genCount++;
            }

            // 4. Configurar Pilares Dimensionales (RestorableObject)
            var restorables = Object.FindObjectsByType<RestorableObject>(FindObjectsInactive.Include);
            foreach (var rest in restorables)
            {
                if (rest.GetComponent<FractureGenerator>() != null) continue; // Generadores se manejan aparte

                var sr = rest.GetComponent<SpriteRenderer>();
                if (sr == null) sr = rest.gameObject.AddComponent<SpriteRenderer>();

                var so = new SerializedObject(rest);
                var damagedColProp = so.FindProperty("damagedColor");
                var restoredColProp = so.FindProperty("restoredColor");
                var targetRendProp = so.FindProperty("targetRenderer");

                if (targetRendProp != null) targetRendProp.objectReferenceValue = sr;

                Color damagedCol = new Color(0.5f, 0.3f, 0.7f, 1f); // Corrompido morado
                Color restoredCol = new Color(0.3f, 1f, 1f, 1f);   // Cian restaurado celestial

                if (damagedColProp != null) damagedColProp.colorValue = damagedCol;
                if (restoredColProp != null) restoredColProp.colorValue = restoredCol;
                so.ApplyModifiedProperties();

                Undo.RecordObject(sr, "Configurar Sprite de Pilar");
                if (pillarSprite != null) sr.sprite = pillarSprite;
                sr.color = rest.IsRestored ? restoredCol : damagedCol;
                sr.sortingLayerName = CharacterSortingLayer;
                sr.sortingOrder = 4;

                var box = rest.GetComponent<BoxCollider2D>();
                if (box != null && pillarSprite != null)
                {
                    Undo.RecordObject(box, "Ajustar Collider Pilar");
                    box.isTrigger = true;
                    box.size = new Vector2(1.2f, 2.6f);
                    box.offset = new Vector2(0f, 1.2f);
                }

                EditorUtility.SetDirty(rest);
                EditorUtility.SetDirty(sr);
                pillarCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Armanya] ¡Listo! Se actualizaron {genCount} Generadores (Switch_0/1) y {pillarCount} Pilares Dimensionales (AlienSculpture01), y se eliminó el cubo Dummy. Guarda (Ctrl+S).");
        }

        // [MenuItem("Armanya/13. Configurar Dialogos de Historia en Escena Actual")]
        public static void SetupStoryDialoguesInScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Desactivar diálogos tutoriales de Ellen de 2D Gamekit para que no interfieran
            var oldDialogues = Object.FindObjectsByType<Gamekit2D.DialogueCanvasController>(FindObjectsInactive.Include);
            foreach (var od in oldDialogues)
            {
                Undo.RecordObject(od.gameObject, "Desactivar DialogueCanvas de Ellen");
                od.gameObject.SetActive(false);
            }

            // Buscar retratos de personajes
            Sprite angusPortrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlayersFolder}/Angus_Portrait.png");
            Sprite anyaPortrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlayersFolder}/Anya_Portrait.png");
            Sprite aleekPortrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlayersFolder}/Aleek_Portrait.png");
            Sprite ankeaPortrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlayersFolder}/Ankea_Portrait.png");

            // Buscar o crear StoryDialogueTrigger
            var existing = Object.FindAnyObjectByType<StoryDialogueTrigger>();
            GameObject host;
            StoryDialogueTrigger trigger;

            if (existing != null)
            {
                host = existing.gameObject;
                trigger = existing;
            }
            else
            {
                host = new GameObject("[Armanya_StoryDialogue]");
                trigger = host.AddComponent<StoryDialogueTrigger>();
                Undo.RegisterCreatedObjectUndo(host, "Crear StoryDialogueTrigger");
            }

            var so = new SerializedObject(trigger);
            so.FindProperty("showAtLevelStart").boolValue = true;
            so.FindProperty("startDelay").floatValue = 0.6f;

            var linesProp = so.FindProperty("lines");
            linesProp.ClearArray();

            string sceneName = scene.name.ToLowerInvariant();

            if (sceneName.Contains("ruinasfinales") || sceneName.Contains("level4"))
            {
                AddDialogueLine(linesProp, "Angus", angusPortrait, "¡Hemos llegado a las Ruinas Finales de Armanya! Ankea ha despertado el cataclismo de la Fractura.");
                AddDialogueLine(linesProp, "Aleek", aleekPortrait, "Su cúpula térmica es impenetrable. Para romperla, debemos restaurar los 3 Generadores de Fractura usando nuestros poderes K correspondientes.");
                AddDialogueLine(linesProp, "Anya", anyaPortrait, "¡Sincronicemos la ofensiva! Angus con Tormenta Eléctrica, yo con Onda Sónica y Aleek con Telequinesis.");
                AddDialogueLine(linesProp, "Ankea", ankeaPortrait, "¡Mortales ilusos! Ninguna dimensión podrá resistir el colapso. ¡Ardan en el fuego del Nexo!");
                AddDialogueLine(linesProp, "Angus", angusPortrait, "Usa [1], [2] y [3] para alternar entre nosotros en combate. ¡Restauremos los 3 generadores y salvemos Armanya!");
            }
            else if (sceneName.Contains("grieta") || sceneName.Contains("level1"))
            {
                AddDialogueLine(linesProp, "Angus", angusPortrait, "La Grieta Árida está perdiendo cohesión dimensional. Las Bestias de Caos rondan los cañones.");
                AddDialogueLine(linesProp, "Angus", angusPortrait, "Usa [J] para atacar con Arco Eléctrico y [K] para invocar la Tormenta. Debo encontrar la Bitácora Hídrica.");
                AddDialogueLine(linesProp, "Angus", angusPortrait, "Si encuentras una compuerta o portal dimensional, pulsa [S] para adentrarte en las cavernas subterráneas.");
            }
            else if (sceneName.Contains("caos") || sceneName.Contains("level2"))
            {
                AddDialogueLine(linesProp, "Anya", anyaPortrait, "Distrito Caos... Las ondas gravitatorias están invertidas y los Ojos Fantasma vigilan desde el aire.");
                AddDialogueLine(linesProp, "Anya", anyaPortrait, "Con [J] disparo ráfagas sónicas veloces, y con [K] realizo un Dash Intangible con onda de choque.");
                AddDialogueLine(linesProp, "Anya", anyaPortrait, "La pista que busco es el Manifiesto Roto. ¡Ningún monstruo detendrá mi camino!");
            }
            else if (sceneName.Contains("nexo") || sceneName.Contains("level3"))
            {
                AddDialogueLine(linesProp, "Aleek", aleekPortrait, "Nexo Orbital. La estación cuántica está infestada de Centinelas Tentaculares.");
                AddDialogueLine(linesProp, "Aleek", aleekPortrait, "Con [J] disparo proyectiles cinéticos y con [K] levanto un Escudo Cinético que absorbe impactos.");
                AddDialogueLine(linesProp, "Aleek", aleekPortrait, "Debo calibrar la Consola Central para activar el Portal de Convergencia hacia las Ruinas Finales.");
            }
            else
            {
                AddDialogueLine(linesProp, "Angus", angusPortrait, "¡Bienvenidos a Armanya: Fractured Dimensions! El multiverso ha colapsado.");
                AddDialogueLine(linesProp, "Anya", anyaPortrait, "Usa [1], [2] y [3] para cambiar de héroe, [J] para ataque básico y [K] para poder secreto.");
                AddDialogueLine(linesProp, "Aleek", aleekPortrait, "Recoge las 3 pistas de lore y cruza los portales con [S] para restaurar la realidad.");
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Armanya] ¡Diálogos de historia configurados con éxito en '{scene.name}'! Diálogos de tutorial anteriores desactivados. Guarda (Ctrl+S).");
        }

        static void AddDialogueLine(SerializedProperty linesProp, string speaker, Sprite portrait, string message)
        {
            int index = linesProp.arraySize;
            linesProp.InsertArrayElementAtIndex(index);
            var elem = linesProp.GetArrayElementAtIndex(index);
            elem.FindPropertyRelative("speakerName").stringValue = speaker;
            elem.FindPropertyRelative("speakerPortrait").objectReferenceValue = portrait;
            elem.FindPropertyRelative("message").stringValue = message;
        }

        // [MenuItem("Armanya/14. Crear Portal a Sub-Zona (Explorar con Tecla S)")]
        public static void CreateSubzonePortalInScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Cargar sprite de puerta de 2D Gamekit
            Sprite doorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/2DGamekit/Art/Sprites/Interactables/Door.png");

            // Crear objeto entrada
            var entrance = new GameObject("Portal_Subzona_Entrada");
            var sr = entrance.AddComponent<SpriteRenderer>();
            if (doorSprite != null) sr.sprite = doorSprite;
            sr.sortingLayerName = CharacterSortingLayer;
            sr.sortingOrder = 3;

            var col = entrance.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3f);
            col.offset = new Vector2(0f, 1.5f);

            var portal = entrance.AddComponent<SubzonePortal>();

            // Crear punto de destino en el mapa (desplazado a otra zona o caverna)
            var dest = new GameObject("Subzona_Destino_Cavernas");
            dest.transform.position = entrance.transform.position + new Vector3(25f, -8f, 0f);

            var so = new SerializedObject(portal);
            so.FindProperty("targetDestination").objectReferenceValue = dest.transform;
            so.FindProperty("portalName").stringValue = "Cavernas Profundas";
            so.FindProperty("hintMessage").stringValue = "Presiona [S] para descender a las Cavernas";
            so.FindProperty("useSKey").boolValue = true;
            so.FindProperty("useEKey").boolValue = true;
            so.ApplyModifiedProperties();

            // Ubicar la entrada cerca del jugador si existe
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                entrance.transform.position = player.transform.position + new Vector3(8f, 0f, 0f);
                dest.transform.position = entrance.transform.position + new Vector3(25f, -6f, 0f);
            }

            Undo.RegisterCreatedObjectUndo(entrance, "Crear Portal Subzona");
            Undo.RegisterCreatedObjectUndo(dest, "Crear Destino Subzona");

            Selection.activeGameObject = entrance;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Armanya] Se creó el portal 'Portal_Subzona_Entrada' y su destino 'Subzona_Destino_Cavernas'. Puedes moverlos a donde gustes en la escena.");
        }
    }
}
#endif
