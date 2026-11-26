#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Unity.Cinemachine;
using Armanya.CameraSystem;
using Armanya.Player;

namespace Armanya.EditorTools
{
    /// <summary>
    /// Construye y asigna el pulido de Criterio DE:
    ///   - Animator Controller con Blend Tree 1D (Speed: Idle -> Walk -> Run) sin reemplazar los sprites custom.
    ///   - PlayerAnimatorBridge en cada héroe.
    ///   - CinemachineImpulseSource en la Main Camera para Game Feel (Camera Shake).
    /// </summary>
    public static class ArmanyaPolishBuilderTool
    {
        const string AnimFolder = "Assets/Art/Animations";
        const string ControllerPath = "Assets/Art/Animations/ArmanyaHero_BlendTree.controller";

        public static RuntimeAnimatorController EnsureHeroBlendTreeController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(AnimFolder);

            // 1. Crear clips ligeros de escala/respiración (no alteran SpriteRenderer.sprite)
            var idleClip = CreateScaleLoopClip($"{AnimFolder}/Hero_Idle.anim", 1.2f, 1.00f, 1.03f, 1.00f, 0.98f);
            var walkClip = CreateScaleLoopClip($"{AnimFolder}/Hero_Walk.anim", 0.55f, 1.03f, 0.96f, 0.97f, 1.04f);
            var runClip  = CreateScaleLoopClip($"{AnimFolder}/Hero_Run.anim",  0.34f, 1.07f, 0.92f, 0.94f, 1.08f);

            // 2. Crear AnimatorController con parámetros de movimiento
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("SecretPower", AnimatorControllerParameterType.Trigger);

            // 3. Crear Blend Tree 1D controlado por "Speed"
            AnimatorState blendState = controller.CreateBlendTreeInController("Locomotion_BlendTree", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;

            tree.AddChild(idleClip, 0.0f);
            tree.AddChild(walkClip, 2.0f);
            tree.AddChild(runClip,  6.0f);

            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.defaultState = blendState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Armanya] Blend Tree de animación creado en '{ControllerPath}' (Idle 0.0 -> Walk 2.0 -> Run 6.0).");
            return controller;
        }

        static AnimationClip CreateScaleLoopClip(string path, float duration, float sxA, float syA, float sxB, float syB)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;

            clip = new AnimationClip { frameRate = 30f };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float half = duration * 0.5f;
            var curveY = new AnimationCurve(
                new Keyframe(0f, syA),
                new Keyframe(half, syB),
                new Keyframe(duration, syA));

            // Solo animamos la escala vertical (localScale.y) para NO interferir con el flip horizontal (localScale.x) de PlayerController2D
            clip.SetCurve("", typeof(Transform), "localScale.y", curveY);

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        /// <summary>Asigna Animator + Blend Tree + PlayerAnimatorBridge a un héroe y CinemachineImpulseSource a la cámara.</summary>
        public static void WireHeroPolish(GameObject heroGo)
        {
            if (heroGo == null) return;

            var ctrl = EnsureHeroBlendTreeController();
            var anim = heroGo.GetComponent<Animator>();
            if (anim == null) anim = Undo.AddComponent<Animator>(heroGo);
            if (anim.runtimeAnimatorController == null && ctrl != null)
            {
                Undo.RecordObject(anim, "Asignar Blend Tree Controller");
                anim.runtimeAnimatorController = ctrl;
                EditorUtility.SetDirty(anim);
            }

            if (heroGo.GetComponent<PlayerAnimatorBridge>() == null)
            {
                Undo.AddComponent<PlayerAnimatorBridge>(heroGo);
            }

            // Asegurar CinemachineImpulseSource en la cámara para Camera Shake (Game Feel)
            var camFollow = Object.FindFirstObjectByType<CameraFollow2D>();
            if (camFollow != null && camFollow.GetComponent<CinemachineImpulseSource>() == null)
            {
                Undo.AddComponent<CinemachineImpulseSource>(camFollow.gameObject);
            }
        }
    }
}
#endif
