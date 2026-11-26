#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Armanya.Abilities;
using Armanya.Gameplay;
using Armanya.Interactables;
using Armanya.Player;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Genera los prefabs VFX de combate (J/K), el prefab de Orbe de Estabilidad (+15 HP),
    /// configura los 6 AbilityData con los valores exactos de Aleek, Angus y Anya,
    /// y conecta FirePoint + HealthSystem en héroes y monstruos.
    /// </summary>
    public static class ArmanyaCombatVFXBuilderTool
    {
        const string VfxFolder = "Assets/Prefabs/VFX";
        const string PickupFolder = "Assets/Prefabs/Pickups";
        const string MonstersFolder = "Assets/Prefabs/Monsters";
        const string AbilityFolder = "Assets/Scripts";

        static readonly Color CyanAleek = new Color(0.15f, 0.92f, 1f, 0.9f);
        static readonly Color ElectricAngus = new Color(1f, 0.92f, 0.25f, 0.92f);
        static readonly Color LightningAngus = new Color(0.45f, 0.92f, 1f, 0.92f);
        static readonly Color VioletAnya = new Color(0.75f, 0.25f, 1f, 0.9f);

        [MenuItem("Armanya/7. Generar VFX de Combate (J/K), Habilidades y Monstruos")]
        public static void BuildCombatVfxAndData()
        {
            Directory.CreateDirectory(VfxFolder);
            Directory.CreateDirectory(PickupFolder);

            Sprite circleSprite = FindBuiltinSprite("Circle");
            Sprite squareSprite = FindBuiltinSprite("Square");
            Sprite capsuleSprite = FindBuiltinSprite("Capsule");

            // 1. Prefab de Orbe de Estabilidad Dimensional (+15 HP)
            var orbPrefab = CreateStabilityOrbPrefab(circleSprite);

            // 2. Prefabs VFX (proyectiles, arcos, cúpulas, rayos y ondas con partículas)
            var aleekProj = CreateProjectilePrefab($"{VfxFolder}/VFX_Aleek_Projectile.prefab", "VFX_Aleek_Projectile",
                circleSprite, CyanAleek, new Vector3(0.45f, 0.45f, 1f));
            var aleekDome = CreateAreaVfxPrefab($"{VfxFolder}/VFX_Aleek_KineticDome.prefab", "VFX_Aleek_KineticDome",
                circleSprite, CyanAleek);

            var angusArc = CreateAreaVfxPrefab($"{VfxFolder}/VFX_Angus_ElectricArc.prefab", "VFX_Angus_ElectricArc",
                capsuleSprite != null ? capsuleSprite : squareSprite, ElectricAngus);
            var angusBolt = CreateAreaVfxPrefab($"{VfxFolder}/VFX_Angus_LightningArea.prefab", "VFX_Angus_LightningArea",
                circleSprite, LightningAngus);

            var anyaBurst = CreateProjectilePrefab($"{VfxFolder}/VFX_Anya_SonicBurst.prefab", "VFX_Anya_SonicBurst",
                capsuleSprite != null ? capsuleSprite : circleSprite, VioletAnya, new Vector3(0.55f, 0.28f, 1f));
            var anyaWave = CreateAreaVfxPrefab($"{VfxFolder}/VFX_Anya_SonicWave.prefab", "VFX_Anya_SonicWave",
                circleSprite, VioletAnya);

            // 3. Configurar los 6 AbilityData con los valores exactos de cada héroe
            // Aleek (Cian): J = proyectil 20 daño; K = cúpula cinética 40 daño, stun 2.5s, cd 4.0s
            ConfigureAbility($"{AbilityFolder}/Aleek_Basic.asset", "Proyectil Cinético", AbilityShape.Projectile,
                cooldown: 0.45f, damage: 20f, range: 1.5f, height: 1f, knockback: 0f, stun: 0f,
                dashDur: 0f, dashSpd: 0f, proj: aleekProj, projSpd: 14f, vfx: null, color: CyanAleek,
                vfxLife: 0.35f, rest: RestorationEffect.None);

            ConfigureAbility($"{AbilityFolder}/Aleek_Secret.asset", "Cúpula Cinética", AbilityShape.Area,
                cooldown: 4.0f, damage: 40f, range: 3.2f, height: 1f, knockback: 0f, stun: 2.5f,
                dashDur: 0f, dashSpd: 0f, proj: null, projSpd: 0f, vfx: aleekDome, color: CyanAleek,
                vfxLife: 0.55f, rest: RestorationEffect.Telekinesis);

            // Angus (Celeste/Amarillo): J = arco eléctrico 25 daño + knockback; K = rayo en área 45 daño, stun 2.5s, cd 4.5s
            ConfigureAbility($"{AbilityFolder}/Angus_Basic.asset", "Arco Eléctrico", AbilityShape.Melee,
                cooldown: 0.55f, damage: 25f, range: 2.0f, height: 1.4f, knockback: 8f, stun: 0f,
                dashDur: 0f, dashSpd: 0f, proj: null, projSpd: 0f, vfx: angusArc, color: ElectricAngus,
                vfxLife: 0.25f, rest: RestorationEffect.None);

            ConfigureAbility($"{AbilityFolder}/Angus_Secret.asset", "Tormenta Eléctrica", AbilityShape.Area,
                cooldown: 4.5f, damage: 45f, range: 3.5f, height: 1f, knockback: 4f, stun: 2.5f,
                dashDur: 0f, dashSpd: 0f, proj: null, projSpd: 0f, vfx: angusBolt, color: LightningAngus,
                vfxLife: 0.55f, rest: RestorationEffect.LightningWeather);

            // Anya (Violeta): J = ráfaga sónica 15 daño; K = dash intangible 0.5s + onda frontal 35 daño, stun 3.0s, cd 3.5s
            ConfigureAbility($"{AbilityFolder}/Anya_Basic.asset", "Ráfaga Sónica", AbilityShape.Projectile,
                cooldown: 0.30f, damage: 15f, range: 1.5f, height: 1f, knockback: 0f, stun: 0f,
                dashDur: 0f, dashSpd: 0f, proj: anyaBurst, projSpd: 18f, vfx: null, color: VioletAnya,
                vfxLife: 0.30f, rest: RestorationEffect.None);

            ConfigureAbility($"{AbilityFolder}/Anya_Secret.asset", "Dash Intangible + Onda", AbilityShape.Area,
                cooldown: 3.5f, damage: 35f, range: 3.0f, height: 1f, knockback: 0f, stun: 3.0f,
                dashDur: 0.5f, dashSpd: 14f, proj: null, projSpd: 0f, vfx: anyaWave, color: VioletAnya,
                vfxLife: 0.50f, rest: RestorationEffect.SoundIntangibility);

            // 4. Actualizar prefabs de monstruos con HealthSystem, Tag "Enemy" y 30% drop de Orbe
            UpgradeMonsterPrefabs(orbPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Armanya] ¡VFX, 6 AbilityData (Aleek/Angus/Anya), Orbe y Prefabs de Monstruos actualizados!");
        }

        [MenuItem("Armanya/8. Conectar FirePoint, J/K y HealthSystem en Escena Actual")]
        public static void WireSceneHeroesAndMonsters()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var angusBasic = AssetDatabase.LoadAssetAtPath<AbilityData>($"{AbilityFolder}/Angus_Basic.asset");
            var angusSecret = AssetDatabase.LoadAssetAtPath<AbilityData>($"{AbilityFolder}/Angus_Secret.asset");
            var anyaBasic = AssetDatabase.LoadAssetAtPath<AbilityData>($"{AbilityFolder}/Anya_Basic.asset");
            var anyaSecret = AssetDatabase.LoadAssetAtPath<AbilityData>($"{AbilityFolder}/Anya_Secret.asset");
            var aleekBasic = AssetDatabase.LoadAssetAtPath<AbilityData>($"{AbilityFolder}/Aleek_Basic.asset");
            var aleekSecret = AssetDatabase.LoadAssetAtPath<AbilityData>($"{AbilityFolder}/Aleek_Secret.asset");

            var users = Object.FindObjectsByType<AbilityUser>(FindObjectsInactive.Include);
            int wired = 0;

            foreach (var user in users)
            {
                var go = user.gameObject;

                // Crear o reutilizar FirePoint hijo frente al jugador
                Transform fp = go.transform.Find("FirePoint");
                if (fp == null)
                {
                    var fpGo = new GameObject("FirePoint");
                    Undo.RegisterCreatedObjectUndo(fpGo, "Crear FirePoint");
                    fpGo.transform.SetParent(go.transform, false);
                    fpGo.transform.localPosition = new Vector3(0.85f, 0.1f, 0f);
                    fp = fpGo.transform;
                }

                // Determinar héroe por nombre o perfil
                string n = go.name.ToLowerInvariant();
                string sn = scene.name.ToLowerInvariant();
                AbilityData basic = angusBasic, secret = angusSecret;

                if (n.Contains("anya") || (!n.Contains("angus") && !n.Contains("aleek") && (sn.Contains("distritocaos") || sn.Contains("level2"))))
                {
                    basic = anyaBasic; secret = anyaSecret;
                }
                else if (n.Contains("aleek") || (!n.Contains("angus") && !n.Contains("anya") && (sn.Contains("nexoorbital") || sn.Contains("level3"))))
                {
                    basic = aleekBasic; secret = aleekSecret;
                }

                var so = new SerializedObject(user);
                if (basic != null) so.FindProperty("basicAttack").objectReferenceValue = basic;
                if (secret != null) so.FindProperty("secretPower").objectReferenceValue = secret;
                so.FindProperty("attackPoint").objectReferenceValue = fp;
                so.ApplyModifiedProperties();

                // Asegurar HealthSystem en el jugador (sin destruir al morir ni soltar orbes)
                var hs = go.GetComponent<HealthSystem>();
                if (hs == null)
                {
                    hs = Undo.AddComponent<HealthSystem>(go);
                    var hso = new SerializedObject(hs);
                    hso.FindProperty("maxHealth").intValue = 100;
                    hso.FindProperty("currentHealth").intValue = 100;
                    hso.FindProperty("destroyOnDeath").boolValue = false;
                    hso.FindProperty("dropOrbOnDeath").boolValue = false;
                    hso.ApplyModifiedPropertiesWithoutUndo();
                }

                wired++;
            }

            if (wired > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Armanya] Conectados FirePoint, J/K y HealthSystem en {wired} héroe(s) de '{scene.name}'. Guarda (Ctrl+S).");
            }
            else
            {
                Debug.LogWarning("[Armanya] No se encontraron objetos con AbilityUser en la escena actual.");
            }
        }

        static GameObject CreateStabilityOrbPrefab(Sprite circleSprite)
        {
            string path = $"{PickupFolder}/StabilityOrb.prefab";
            var go = new GameObject("StabilityOrb");
            go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            sr.color = new Color(0.15f, 1f, 0.55f, 1f);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 8;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            go.AddComponent<StabilityOrb>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Projectile2D CreateProjectilePrefab(string path, string name, Sprite sprite, Color color, Vector3 scale)
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
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            AddSimpleParticles(go, color, 18);
            go.AddComponent<Projectile2D>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Projectile2D>();
        }

        static GameObject CreateAreaVfxPrefab(string path, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 12;

            AddSimpleParticles(go, color, 28);
            go.AddComponent<AbilityVFXEffect>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void AddSimpleParticles(GameObject parent, Color color, int burstCount)
        {
            var ps = parent.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 3.5f;
            main.startSize = 0.18f;
            main.startColor = color;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 13;
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        }

        static void ConfigureAbility(
            string path, string displayName, AbilityShape shape,
            float cooldown, float damage, float range, float height,
            float knockback, float stun, float dashDur, float dashSpd,
            Projectile2D proj, float projSpd, GameObject vfx, Color color,
            float vfxLife, RestorationEffect rest)
        {
            var data = AssetDatabase.LoadAssetAtPath<AbilityData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AbilityData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.abilityName = displayName;
            data.shape = shape;
            data.cooldown = cooldown;
            data.targetLayers = ~0;
            data.damage = damage;
            data.range = range;
            data.height = height;
            data.knockbackForce = knockback;
            data.stunDuration = stun;
            data.dashDuration = dashDur;
            data.dashSpeed = dashSpd;
            data.projectilePrefab = proj;
            data.projectileSpeed = projSpd;
            data.projectileLifetime = 2f;
            data.vfxPrefab = vfx;
            data.vfxColor = color;
            data.vfxLifetime = vfxLife;
            data.restoration = rest;
            data.effectDuration = 3f;

            EditorUtility.SetDirty(data);
        }

        static void UpgradeMonsterPrefabs(GameObject orbPrefab)
        {
            if (!Directory.Exists(MonstersFolder)) return;

            string[] prefabPaths = Directory.GetFiles(MonstersFolder, "*.prefab", SearchOption.TopDirectoryOnly);
            foreach (string rawPath in prefabPaths)
            {
                string path = rawPath.Replace("\\", "/");
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                root.tag = "Enemy";

                var dummy = root.GetComponent<TestDummy>();
                if (dummy != null) Object.DestroyImmediate(dummy);

                var hs = root.GetComponent<HealthSystem>();
                if (hs == null) hs = root.AddComponent<HealthSystem>();
                var so = new SerializedObject(hs);
                so.FindProperty("maxHealth").intValue = 80;
                so.FindProperty("currentHealth").intValue = 80;
                so.FindProperty("destroyOnDeath").boolValue = true;
                so.FindProperty("dropOrbOnDeath").boolValue = true;
                so.FindProperty("orbDropChance").floatValue = 0.30f;
                so.FindProperty("stabilityOrbPrefab").objectReferenceValue = orbPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
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
