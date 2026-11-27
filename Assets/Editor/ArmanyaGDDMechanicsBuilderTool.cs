#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Armanya.Abilities;
using Armanya.Enemies;
using Armanya.Gameplay;
using Armanya.Interactables;
using Armanya.Player;
using Armanya.CameraSystem;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Herramienta idempotente para completar las mecánicas exactas del GDD / Portafolio UTEQ
    /// reutilizando visuales de 2DGamekit (limpios en URP 2D) sin romper escenas ni scripts existentes.
    /// </summary>
    public static class ArmanyaGDDMechanicsBuilderTool
    {
        const string Level1Path = "Assets/Scenes/Biomes/Level1_GrietaArida.unity";
        const string Level2Path = "Assets/Scenes/Biomes/Level2_DistritoCaos.unity";
        const string Level3Path = "Assets/Scenes/Biomes/Level3_NexoOrbital.unity";
        const string Level4Path = "Assets/Scenes/Biomes/Level4_RuinasFinales.unity";

        const string SwitchPrefabPath = "Assets/2DGamekit/Prefabs/Interactables/SingleUseSwitch.prefab";
        const string ReusableSwitchPath = "Assets/2DGamekit/Prefabs/Interactables/ReusableSwitch.prefab";
        const string DestructableWallPath = "Assets/2DGamekit/Prefabs/Interactables/DestructableWall.prefab";
        const string DoorPrefabPath = "Assets/2DGamekit/Prefabs/Interactables/Door.prefab";
        const string AnkeaPrefabPath = "Assets/Prefabs/Monsters/Ankea_Boss.prefab";
        const string PortalPrefabPath = "Assets/Prefabs/Interactables/Armanya_DimensionPortal.prefab";

        [MenuItem("Armanya/GDD 100%/Instalar TODO (Niveles 1-3 + Tag-Team Nivel 4)", false, 200)]
        public static void InstallAllGDDMechanics()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string originalScene = SceneManager.GetActiveScene().path;

            InstallLevel1Mechanics();
            InstallLevel2Mechanics();
            InstallLevel3Mechanics();
            InstallPortalsInLevels123();
            InstallLevel4TagTeamAndBoss();

            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

            EditorUtility.DisplayDialog(
                "Armanya — GDD 100% Completado",
                "Se instalaron correctamente sin romper nada:\n\n" +
                "• Nivel 1 (Grieta Árida): Bomba de Agua (K de Angus), Barrera de Raíces y Zona de Viento.\n" +
                "• Nivel 2 (Distrito Caos): Candado Acústico (K de Anya) y Barricada de Fuego.\n" +
                "• Nivel 3 (Nexo Orbital): Consola Orbital (K de Aleek) y Compuerta Láser.\n" +
                "• Nivel 4 (Ruinas Finales): Tag-Team [1] Angus, [2] Anya, [3] Aleek + Ankea Boss + 3 Generadores.\n\n" +
                "Puedes ajustar sus posiciones exactas en cada escena si deseas moverlos.",
                "¡Excelente!"
            );
        }

        [MenuItem("Armanya/GDD 100%/1. Nivel 1 - Bomba de Agua, Raíces y Viento", false, 210)]
        public static void InstallLevel1Mechanics()
        {
            var scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);
            Vector3 anchor = GetPlayerAnchorPosition();

            // 1. Barrera de Raíces Secas (se desactiva al restaurar la bomba)
            GameObject roots = CreateOrUpdateVisualProp(
                "Barrera_RaicesSecas",
                DestructableWallPath,
                anchor + new Vector3(11f, 0.5f, 0f),
                new Color(0.55f, 0.35f, 0.18f, 1f),
                isBlockingCollider: true
            );

            // 2. Bomba de Agua restaurable con Rayo de Angus (K)
            GameObject pump = CreateOrUpdateRestorableSwitch(
                "BombaAgua_GrietaArida",
                SwitchPrefabPath,
                anchor + new Vector3(7.5f, 0f, 0f),
                RestorationEffect.LightningWeather,
                "¡Bomba de Agua Reactivada! Las raíces secas se despejan.",
                new Color(0.85f, 0.55f, 0.25f, 1f),
                new Color(0.25f, 0.9f, 1f, 1f),
                roots
            );

            // 3. Zona de Viento en la represa
            GameObject wind = GameObject.Find("ZonaViento_Represa");
            if (wind == null)
            {
                wind = new GameObject("ZonaViento_Represa");
                var box = wind.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(8f, 4.5f);
                wind.AddComponent<WindZone2D>();
            }
            wind.transform.position = anchor + new Vector3(16f, 1.5f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Armanya GDD] Nivel 1 (Grieta Árida) actualizado con Bomba de Agua, Raíces y Viento.");
        }

        [MenuItem("Armanya/GDD 100%/2. Nivel 2 - Candado Acústico y Barricada", false, 211)]
        public static void InstallLevel2Mechanics()
        {
            var scene = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);
            Vector3 anchor = GetPlayerAnchorPosition();

            // 1. Barricada de fuego / reja urbana
            GameObject barricade = CreateOrUpdateVisualProp(
                "Barricada_DistritoCaos",
                DoorPrefabPath,
                anchor + new Vector3(11f, 0.5f, 0f),
                new Color(1f, 0.35f, 0.15f, 1f),
                isBlockingCollider: true
            );

            // 2. Candado Acústico restaurable con Grito Ultrasónico de Anya (K)
            CreateOrUpdateRestorableSwitch(
                "CandadoAcustico_Alcaldia",
                ReusableSwitchPath,
                anchor + new Vector3(7.5f, 0f, 0f),
                RestorationEffect.SoundIntangibility,
                "¡Barricada Apaciguada! Paso hacia la alcaldía despejado.",
                new Color(0.6f, 0.2f, 0.7f, 1f),
                new Color(0.85f, 0.45f, 1f, 1f),
                barricade
            );

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Armanya GDD] Nivel 2 (Distrito Caos) actualizado con Candado Acústico y Barricada.");
        }

        [MenuItem("Armanya/GDD 100%/3. Nivel 3 - Consola Orbital y Compuerta Láser", false, 212)]
        public static void InstallLevel3Mechanics()
        {
            var scene = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Single);
            Vector3 anchor = GetPlayerAnchorPosition();

            // 1. Compuerta Láser
            GameObject laserGate = CreateOrUpdateVisualProp(
                "CompuertaLaser_Nexo",
                DoorPrefabPath,
                anchor + new Vector3(11f, 0.5f, 0f),
                new Color(1f, 0.15f, 0.25f, 1f),
                isBlockingCollider: true
            );

            // 2. Consola de Seguridad hackeable con Telequinesis de Aleek (K)
            CreateOrUpdateRestorableSwitch(
                "ConsolaSeguridad_Nexo",
                SwitchPrefabPath,
                anchor + new Vector3(7.5f, 0f, 0f),
                RestorationEffect.Telekinesis,
                "¡Consola Orbital Hackeada! Compuerta láser desactivada.",
                new Color(0.2f, 0.45f, 0.75f, 1f),
                new Color(0.15f, 1f, 0.95f, 1f),
                laserGate
            );

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Armanya GDD] Nivel 3 (Nexo Orbital) actualizado con Consola Orbital y Compuerta Láser.");
        }

        [MenuItem("Armanya/GDD 100%/4. Nivel 4 - Configurar Tag-Team (1,2,3) y Ankea Boss", false, 213)]
        public static void InstallLevel4TagTeamAndBoss()
        {
            var scene = EditorSceneManager.OpenScene(Level4Path, OpenSceneMode.Single);

            // 1. Encontrar jugador base existente en la escena para clonar su configuración exacta (física, colliders, puente de vida)
            var basePlayer = Object.FindFirstObjectByType<PlayerController2D>(FindObjectsInactive.Include);
            if (basePlayer == null)
            {
                Debug.LogError("[Armanya GDD] No se encontró ningún PlayerController2D base en Level4_RuinasFinales.");
                return;
            }

            Vector3 spawnPos = basePlayer.transform.position;

            GameObject angus = EnsureHeroClone(basePlayer.gameObject, "Hero_Angus", spawnPos,
                "Assets/Art/Sprites/Players/Angus_Side.png",
                "Assets/Scripts/Angus_Basic.asset",
                "Assets/Scripts/Angus_Secret.asset");

            GameObject anya = EnsureHeroClone(basePlayer.gameObject, "Hero_Anya", spawnPos,
                "Assets/Art/Sprites/Players/Anya_Side.png",
                "Assets/Scripts/Anya_Basic.asset",
                "Assets/Scripts/Anya_Secret.asset");

            GameObject aleek = EnsureHeroClone(basePlayer.gameObject, "Hero_Aleek", spawnPos,
                "Assets/Art/Sprites/Players/Aleek_Side.png",
                "Assets/Scripts/Aleek_Basic.asset",
                "Assets/Scripts/Aleek_Secret.asset");

            // Si el basePlayer no es uno de los 3 héroes nombrados, lo desactivamos para evitar jugador duplicado
            if (basePlayer.gameObject != angus && basePlayer.gameObject != anya && basePlayer.gameObject != aleek)
            {
                basePlayer.gameObject.SetActive(false);
            }

            // 2. Configurar TagTeamManager
            var tagManager = Object.FindFirstObjectByType<TagTeamManager>(FindObjectsInactive.Include);
            if (tagManager == null)
            {
                var go = new GameObject("TagTeamManager");
                tagManager = go.AddComponent<TagTeamManager>();
            }

            var camFollow = Object.FindFirstObjectByType<CameraFollow2D>(FindObjectsInactive.Include);
            var so = new SerializedObject(tagManager);
            var heroesProp = so.FindProperty("heroes");
            heroesProp.arraySize = 3;

            AssignHeroSlot(heroesProp.GetArrayElementAtIndex(0), "Angus", angus);
            AssignHeroSlot(heroesProp.GetArrayElementAtIndex(1), "Anya", anya);
            AssignHeroSlot(heroesProp.GetArrayElementAtIndex(2), "Aleek", aleek);

            var camProp = so.FindProperty("cameraFollow");
            if (camProp != null && camFollow != null) camProp.objectReferenceValue = camFollow;

            var startProp = so.FindProperty("startingHeroIndex");
            if (startProp != null) startProp.intValue = 0;

            so.ApplyModifiedPropertiesWithoutUndo();

            // 3. Asegurar Ankea_Boss en la escena y conectarla a FractureGeneratorManager
            var genManager = Object.FindFirstObjectByType<FractureGeneratorManager>(FindObjectsInactive.Include);
            var ankea = Object.FindFirstObjectByType<AnkeaBossAI>(FindObjectsInactive.Include);
            if (ankea == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnkeaPrefabPath);
                if (prefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.name = "Ankea_Boss";
                    instance.transform.position = spawnPos + new Vector3(10f, 3.5f, 0f);
                    ankea = instance.GetComponent<AnkeaBossAI>();
                }
            }

            if (ankea != null && genManager != null)
            {
                var ankeaSO = new SerializedObject(ankea);
                var gmProp = ankeaSO.FindProperty("generatorManager");
                if (gmProp != null) gmProp.objectReferenceValue = genManager;
                ankeaSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // 4. Asegurar materiales visibles URP 2D en los Generadores de Fractura
            var defaultSpriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            var gens = Object.FindObjectsByType<FractureGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var g in gens)
            {
                var sr = g.GetComponent<SpriteRenderer>();
                if (sr != null && defaultSpriteMat != null) sr.sharedMaterial = defaultSpriteMat;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Armanya GDD] Nivel 4 (Ruinas Finales) configurado con Tag-Team [1] Angus, [2] Anya, [3] Aleek y Ankea Boss.");
        }

        [MenuItem("Armanya/GDD 100%/5. Colocar Portal Dimensional en Niveles 1, 2 y 3", false, 214)]
        public static void InstallPortalsInLevels123()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Modo Play activo", "Detén el botón Play antes de colocar los portales en las escenas.", "Entendido");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string originalScene = SceneManager.GetActiveScene().path;

            Vector3 p1 = EnsurePortalInScene(Level1Path, "Level2_DistritoCaos", LoreClueId.BitacoraHidrica);
            Vector3 p2 = EnsurePortalInScene(Level2Path, "Level3_NexoOrbital", LoreClueId.ManifiestoRoto);
            Vector3 p3 = EnsurePortalInScene(Level3Path, "Level4_RuinasFinales", LoreClueId.ConsolaCentral);

            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

            var currentPortal = Object.FindFirstObjectByType<ArmanyaPortalController>(FindObjectsInactive.Include);
            if (currentPortal != null)
            {
                Selection.activeGameObject = currentPortal.gameObject;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorUtility.DisplayDialog(
                "Portales Dimensionales Colocados",
                "Se colocaron y guardaron los portales visibles en:\n\n" +
                $"• Nivel 1 (Grieta Árida): X={p1.x:F1}, Y={p1.y:F1} -> Nivel 2\n" +
                $"• Nivel 2 (Distrito Caos): X={p2.x:F1}, Y={p2.y:F1} -> Nivel 3\n" +
                $"• Nivel 3 (Nexo Orbital): X={p3.x:F1}, Y={p3.y:F1} -> Nivel 4\n\n" +
                "El portal de tu escena actual quedó seleccionado en la Jerarquía.",
                "¡Listo!"
            );
        }

        [MenuItem("Armanya/GDD 100%/Colocar Portal AQUÍ (Escena Actual)", false, 215)]
        public static void PlacePortalInCurrentScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Modo Play activo", "Detén el botón Play para colocar y guardar el portal.", "Entendido");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            string sn = scene.name.ToLowerInvariant();
            string nextScene = "Level2_DistritoCaos";
            LoreClueId clue = LoreClueId.BitacoraHidrica;

            if (sn.Contains("caos") || sn.Contains("level2"))
            {
                nextScene = "Level3_NexoOrbital";
                clue = LoreClueId.ManifiestoRoto;
            }
            else if (sn.Contains("nexo") || sn.Contains("level3"))
            {
                nextScene = "Level4_RuinasFinales";
                clue = LoreClueId.ConsolaCentral;
            }

            Vector3 pos = EnsurePortalInOpenScene(scene, nextScene, clue);
            var currentPortal = Object.FindFirstObjectByType<ArmanyaPortalController>(FindObjectsInactive.Include);
            if (currentPortal != null)
            {
                Selection.activeGameObject = currentPortal.gameObject;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorUtility.DisplayDialog(
                "Portal Creado en Escena Actual",
                $"Se colocó 'Armanya_DimensionPortal' en ({pos.x:F1}, {pos.y:F1}) apuntando a {nextScene}.\nYa quedó seleccionado para que puedas moverlo a donde quieras.",
                "¡Genial!"
            );
        }

        [MenuItem("Armanya/GDD 100%/Desbloquear Puertas Viejas 2DGamekit (Escena Actual)", false, 216)]
        public static void UnlockLegacy2DGamekitDoors()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Modo Play activo", "Detén el botón Play antes de desbloquear puertas.", "Entendido");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            int disabledCount = 0;

            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null || t.gameObject == null) continue;
                string n = t.name;

                // No tocar nuestra compuerta oficial ni nuestro portal ni el jugador
                if (n.Contains("CompuertaLaser") || n.Contains("Barricada_") || n.Contains("Barrera_") || n.Contains("Armanya_"))
                    continue;

                bool isLegacyBlocker =
                    n.StartsWith("Door") ||
                    n == "Tilemap_WaterBlockers" ||
                    n.StartsWith("WallTile") ||
                    n.Contains("AproachingWall");

                if (isLegacyBlocker && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    disabledCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog(
                "Puertas Viejas Desbloqueadas",
                $"Se desactivaron {disabledCount} bloqueos/puertas heredadas de 2DGamekit en '{scene.name}'.\n\n" +
                "Tu compuerta oficial de Armanya (CompuertaLaser_Nexo) y tus plataformas siguen intactas.",
                "¡Perfecto!"
            );
        }

        static Vector3 EnsurePortalInScene(string scenePath, string nextScene, LoreClueId clueId)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return EnsurePortalInOpenScene(scene, nextScene, clueId);
        }

        static Vector3 EnsurePortalInOpenScene(Scene scene, string nextScene, LoreClueId clueId)
        {
            var existing = Object.FindFirstObjectByType<ArmanyaPortalController>(FindObjectsInactive.Include);
            GameObject portalObj = existing != null ? existing.gameObject : null;

            if (portalObj == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
                GameObject pilar = GameObject.Find("Pilar_Dimensional");
                Vector3 pos = pilar != null ? pilar.transform.position : GetPlayerAnchorPosition() + new Vector3(12f, 0f, 0f);

                if (prefab != null)
                {
                    portalObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    portalObj.name = "Armanya_DimensionPortal";
                    portalObj.transform.position = pos;
                }
                else
                {
                    // Fallback seguro por si el prefab no existiera
                    portalObj = new GameObject("Armanya_DimensionPortal");
                    portalObj.transform.position = pos;
                    var sr = portalObj.AddComponent<SpriteRenderer>();
                    sr.sprite = ExtractFirstSpriteFromPrefab(DoorPrefabPath);
                    sr.color = new Color(0.25f, 0.95f, 1f, 1f);
                    sr.sortingOrder = 8;
                    var col = portalObj.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.size = new Vector2(2.4f, 3.2f);
                    portalObj.AddComponent<ArmanyaPortalController>();
                }
            }

            // Asegurar material visible URP 2D
            var defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            foreach (var sr in portalObj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (defaultMat != null) sr.sharedMaterial = defaultMat;
            }

            var ctrl = portalObj.GetComponent<ArmanyaPortalController>();
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                so.FindProperty("dimensionClue").enumValueIndex = (int)clueId;
                so.FindProperty("nextSceneName").stringValue = nextScene;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return portalObj.transform.position;
        }

        static GameObject EnsureHeroClone(GameObject template, string heroObjName, Vector3 pos, string spritePath, string basicPath, string secretPath)
        {
            GameObject existing = GameObject.Find(heroObjName);
            GameObject hero = existing != null ? existing : Object.Instantiate(template);
            hero.name = heroObjName;
            hero.transform.position = pos;
            hero.SetActive(true);

            // Sprite
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            var sr = hero.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (sp != null) sr.sprite = sp;
                var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                if (mat != null) sr.sharedMaterial = mat;
            }

            // Habilidades ScriptableObject
            var abilityUser = hero.GetComponent<AbilityUser>();
            if (abilityUser != null)
            {
                var basic = AssetDatabase.LoadAssetAtPath<AbilityData>(basicPath);
                var secret = AssetDatabase.LoadAssetAtPath<AbilityData>(secretPath);
                var so = new SerializedObject(abilityUser);
                if (basic != null) so.FindProperty("basicAttack").objectReferenceValue = basic;
                if (secret != null) so.FindProperty("secretPower").objectReferenceValue = secret;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return hero;
        }

        static void AssignHeroSlot(SerializedProperty slot, string name, GameObject hero)
        {
            slot.FindPropertyRelative("heroName").stringValue = name;
            slot.FindPropertyRelative("hero").objectReferenceValue = hero;
        }

        static Vector3 GetPlayerAnchorPosition()
        {
            var pc = Object.FindFirstObjectByType<PlayerController2D>(FindObjectsInactive.Include);
            return pc != null ? pc.transform.position : Vector3.zero;
        }

        static GameObject CreateOrUpdateVisualProp(string objName, string prefabPath, Vector3 position, Color tint, bool isBlockingCollider)
        {
            GameObject existing = GameObject.Find(objName);
            if (existing != null) return existing;

            GameObject go = new GameObject(objName);
            go.transform.position = position;

            Sprite extractedSprite = ExtractFirstSpriteFromPrefab(prefabPath);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = extractedSprite;
            sr.color = tint;
            sr.sortingOrder = 5;
            var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (mat != null) sr.sharedMaterial = mat;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = !isBlockingCollider;
            if (extractedSprite != null) col.size = extractedSprite.bounds.size;
            else col.size = new Vector2(1.5f, 3.0f);

            return go;
        }

        static GameObject CreateOrUpdateRestorableSwitch(
            string objName,
            string prefabPath,
            Vector3 position,
            RestorationEffect effect,
            string message,
            Color damagedColor,
            Color restoredColor,
            GameObject targetToDisable)
        {
            GameObject go = GameObject.Find(objName);
            if (go == null)
            {
                go = new GameObject(objName);
                go.transform.position = position;

                Sprite sp = ExtractFirstSpriteFromPrefab(prefabPath);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = 6;
                var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                if (mat != null) sr.sharedMaterial = mat;

                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = sp != null ? sp.bounds.size * 1.4f : new Vector2(2f, 2f);
            }

            var restorable = go.GetComponent<RestorableObject>();
            if (restorable == null) restorable = go.AddComponent<RestorableObject>();

            var so = new SerializedObject(restorable);
            so.FindProperty("requiredEffect").enumValueIndex = (int)effect;
            so.FindProperty("restorationMessage").stringValue = message;
            so.FindProperty("damagedColor").colorValue = damagedColor;
            so.FindProperty("restoredColor").colorValue = restoredColor;
            so.FindProperty("targetRenderer").objectReferenceValue = go.GetComponent<SpriteRenderer>();

            if (targetToDisable != null)
            {
                var disableArr = so.FindProperty("disableOnRestore");
                disableArr.arraySize = 1;
                disableArr.GetArrayElementAtIndex(0).objectReferenceValue = targetToDisable;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static Sprite ExtractFirstSpriteFromPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return null;
            var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }
    }
}
#endif
