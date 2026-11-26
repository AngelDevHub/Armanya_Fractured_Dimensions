#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Armanya.Gameplay;
using Armanya.UI;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Crea automáticamente la escena Menu_Inicio con su Canvas, EventSystem (Input System nuevo),
    /// GameManager persistente, 3 tarjetas de héroes con retratos, botón de Portal de Convergencia
    /// y ordena las 5 escenas en File > Build Profiles > Scene List.
    /// </summary>
    public static class ArmanyaMenuBuilderTool
    {
        const string MenuScenePath = "Assets/Scenes/Menu_Inicio.unity";

        static readonly string[] BuildSceneOrder =
        {
            "Assets/Scenes/Menu_Inicio.unity",
            "Assets/Scenes/Biomes/Level1_GrietaArida.unity",
            "Assets/Scenes/Biomes/Level2_DistritoCaos.unity",
            "Assets/Scenes/Biomes/Level3_NexoOrbital.unity",
            "Assets/Scenes/Biomes/Level4_RuinasFinales.unity"
        };

        // [MenuItem("Armanya/5. Crear Escena Menu_Inicio y Ordenar Build Settings")]
        public static void BuildMenuSceneAndSettings()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Cámara de fondo oscuro
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.14f, 1f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 0, -10);

            // 2. EventSystem compatible con el Nuevo Input System
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // 3. GameManager persistente
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();

            // 4. Canvas principal (Scale With Screen Size para WebGL)
            var canvasGo = new GameObject("MenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Título
            CreateText(canvasGo.transform, "TitleText", "ARMANYA: FRACTURED DIMENSIONS", uiFont, 42, FontStyle.Bold,
                new Color(0.95f, 0.85f, 0.4f), new Vector2(0, 280), new Vector2(980, 70));

            // Subtítulo / Progreso global de pistas (0/3 a 3/3)
            var progressText = CreateText(canvasGo.transform, "LoreProgressText", "Pistas de Lore: 0 / 3", uiFont, 24, FontStyle.Bold,
                new Color(0.55f, 0.92f, 1f), new Vector2(0, 220), new Vector2(900, 42));

            // 3 Tarjetas de Selección de Personaje
            var angusCard = CreateHeroCard(canvasGo.transform, "Card_Angus", "ANGUS\nGrieta Árida",
                "Assets/Art/Sprites/Players/Angus_Portrait.png", new Color(0.75f, 0.45f, 0.15f),
                new Vector2(-340, 15), uiFont, out Text angusClueText);

            var anyaCard = CreateHeroCard(canvasGo.transform, "Card_Anya", "ANYA\nDistrito Caos",
                "Assets/Art/Sprites/Players/Anya_Portrait.png", new Color(0.45f, 0.2f, 0.65f),
                new Vector2(0, 15), uiFont, out Text anyaClueText);

            var aleekCard = CreateHeroCard(canvasGo.transform, "Card_Aleek", "ALEEK\nNexo Orbital",
                "Assets/Art/Sprites/Players/Aleek_Portrait.png", new Color(0.12f, 0.45f, 0.62f),
                new Vector2(340, 15), uiFont, out Text aleekClueText);

            // Botón Portal de Convergencia (Ruinas Finales)
            var portalBtn = CreateButton(canvasGo.transform, "Btn_PortalConvergencia",
                "PORTAL DE CONVERGENCIA BLOQUEADO\n(Recolecta las 3 Pistas: 0/3)",
                uiFont, 20, new Color(0.15f, 0.65f, 0.55f),
                new Vector2(0, -235), new Vector2(560, 76), out Text portalLabel);

            // Botón pequeño de reinicio para pruebas
            var resetBtn = CreateButton(canvasGo.transform, "Btn_ResetProgress", "Reiniciar Progreso (0/3)",
                uiFont, 14, new Color(0.35f, 0.18f, 0.22f),
                new Vector2(490, -310), new Vector2(210, 36), out _);

            // 5. Conectar MainMenuController
            var menuCtrl = canvasGo.AddComponent<MainMenuController>();
            var so = new SerializedObject(menuCtrl);
            so.FindProperty("angusButton").objectReferenceValue = angusCard;
            so.FindProperty("anyaButton").objectReferenceValue = anyaCard;
            so.FindProperty("aleekButton").objectReferenceValue = aleekCard;
            so.FindProperty("convergencePortalButton").objectReferenceValue = portalBtn;
            so.FindProperty("convergenceButtonLabel").objectReferenceValue = portalLabel;
            so.FindProperty("loreProgressText").objectReferenceValue = progressText;
            so.FindProperty("angusClueStatusText").objectReferenceValue = angusClueText;
            so.FindProperty("anyaClueStatusText").objectReferenceValue = anyaClueText;
            so.FindProperty("aleekClueStatusText").objectReferenceValue = aleekClueText;
            so.FindProperty("resetProgressButton").objectReferenceValue = resetBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Guardar escena Menu_Inicio
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            ConfigureBuildSettings();

            Debug.Log("[Armanya] ¡Escena 'Menu_Inicio' creada y Build Settings ordenado (0..4)!");
        }

        [MenuItem("Armanya/3. Actualizar Build Settings (Menú + 4 Niveles)")]
        public static void ConfigureBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (string path in BuildSceneOrder)
            {
                if (File.Exists(path))
                    list.Add(new EditorBuildSettingsScene(path, true));
                else
                    Debug.LogWarning($"[Armanya] No se encontró la escena '{path}' para Build Settings.");
            }
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[Armanya] Build Settings actualizado con {list.Count} escenas en orden.");
        }

        static Button CreateHeroCard(Transform parent, string name, string title, string portraitPath,
            Color panelColor, Vector2 pos, Font font, out Text clueStatusText)
        {
            var cardGo = new GameObject(name);
            cardGo.transform.SetParent(parent, false);
            var rt = cardGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(290, 340);

            var bg = cardGo.AddComponent<Image>();
            bg.color = panelColor;
            var btn = cardGo.AddComponent<Button>();

            // Retrato del personaje
            var imgGo = new GameObject("Portrait");
            imgGo.transform.SetParent(cardGo.transform, false);
            var imgRt = imgGo.AddComponent<RectTransform>();
            imgRt.anchoredPosition = new Vector2(0, 40);
            imgRt.sizeDelta = new Vector2(180, 180);
            var portraitImg = imgGo.AddComponent<Image>();
            portraitImg.preserveAspect = true;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            if (sprite != null) portraitImg.sprite = sprite;

            // Nombre y Bioma
            CreateText(cardGo.transform, "HeroLabel", title, font, 22, FontStyle.Bold,
                Color.white, new Vector2(0, -85), new Vector2(270, 62));

            // Estado de la pista de lore
            clueStatusText = CreateText(cardGo.transform, "ClueStatus", "[ ] Pista", font, 16, FontStyle.Normal,
                new Color(1f, 0.95f, 0.7f), new Vector2(0, -140), new Vector2(270, 32));

            return btn;
        }

        static Button CreateButton(Transform parent, string name, string label, Font font, int fontSize,
            Color color, Vector2 pos, Vector2 size, out Text labelText)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();

            labelText = CreateText(go.transform, "Label", label, font, fontSize, FontStyle.Bold,
                Color.white, Vector2.zero, size - new Vector2(16, 8));

            return btn;
        }

        static Text CreateText(Transform parent, string name, string content, Font font, int size,
            FontStyle style, Color color, Vector2 pos, Vector2 rectSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = rectSize;

            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            return txt;
        }
    }
}
#endif
