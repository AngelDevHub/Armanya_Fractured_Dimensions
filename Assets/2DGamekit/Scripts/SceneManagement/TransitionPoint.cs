using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gamekit2D
{
    [RequireComponent(typeof(Collider2D))]
    public class TransitionPoint : MonoBehaviour
    {
        public enum TransitionType
        {
            DifferentZone, DifferentNonGameplayScene, SameScene,
        }

        public enum TransitionWhen
        {
            ExternalCall, InteractPressed, OnTriggerEnter,
        }

        [Tooltip("This is the gameobject that will transition. For example, the player.")]
        public GameObject transitioningGameObject;
        [Tooltip("Whether the transition will be within this scene, to a different zone or a non-gameplay scene.")]
        public TransitionType transitionType;
        [SceneName]
        public string newSceneName;
        [Tooltip("The tag of the SceneTransitionDestination script in the scene being transitioned to.")]
        public SceneTransitionDestination.DestinationTag transitionDestinationTag;
        [Tooltip("The destination in this scene that the transitioning gameobject will be teleported.")]
        public TransitionPoint destinationTransform;
        [Tooltip("What should trigger the transition to start.")]
        public TransitionWhen transitionWhen;
        [Tooltip("The player will lose control when the transition happens.")]
        public bool resetInputValuesOnTransition = true;
        [Tooltip("Is this transition only possible with specific items in the inventory?")]
        public bool requiresInventoryCheck;
        [Tooltip("The inventory to be checked.")]
        public InventoryController inventoryController;
        [Tooltip("The required items.")]
        public InventoryController.InventoryChecker inventoryCheck;

        bool m_TransitioningGameObjectPresent;
        GameObject m_ActivePlayerInTrigger;

        void Start()
        {
            // Desactivar triggers heredados de 2DGamekit (ej. TransitionStartToZone2 en Nivel 3)
            // que regresan al jugador por accidente a un nivel anterior al caminar.
            // En Armanya los cambios de nivel se hacen con Armanya_DimensionPortal.
            if (transitionType != TransitionType.SameScene && transitionWhen == TransitionWhen.OnTriggerEnter)
            {
                string objName = gameObject.name.ToLowerInvariant();
                string rawDest = (newSceneName ?? "").ToLowerInvariant();
                if (objName.Contains("tozone") || rawDest.Contains("zone") ||
                    Object.FindFirstObjectByType<Armanya.Interactables.ArmanyaPortalController>() != null)
                {
                    Debug.Log($"[TransitionPoint] Trigger heredado '{gameObject.name}' desactivado para evitar regreso accidental de nivel.");
                    enabled = false;
                    var col = GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                    return;
                }
            }

            if (transitionWhen == TransitionWhen.ExternalCall)
                m_TransitioningGameObjectPresent = true;
        }

        bool IsPlayerCollider(Collider2D other, out GameObject playerRoot)
        {
            playerRoot = null;
            if (other == null) return false;

            if (transitioningGameObject != null && other.gameObject == transitioningGameObject)
            {
                playerRoot = transitioningGameObject;
                return true;
            }

            var pc = other.GetComponent<Armanya.Player.PlayerController2D>() ?? other.GetComponentInParent<Armanya.Player.PlayerController2D>();
            if (pc != null)
            {
                playerRoot = pc.gameObject;
                return true;
            }

            if (other.CompareTag("Player"))
            {
                playerRoot = other.gameObject;
                return true;
            }

            return false;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayerCollider(other, out GameObject playerRoot))
            {
                m_TransitioningGameObjectPresent = true;
                m_ActivePlayerInTrigger = playerRoot;

                if (ScreenFader.IsFading || SceneController.Transitioning)
                    return;

                if (transitionWhen == TransitionWhen.OnTriggerEnter)
                    TransitionInternal();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayerCollider(other, out _))
            {
                m_TransitioningGameObjectPresent = false;
                m_ActivePlayerInTrigger = null;
            }
        }

        void Update()
        {
            if (ScreenFader.IsFading || SceneController.Transitioning)
                return;

            if (!m_TransitioningGameObjectPresent)
                return;

            if (transitionWhen == TransitionWhen.InteractPressed)
            {
                bool pressedS = false;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    pressedS = kb.sKey.wasPressedThisFrame ||
                               kb.downArrowKey.wasPressedThisFrame ||
                               kb.eKey.wasPressedThisFrame;
                }

                if (!pressedS && PlayerInput.Instance != null && PlayerInput.Instance.Interact.Down)
                    pressedS = true;

                if (pressedS)
                {
                    TransitionInternal();
                }
            }
        }

        protected void TransitionInternal()
        {
            if (requiresInventoryCheck && inventoryController != null)
            {
                if (!inventoryCheck.CheckInventory(inventoryController))
                    return;
            }

            GameObject targetObj = m_ActivePlayerInTrigger != null ? m_ActivePlayerInTrigger : transitioningGameObject;

            if (transitionType == TransitionType.SameScene)
            {
                if (destinationTransform != null && targetObj != null)
                {
                    targetObj.transform.position = destinationTransform.transform.position + new Vector3(0f, 0.5f, 0f);
                    var rb = targetObj.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                    m_TransitioningGameObjectPresent = false;
                }
            }
            else
            {
                string mappedScene = MapToArmanyaScene(newSceneName);
                if (!string.IsNullOrEmpty(mappedScene) && Application.CanStreamedLevelBeLoaded(mappedScene))
                {
                    SceneManager.LoadScene(mappedScene);
                }
                else if (!string.IsNullOrEmpty(newSceneName) && Application.CanStreamedLevelBeLoaded(newSceneName))
                {
                    SceneManager.LoadScene(newSceneName);
                }
                else
                {
                    // Si la puerta apuntaba a una zona no incluida en Build Settings, ir al siguiente nivel en orden
                    int nextIdx = SceneManager.GetActiveScene().buildIndex + 1;
                    if (nextIdx < SceneManager.sceneCountInBuildSettings)
                        SceneManager.LoadScene(nextIdx);
                }
            }
        }

        string MapToArmanyaScene(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return null;
            string n = rawName.ToLowerInvariant();
            if (n.Contains("zone1")) return "Level1_GrietaArida";
            if (n.Contains("zone2") || n.Contains("zone3")) return "Level2_DistritoCaos";
            if (n.Contains("zone4")) return "Level3_NexoOrbital";
            if (n.Contains("zone5")) return "Level4_RuinasFinales";
            return rawName;
        }

        public void Transition()
        {
            if (!m_TransitioningGameObjectPresent)
                return;

            if (transitionWhen == TransitionWhen.ExternalCall)
                TransitionInternal();
        }

        void OnGUI()
        {
            if (!m_TransitioningGameObjectPresent || transitionWhen != TransitionWhen.InteractPressed) return;
            if (ScreenFader.IsFading) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.6f)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            if (screenPos.z < 0f) return;

            float w = 210f;
            float h = 30f;
            float x = Mathf.Clamp(screenPos.x - w * 0.5f, 10f, Screen.width - w - 10f);
            float y = Mathf.Clamp(Screen.height - screenPos.y - h, 10f, Screen.height - h - 10f);

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.95f, 0.45f, 1f) }
            };

            GUI.Box(new Rect(x, y, w, h), "Presiona [S] para cruzar", style);
        }
    }
}
