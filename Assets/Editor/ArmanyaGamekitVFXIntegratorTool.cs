#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Armanya.Abilities;
using Armanya.Enemies;
using Armanya.Gameplay;
using Armanya.Interactables;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Reutiliza los prefabs visuales de Assets/2DGamekit/ (proyectiles, VFX de impacto/daño y portales)
    /// para crear variantes limpias en Assets/Prefabs/ conectadas exclusivamente a los scripts de Armanya
    /// (Projectile2D, EnemyProjectile2D, HealthSystem y ArmanyaPortalController/GameManager).
    /// </summary>
    public static class ArmanyaGamekitVFXIntegratorTool
    {
        // Fuentes visuales de 2DGamekit
        const string GkBulletPath            = "Assets/2DGamekit/Prefabs/VFX/Ellen/Bullet.prefab";
        const string GkBulletImpactPath      = "Assets/2DGamekit/Prefabs/VFX/Ellen/BulletImpact.prefab";
        const string GkEllenHurtPath         = "Assets/2DGamekit/Prefabs/VFX/Ellen/EllenHurt.prefab";
        const string GkSpitterSpitPath       = "Assets/2DGamekit/Prefabs/VFX/Enemies/Spitter/SpitProjectille.prefab";
        const string GkGunnerProjectilePath  = "Assets/2DGamekit/Prefabs/VFX/Enemies/Gunner/GunnerProjectile.prefab";
        const string GkGunnerHitPath         = "Assets/2DGamekit/Prefabs/VFX/Enemies/Gunner/GunnerProjectileHit.prefab";
        const string GkChomperDiePath        = "Assets/2DGamekit/Prefabs/VFX/Enemies/Chomper/ChomperDie.prefab";
        const string GkTeleporterPath        = "Assets/2DGamekit/Prefabs/Interactables/Teleporter.prefab";

        // Carpetas de salida limpias de Armanya
        const string ProjectilesOutFolder    = "Assets/Prefabs/Projectiles";
        const string InteractablesOutFolder  = "Assets/Prefabs/Interactables";
        const string MonstersFolder          = "Assets/Prefabs/Monsters";
        const string AbilityFolder           = "Assets/Scripts";

        // Colores por personaje / enemigo
        static readonly Color ColorAleek     = new Color(0.15f, 0.92f, 1.00f, 1f); // Cian
        static readonly Color ColorAngus     = new Color(1.00f, 0.90f, 0.25f, 1f); // Amarillo / Celeste eléctrico
        static readonly Color ColorAnya      = new Color(0.75f, 0.25f, 1.00f, 1f); // Violeta neón
        static readonly Color ColorOjo       = new Color(1.00f, 0.20f, 0.28f, 1f); // Rojo
        static readonly Color ColorCentinela = new Color(0.30f, 1.00f, 0.20f, 1f); // Verde ácido
        static readonly Color ColorAnkea     = new Color(1.00f, 0.42f, 0.08f, 1f); // Naranja fuego

        static readonly string[] LevelScenes =
        {
            "Assets/Scenes/Biomes/Level1_GrietaArida.unity",
            "Assets/Scenes/Biomes/Level2_DistritoCaos.unity",
            "Assets/Scenes/Biomes/Level3_NexoOrbital.unity",
            "Assets/Scenes/Biomes/Level4_RuinasFinales.unity"
        };

        [MenuItem("Armanya/5. Integrar Proyectiles, VFX y Portales de 2DGamekit")]
        public static void IntegrateAllGamekitVisuals()
        {
            Directory.CreateDirectory(ProjectilesOutFolder);
            Directory.CreateDirectory(InteractablesOutFolder);

            GameObject bulletImpactVfx = AssetDatabase.LoadAssetAtPath<GameObject>(GkBulletImpactPath);
            GameObject gunnerHitVfx    = AssetDatabase.LoadAssetAtPath<GameObject>(GkGunnerHitPath);
            GameObject hurtVfx         = AssetDatabase.LoadAssetAtPath<GameObject>(GkEllenHurtPath);
            GameObject deathVfx        = AssetDatabase.LoadAssetAtPath<GameObject>(GkChomperDiePath);

            // 1. Proyectiles limpios de Héroes (usando visual de Bullet.prefab + Projectile2D + BulletImpact)
            var projAleek = CreateHeroProjectileVariant("HeroProjectile_Aleek", GkBulletPath, ColorAleek, bulletImpactVfx);
            var projAngus = CreateHeroProjectileVariant("HeroProjectile_Angus", GkBulletPath, ColorAngus, bulletImpactVfx);
            var projAnya  = CreateHeroProjectileVariant("HeroProjectile_Anya",  GkBulletPath, ColorAnya,  bulletImpactVfx);

            // Conectar proyectiles a los ScriptableObjects AbilityData (sin alterar daño ni forma melee/area)
            AssignHeroProjectileToAbility($"{AbilityFolder}/Aleek_Basic.asset", projAleek);
            AssignHeroProjectileToAbility($"{AbilityFolder}/Angus_Basic.asset", projAngus);
            AssignHeroProjectileToAbility($"{AbilityFolder}/Anya_Basic.asset",  projAnya);

            // 2. Proyectiles limpios de Monstruos y Ankea (usando visuales 2DGamekit + EnemyProjectile2D + VFX impacto)
            var projOjo       = CreateEnemyProjectileVariant("EnemyProjectile_OjoFantasma", GkSpitterSpitPath,      ColorOjo,       15, 8.0f, bulletImpactVfx);
            var projCentinela = CreateEnemyProjectileVariant("EnemyProjectile_Centinela",   GkSpitterSpitPath,      ColorCentinela, 15, 8.5f, bulletImpactVfx);
            var projAnkea     = CreateEnemyProjectileVariant("EnemyProjectile_Ankea",       GkGunnerProjectilePath, ColorAnkea,     20, 9.5f, gunnerHitVfx != null ? gunnerHitVfx : bulletImpactVfx);

            // Conectar proyectiles + VFX de daño/muerte a los prefabs de monstruos
            WireMonsterPrefab($"{MonstersFolder}/OjoFantasma.prefab",         projOjo,       bulletImpactVfx, deathVfx);
            WireMonsterPrefab($"{MonstersFolder}/CentinelaTentacular.prefab", projCentinela, bulletImpactVfx, deathVfx);
            WireMonsterPrefab($"{MonstersFolder}/BestiaDeCaos.prefab",        null,          bulletImpactVfx, deathVfx);
            WireMonsterPrefab($"{MonstersFolder}/MonstruoTresOjos.prefab",    null,          bulletImpactVfx, deathVfx);
            WireAnkeaBossPrefab($"{MonstersFolder}/Ankea_Boss.prefab",        projAnkea,     gunnerHitVfx != null ? gunnerHitVfx : bulletImpactVfx, deathVfx);

            // 3. Crear prefab limpio de Portal de Escena (visual Teleporter de 2DGamekit + ArmanyaPortalController)
            CreateDimensionPortalVariant();

            // 4. Conectar VFX de daño (hitVfxPrefab) en los Héroes de las 4 escenas sin tocar nada más
            WireHeroHitVfxInAllLevels(hurtVfx != null ? hurtVfx : bulletImpactVfx);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Armanya] ¡Integración de 2DGamekit completada! Proyectiles tintados (Aleek/Angus/Anya/Monstruos/Ankea), VFX de impacto/daño y Portal Dimensional listos.");
        }

        static Projectile2D CreateHeroProjectileVariant(string name, string sourcePrefabPath, Color tint, GameObject impactVfx)
        {
            GameObject go = InstantiateCleanVisual(sourcePrefabPath, name);
            StripGamekitScripts(go);

            // Asegurar física y collider trigger limpios para Projectile2D
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            EnsureSingleTriggerCollider(go, 0.35f);
            TintVisuals(go, tint);

            var proj = go.AddComponent<Projectile2D>();
            var so = new SerializedObject(proj);
            so.FindProperty("impactVfxPrefab").objectReferenceValue = impactVfx;
            so.ApplyModifiedPropertiesWithoutUndo();

            string outPath = $"{ProjectilesOutFolder}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, outPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Projectile2D>();
        }

        static EnemyProjectile2D CreateEnemyProjectileVariant(string name, string sourcePrefabPath, Color tint, int damage, float speed, GameObject impactVfx)
        {
            GameObject go = InstantiateCleanVisual(sourcePrefabPath, name);
            StripGamekitScripts(go);

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;

            EnsureSingleTriggerCollider(go, 0.35f);
            TintVisuals(go, tint);

            var proj = go.AddComponent<EnemyProjectile2D>();
            var so = new SerializedObject(proj);
            so.FindProperty("damage").intValue = damage;
            so.FindProperty("speed").floatValue = speed;
            so.FindProperty("projectileTint").colorValue = tint;
            so.FindProperty("impactVfxPrefab").objectReferenceValue = impactVfx;
            so.ApplyModifiedPropertiesWithoutUndo();

            string outPath = $"{ProjectilesOutFolder}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, outPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<EnemyProjectile2D>();
        }

        static void CreateDimensionPortalVariant()
        {
            GameObject go = InstantiateCleanVisual(GkTeleporterPath, "Armanya_DimensionPortal");
            StripGamekitScripts(go);

            // Quitar el material viejo de 2DGamekit (que se vuelve invisible en URP 2D) y usar Sprite-Default limpio
            var defaultSpriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (defaultSpriteMat != null) sr.sharedMaterial = defaultSpriteMat;
                sr.color = new Color(0.55f, 0.95f, 1f, 1f);
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 6;
            }

            // Quitar Animator viejo del Teleporter para que no oculte el sprite por transparencia
            foreach (var anim in go.GetComponentsInChildren<Animator>(true))
                Object.DestroyImmediate(anim);

            // Quitar colliders viejos no-trigger y asegurar un BoxCollider2D trigger limpio
            foreach (var c in go.GetComponents<Collider2D>())
                Object.DestroyImmediate(c);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.8f, 2.6f);

            // Añadir brillo de partículas cian para que el portal destaque claramente
            var ps = go.GetComponentInChildren<ParticleSystem>(true);
            if (ps == null)
            {
                var vfxChild = new GameObject("PortalGlowVFX");
                vfxChild.transform.SetParent(go.transform, false);
                ps = vfxChild.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 1.1f;
                main.startSpeed = 0.8f;
                main.startSize = 0.22f;
                main.startColor = new Color(0.2f, 0.95f, 1f, 0.85f);
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var emission = ps.emission;
                emission.rateOverTime = 18f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.7f;
                var renderer = vfxChild.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && defaultSpriteMat != null)
                {
                    renderer.sharedMaterial = defaultSpriteMat;
                    renderer.sortingOrder = 7;
                }
            }

            go.AddComponent<ArmanyaPortalController>();

            string outPath = $"{InteractablesOutFolder}/Armanya_DimensionPortal.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, outPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[Armanya] Portal Dimensional visible creado en '{outPath}' (conectado a GameManager).");
        }

        static void AssignHeroProjectileToAbility(string abilityPath, Projectile2D proj)
        {
            if (proj == null) return;
            var data = AssetDatabase.LoadAssetAtPath<AbilityData>(abilityPath);
            if (data == null) return;

            var so = new SerializedObject(data);
            var prop = so.FindProperty("projectilePrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = proj;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }
        }

        static void WireMonsterPrefab(string prefabPath, EnemyProjectile2D proj, GameObject hitVfx, GameObject deathVfx)
        {
            if (!File.Exists(prefabPath)) return;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;

            var ai = root.GetComponent<EnemyAI2D>();
            if (ai != null && proj != null)
            {
                var so = new SerializedObject(ai);
                so.FindProperty("projectilePrefab").objectReferenceValue = proj;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var hs = root.GetComponent<HealthSystem>();
            if (hs != null)
            {
                var hso = new SerializedObject(hs);
                if (hitVfx != null)   hso.FindProperty("hitVfxPrefab").objectReferenceValue = hitVfx;
                if (deathVfx != null) hso.FindProperty("deathVfxPrefab").objectReferenceValue = deathVfx;
                hso.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void WireAnkeaBossPrefab(string prefabPath, EnemyProjectile2D proj, GameObject hitVfx, GameObject deathVfx)
        {
            if (!File.Exists(prefabPath)) return;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;

            var boss = root.GetComponent<AnkeaBossAI>();
            if (boss != null && proj != null)
            {
                var so = new SerializedObject(boss);
                var prop = so.FindProperty("thermalOrbPrefab");
                if (prop != null) prop.objectReferenceValue = proj;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var hs = root.GetComponent<HealthSystem>();
            if (hs != null)
            {
                var hso = new SerializedObject(hs);
                if (hitVfx != null)   hso.FindProperty("hitVfxPrefab").objectReferenceValue = hitVfx;
                if (deathVfx != null) hso.FindProperty("deathVfxPrefab").objectReferenceValue = deathVfx;
                hso.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void WireHeroHitVfxInAllLevels(GameObject hurtVfx)
        {
            if (hurtVfx == null) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string activePath = EditorSceneManager.GetActiveScene().path;

            foreach (string path in LevelScenes)
            {
                if (!File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool modified = false;

                foreach (var user in Object.FindObjectsByType<AbilityUser>(FindObjectsInactive.Include))
                {
                    var hs = user.GetComponent<HealthSystem>();
                    if (hs == null) continue;
                    var hso = new SerializedObject(hs);
                    hso.FindProperty("hitVfxPrefab").objectReferenceValue = hurtVfx;
                    hso.ApplyModifiedPropertiesWithoutUndo();
                    modified = true;
                }

                if (modified)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(activePath) && File.Exists(activePath))
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
        }

        static GameObject InstantiateCleanVisual(string sourcePrefabPath, string newName)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            GameObject go;
            if (src != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                go.name = newName;
            }
            else
            {
                go = new GameObject(newName);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            }
            return go;
        }

        /// <summary>Elimina cualquier MonoBehaviour perteneciente a Gamekit2D para que solo corran nuestros scripts de Armanya.</summary>
        static void StripGamekitScripts(GameObject root)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in behaviours)
            {
                if (mb == null) continue;
                string ns = mb.GetType().Namespace ?? "";
                if (ns.StartsWith("Gamekit2D"))
                {
                    Object.DestroyImmediate(mb);
                }
            }
        }

        static void EnsureSingleTriggerCollider(GameObject go, float radius)
        {
            var existing = go.GetComponents<Collider2D>();
            if (existing.Length == 0)
            {
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = radius;
            }
            else
            {
                foreach (var c in existing) c.isTrigger = true;
            }
        }

        static void TintVisuals(GameObject go, Color tint)
        {
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.color = tint;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 12;
            }

            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.startColor = tint;
            }
        }
    }
}
#endif
