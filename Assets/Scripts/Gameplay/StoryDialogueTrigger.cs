using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Armanya.Gameplay
{
    [Serializable]
    public class DialogueLine
    {
        public string speakerName = "Angus";
        public Sprite speakerPortrait;
        [TextArea(2, 5)]
        public string message = "";
    }

    /// <summary>
    /// Cuadro de diálogo narrativo para dar contexto de la historia al iniciar el nivel o al pisar un punto clave.
    /// Presiona Espacio o Enter para avanzar/cerrar.
    /// </summary>
    public class StoryDialogueTrigger : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] bool showAtLevelStart = true;
        [SerializeField] float startDelay = 0.5f;
        [SerializeField] bool pausePlayerMovement = false;

        [Header("Diálogos")]
        [SerializeField] DialogueLine[] lines;

        int currentLineIndex = -1;
        bool isShowing;
        Texture2D cachedPortraitTex;

        void Start()
        {
            if (showAtLevelStart && lines != null && lines.Length > 0)
                StartCoroutine(ShowDelayed());
        }

        IEnumerator ShowDelayed()
        {
            yield return new WaitForSeconds(startDelay);
            StartDialogue();
        }

        public void StartDialogue()
        {
            if (lines == null || lines.Length == 0) return;
            currentLineIndex = 0;
            isShowing = true;
            UpdatePortraitTexture();
        }

        void Update()
        {
            if (!isShowing) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            bool advance = kb.spaceKey.wasPressedThisFrame ||
                           kb.enterKey.wasPressedThisFrame ||
                           kb.eKey.wasPressedThisFrame ||
                           kb.escapeKey.wasPressedThisFrame ||
                           (mouse != null && mouse.leftButton.wasPressedThisFrame);

            if (kb.escapeKey.wasPressedThisFrame)
            {
                isShowing = false;
                return;
            }

            if (advance)
            {
                currentLineIndex++;
                if (currentLineIndex >= lines.Length)
                {
                    isShowing = false;
                }
                else
                {
                    UpdatePortraitTexture();
                }
            }
        }

        void UpdatePortraitTexture()
        {
            if (currentLineIndex < 0 || currentLineIndex >= lines.Length) return;
            var sprite = lines[currentLineIndex].speakerPortrait;
            cachedPortraitTex = sprite != null ? sprite.texture : null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (isShowing) return;
            if (other.CompareTag("Player") || other.GetComponent<Player.PlayerController2D>() != null)
            {
                StartDialogue();
            }
        }

        void OnGUI()
        {
            if (!isShowing || currentLineIndex < 0 || currentLineIndex >= lines.Length) return;

            var line = lines[currentLineIndex];

            // Cuadro inferior estilo 2D Game Kit
            float boxWidth = Mathf.Min(820f, Screen.width - 40f);
            float boxHeight = 160f;
            float boxX = (Screen.width - boxWidth) * 0.5f;
            float boxY = Screen.height - boxHeight - 25f;

            // Fondo gris oscuro translúcido
            var prevColor = GUI.color;
            GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.92f);
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), GUIContent.none);
            GUI.color = prevColor;

            // Borde superior
            GUI.Box(new Rect(boxX, boxY, boxWidth, 26f), line.speakerName.ToUpper());

            // Retrato del personaje a la izquierda
            float portraitSize = 100f;
            float portraitX = boxX + 18f;
            float portraitY = boxY + 38f;

            if (cachedPortraitTex != null)
            {
                GUI.DrawTexture(new Rect(portraitX, portraitY, portraitSize, portraitSize), cachedPortraitTex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Box(new Rect(portraitX, portraitY, portraitSize, portraitSize), line.speakerName);
            }

            // Texto de historia a la derecha
            float textX = portraitX + portraitSize + 18f;
            float textY = boxY + 36f;
            float textW = boxWidth - (textX - boxX) - 20f;
            float textH = boxHeight - 65f;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(textX, textY, textW, textH), line.message, style);

            // Indicador de avance abajo
            var hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = new Color(0.9f, 0.85f, 0.4f, 0.85f) }
            };
            GUI.Label(new Rect(boxX, boxY + boxHeight - 24f, boxWidth - 16f, 20f), "Pulsa [ESPACIO] o [ENTER] para continuar ▶", hintStyle);
        }
    }
}
