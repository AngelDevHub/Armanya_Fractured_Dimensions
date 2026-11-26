#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Armanya.Abilities;
using Armanya.Enemies;
using Armanya.Gameplay;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Arma los 4 prefabs de enemigos (Rigidbody2D, Collider2D, HealthSystem, barra de vida UI sobre la cabeza,
    /// EnemyAI2D), los proyectiles enemigos, el escudo cinético en la cúpula de Aleek y el prefab de Ankea (Nivel 4).
    /// </summary>
    public static class ArmanyaEnemyBuilderTool
    {
        const string MonstersFolder = "Assets/Prefabs/Monsters";
        const string ProjectilesFolder = "Assets/Prefabs/Projectiles";
        const string VfxFolder = "Assets/Prefabs/VFX";
        const string PickupOrbPath = "Assets/Prefabs/Pickups/StabilityOrb.prefab";
        const string AleekDomePath = "Assets/Prefabs/VFX/VFX_Aleek_KineticDome.prefab";

        // [MenuItem("Armanya/9. Armar Prefabs de Enemigos (IA, Barra Vida, Proyectiles y Ankea)")]
        public static void BuildAllEnemyPrefabs()
        {
            Directory.CreateDirectory(MonstersFolder);
            Directory.CreateDirectory(ProjectilesFolder);
            Directory.CreateDirectory(VfxFolder);

            Sprite circleSprite = FindBuiltinSprite("Circle");
            Sprite squareSprite = FindBuiltinSprite("Square");
            GameObject orbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupOrbPath);

            // 1. Añadir KineticShield2D a la Cúpula Cinética de Aleek para que bloquee proyectiles enemigos
            AddKineticShieldToAleekDome();

            // 2. Proyectil enemigo estándar (Ojo Fantasma / Centinela)
            var enemyProj = CreateEnemyProjectilePrefab(
                $"{ProjectilesFolder}/EnemyProjectile.prefab", "EnemyProjectile",
                circleSprite, new Color(1f, 0.25f, 0.35f, 1f), new Vector3(0.42f, 0.42f, 1f), 15, 8f);

            // 3. Orbe Térmico de Ankea (20 daño) y Columna de Fuego (30 daño)
            var ankeaOrb = CreateEnemyProjectilePrefab(
                $"{ProjectilesFolder}/AnkeaThermalOrb.prefab", "AnkeaThermalOrb",
                circleSprite, new Color(1f, 0.5f, 0.08f, 1f), new Vector3(0.6f, 0.6f, 1f), 20, 9f);

            var fireColumn = CreateFireColumnPrefab(
                $"{VfxFolder}/AnkeaFireColumn.prefab", squareSprite);

            // 4. Armar los 4 prefabs de enemigos con IA, físicas, HealthSystem y barra de vida UI
            BuildEnemyPrefab("BestiaDeCaos", "Assets/Art/Sprites/Monsters/BestiaDeCaos_Side.png",
                EnemyBehaviorMode.GroundMelee, maxHp: 100, damage: 18, cooldown: 1.5f,
                patrolSpd: 2.5f, chaseSpd: 4.5f, hoverAmp: 0f, proj: null, orbPrefab: orbPrefab);

            BuildEnemyPrefab("MonstruoTresOjos", "Assets/Art/Sprites/Monsters/MonstruoTresOjos_Side.png",
                EnemyBehaviorMode.GroundMelee, maxHp: 75, damage: 12, cooldown: 1.6f,
                patrolSpd: 2.2f, chaseSpd: 3.8f, hoverAmp: 0f, proj: null, orbPrefab: orbPrefab);

            BuildEnemyPrefab("OjoFantasma", "Assets/Art/Sprites/Monsters/OjoFantasma_Side.png",
                EnemyBehaviorMode.FlyingRanged, maxHp: 65, damage: 15, cooldown: 2.2f,
                patrolSpd: 0f, chaseSpd: 0f, hoverAmp: 0.4f, proj: enemyProj, orbPrefab: orbPrefab);

            BuildEnemyPrefab("CentinelaTentacular", "Assets/Art/Sprites/Monsters/CentinelaTentacular_Side.png",
                EnemyBehaviorMode.FlyingRanged, maxHp: 90, damage: 15, cooldown: 1.9f,
                patrolSpd: 0f, chaseSpd: 0f, hoverAmp: 0f, proj: enemyProj, orbPrefab: orbPrefab);

            // 5. Armar Prefab de Ankea (Jefe Nivel 4)
            BuildAnkeaBossPrefab(ankeaOrb, fireColumn);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Armanya] ¡Listos los 4 prefabs de enemigos (con IA + barra de vida), proyectiles, Escudo Cinético de Aleek y Ankea!");
        }

        // [MenuItem("Armanya/10. Poblar Enemigos del Bioma en Escena Actual")]
        public static void PopulateEnemiesForCurrentBiome()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            string sn = scene.name.ToLowerInvariant();
            int spawned = 0;

            if (sn.Contains("grietaarida") || sn.Contains("level1"))
            {
                spawned += SpawnPrefab($"{MonstersFolder}/BestiaDeCaos.prefab", new Vector3(4f, -2.2f, 0f));
                spawned += SpawnPrefab($"{MonstersFolder}/MonstruoTresOjos.prefab", new Vector3(10f, -1.5f, 0f));
            }
            else if (sn.Contains("distritocaos") || sn.Contains("level2"))
            {
                spawned += SpawnPrefab($"{MonstersFolder}/OjoFantasma.prefab", new Vector3(5f, 0.5f, 0f));
                spawned += SpawnPrefab($"{MonstersFolder}/MonstruoTresOjos.prefab", new Vector3(9f, -1.5f, 0f));
            }
            else if (sn.Contains("nexoorbital") || sn.Contains("level3"))
            {
                spawned += SpawnPrefab($"{MonstersFolder}/CentinelaTentacular.prefab", new Vector3(6f, -2.2f, 0f));
                spawned += SpawnPrefab($"{MonstersFolder}/OjoFantasma.prefab", new Vector3(11f, 0.5f, 0f));
            }
            else if (sn.Contains("ruinasfinales") || sn.Contains("level4"))
            {
                spawned += SpawnPrefab($"{MonstersFolder}/Ankea_Boss.prefab", new Vector3(9f, 1.8f, 0f));
            }
            else
            {
                Debug.LogWarning($"[Armanya] Escena '{scene.name}' no coincide con Level1..4. Arrastra prefabs manualmente desde {MonstersFolder}.");
                return;
            }

            if (spawned > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Armanya] Se colocaron {spawned} enemigo(s) en '{scene.name}'. Ajusta sus posiciones si hace falta y guarda (Ctrl+S).");
            }
        }

        static int SpawnPrefab(string path, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Armanya] No se encontró '{path}'. Ejecuta primero el paso 9.");
                return 0;
            }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(inst, "Colocar Enemigo");
            inst.transform.position = pos;
            return 1;
        }

        static void AddKineticShieldToAleekDome()
        {
            if (!File.Exists(AleekDomePath)) return;
            var root = PrefabUtility.LoadPrefabContents(AleekDomePath);
            if (root == null) return;

            var col = root.GetComponent<CircleCollider2D>();
            if (col == null) col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            if (root.GetComponent<KineticShield2D>() == null)
                root.AddComponent<KineticShield2D>();

            PrefabUtility.SaveAsPrefabAsset(root, AleekDomePath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static EnemyProjectile2D CreateEnemyProjectilePrefab(
            string path, string name, Sprite sprite, Color color, Vector3 scale, int damage, float speed)
        {
            var go = new GameObject(name);
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 12;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            var proj = go.AddComponent<EnemyProjectile2D>();
            var so = new SerializedObject(proj);
            so.FindProperty("damage").intValue = damage;
            so.FindProperty("speed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<EnemyProjectile2D>();
        }

        static FireColumnHazard CreateFireColumnPrefab(string path, Sprite squareSprite)
        {
            var go = new GameObject("AnkeaFireColumn");
            go.transform.localScale = new Vector3(1.4f, 4.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color = new Color(1f, 0.35f, 0.08f, 0.85f);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 11;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            go.AddComponent<FireColumnHazard>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<FireColumnHazard>();
        }

        static void BuildEnemyPrefab(
            string monsterName, string spritePath, EnemyBehaviorMode mode,
            int maxHp, int damage, float cooldown, float patrolSpd, float chaseSpd,
            float hoverAmp, EnemyProjectile2D proj, GameObject orbPrefab)
        {
            string prefabPath = $"{MonstersFolder}/{monsterName}.prefab";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[Armanya] Falta sprite '{spritePath}'.");
                return;
            }

            var go = new GameObject(monsterName);
            go.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) go.layer = enemyLayer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 9;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            if (mode == EnemyBehaviorMode.GroundMelee)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 3f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.size = sprite.bounds.size * 0.82f;

            // FirePoint para enemigos a distancia
            Transform fp = null;
            if (mode == EnemyBehaviorMode.FlyingRanged)
            {
                var fpGo = new GameObject("FirePoint");
                fpGo.transform.SetParent(go.transform, false);
                fpGo.transform.localPosition = new Vector3(0.6f, 0.1f, 0f);
                fp = fpGo.transform;
            }

            // Barra de vida pequeña World-Space sobre la cabeza del enemigo
            Slider slider = CreateWorldHealthBar(go.transform, sprite.bounds.extents.y + 0.35f);

            // HealthSystem (con 30% drop de StabilityOrb)
            var hs = go.AddComponent<HealthSystem>();
            var hso = new SerializedObject(hs);
            hso.FindProperty("maxHealth").intValue = maxHp;
            hso.FindProperty("currentHealth").intValue = maxHp;
            hso.FindProperty("healthSlider").objectReferenceValue = slider;
            hso.FindProperty("destroyOnDeath").boolValue = true;
            hso.FindProperty("dropOrbOnDeath").boolValue = true;
            hso.FindProperty("orbDropChance").floatValue = 0.30f;
            hso.FindProperty("stabilityOrbPrefab").objectReferenceValue = orbPrefab;
            hso.ApplyModifiedPropertiesWithoutUndo();

            // EnemyAI2D
            var ai = go.AddComponent<EnemyAI2D>();
            var aso = new SerializedObject(ai);
            aso.FindProperty("mode").enumValueIndex = (int)mode;
            aso.FindProperty("detectionRadius").floatValue = mode == EnemyBehaviorMode.FlyingRanged ? 10f : 7.5f;
            aso.FindProperty("patrolSpeed").floatValue = patrolSpd;
            aso.FindProperty("chaseSpeed").floatValue = chaseSpd;
            aso.FindProperty("patrolDistance").floatValue = 3.5f;
            aso.FindProperty("hoverAmplitude").floatValue = hoverAmp;
            aso.FindProperty("projectilePrefab").objectReferenceValue = proj;
            aso.FindProperty("firePoint").objectReferenceValue = fp;
            aso.FindProperty("attackDamage").intValue = damage;
            aso.FindProperty("attackCooldown").floatValue = cooldown;
            aso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        static void BuildAnkeaBossPrefab(EnemyProjectile2D thermalOrb, FireColumnHazard fireColumn)
        {
            string prefabPath = $"{MonstersFolder}/Ankea_Boss.prefab";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Players/Ankea_Side.png");

            var go = new GameObject("Ankea_Boss");
            go.tag = "Enemy";
            go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;

            var fpGo = new GameObject("FirePoint");
            fpGo.transform.SetParent(go.transform, false);
            fpGo.transform.localPosition = new Vector3(0.7f, 0.1f, 0f);

            var ai = go.AddComponent<AnkeaBossAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("thermalOrbPrefab").objectReferenceValue = thermalOrb;
            so.FindProperty("firePoint").objectReferenceValue = fpGo.transform;
            so.FindProperty("thermalOrbDamage").intValue = 20;
            so.FindProperty("fireColumnPrefab").objectReferenceValue = fireColumn;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        static Slider CreateWorldHealthBar(Transform parent, float yOffset)
        {
            var canvasGo = new GameObject("HealthBarCanvas");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, yOffset, 0f);
            canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 1f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "Default";
            canvas.sortingOrder = 15;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140f, 20f);

            var sliderGo = new GameObject("HealthSlider");
            sliderGo.transform.SetParent(canvasGo.transform, false);
            var srt = sliderGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            var bgImg = sliderGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.offsetMin = new Vector2(2f, 2f);
            fart.offsetMax = new Vector2(-2f, -2f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var frt = fill.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.9f, 0.18f, 0.22f, 1f);

            var slider = sliderGo.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = frt;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;

            return slider;
        }

        static Sprite FindBuiltinSprite(string spriteName)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null && s.name == spriteName) return s;
            }
            return null;
        }
    }
}
#endif
