#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PooterAnimatorBuilder
{
    private const string ControllerPath = "Assets/Assets In Use/Animations/Pooter/PooterAnim.controller";
    private const string FlyClipPath = "Assets/Assets In Use/Animations/Pooter/PooterFly.anim";
    private const string AttackClipPath = "Assets/Assets In Use/Animations/Pooter/PooterAttack.anim";

    [MenuItem("Tools/Pooter/Rebuild Animator Controller")]
    public static void Rebuild()
    {
        var flyClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FlyClipPath);
        var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPath);
        if (flyClip == null || attackClip == null)
        {
            Debug.LogError("[PooterAnimatorBuilder] Pooter anim clipleri bulunamadi.");
            return;
        }

        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var sm = controller.layers[0].stateMachine;

        controller.AddParameter("isAttacking", AnimatorControllerParameterType.Bool);

        var fly = sm.AddState("PooterFly", new Vector3(260f, 40f, 0f));
        fly.motion = flyClip;

        var attack = sm.AddState("PooterAttack", new Vector3(260f, 200f, 0f));
        attack.motion = attackClip;

        sm.defaultState = fly;

        var toAttack = fly.AddTransition(attack);
        toAttack.hasExitTime = false;
        toAttack.hasFixedDuration = true;
        toAttack.duration = 0f;
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, "isAttacking");

        var toFly = attack.AddTransition(fly);
        toFly.hasExitTime = false;
        toFly.hasFixedDuration = true;
        toFly.duration = 0f;
        toFly.AddCondition(AnimatorConditionMode.IfNot, 0f, "isAttacking");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PooterAnimatorBuilder] PooterAnim.controller yeniden olusturuldu.");
    }
}
#endif
