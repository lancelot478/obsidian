
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class CampAnimatorEditor : Editor {

    private static readonly string baseRoleAnimationPath = "Assets/Animations/Home/RoleAnim";

    private static readonly string modelsPath     = "Assets/Models";
    private static readonly string homeModelsPath = "Assets/Models/Home/Player";

    private static readonly string[] modelTypes = { "ModelHair", "ModelHead", "ModelRole" };
    private static readonly string[][] jobTypes   = {
        new[] {"Novice"}, 
        new[] {"Assassin", "Magician", "Ranger", "Reverend", "Swordman"}, 
        new[] {"Wanderer", "Mage", "Hunter", "Priest", "Berserker"},
        new[] {"ShadowWalker", "Wizzard", "Sniper", "Padre", "SwordMaster"}
    };
    private static readonly string   commonJob  = "Common";
    private static readonly string[] sexes      = { "M", "F" };

    private static readonly string animatorsAttachedPrefabPath = "Assets/Prefabs/HomeBuildMap/HomeAnimatorsAttachedToMap.prefab";
    private static readonly string animatorsAttachedPrefabChildPath = "Animators";

    [MenuItem("Assets/ProcessAssets/Camp/GenerateRoleAnim")]
    public static void GenerateHomeRoleAnimators() {
        AssetDatabase.Refresh();
        
        var baseAnimationControllerPath = Path.Combine(baseRoleAnimationPath, "_Base/anim_role_home.controller");
        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseAnimationControllerPath);
        
        // create attached prefab
        var attachedPrefab = PrefabUtility.LoadPrefabContents(animatorsAttachedPrefabPath);
        DestroyImmediate(attachedPrefab.transform.Find(animatorsAttachedPrefabChildPath)?.gameObject, true);
        
        var childObject = new GameObject(animatorsAttachedPrefabChildPath);
        childObject.transform.SetParent(attachedPrefab.transform);
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localScale = Vector3.one;
        childObject.transform.rotation = Quaternion.identity;

        foreach (var modelType in modelTypes) {
            var modelTypeGameObject = new GameObject(modelType);
            modelTypeGameObject.transform.SetParent(childObject.transform);
            modelTypeGameObject.transform.localPosition = Vector3.zero;
            modelTypeGameObject.transform.localScale = Vector3.one;
            modelTypeGameObject.transform.rotation = Quaternion.identity;

            var overrideControllerFolderPath = Path.Combine(baseRoleAnimationPath, modelType);

            var clipBasePath  = Path.Combine(homeModelsPath, modelType);
            var modelClipPath = Path.Combine(modelsPath, modelType);
            for (var evolve = 0; evolve < jobTypes.Length; evolve++) {
                var evoJobsType = jobTypes[evolve];
                
                for (int i = 0; i < evoJobsType.Length; i++) {
                    foreach (var sex in sexes) {
                        var jobName = evoJobsType[i] + "_" + sex;
                        if (modelType == "ModelHair") {
                            jobName = "Hair_" + jobName;
                        }

                        if (modelType == "ModelHead") {
                            jobName = jobName + "_Hat";
                        }

                        var overrideControllerPath =
                            Path.Combine(overrideControllerFolderPath, jobName + ".controller");
                        var overrideController =
                            AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideControllerPath);
                        bool needCreate = false;
                        if (overrideController == null) {
                            needCreate = true;

                            overrideController = new AnimatorOverrideController();
                        }

                        overrideController.runtimeAnimatorController = baseController;
                        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                        foreach (var baseClip in baseController.animationClips) {
                            // load from specific
                            var overrideClipPath = Path.Combine(clipBasePath, jobName, baseClip.name + ".anim");
                            var overrideClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(overrideClipPath);

                            // load from normal
                            if (overrideClip == null) {
                                overrideClipPath = Path.Combine(modelClipPath, jobName, baseClip.name + ".anim");
                                overrideClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(overrideClipPath);
                            }

                            // load from common
                            if (overrideClip == null) {
                                overrideClipPath = Path.Combine(
                                    clipBasePath,
                                    commonJob + "_" + sex,
                                    baseClip.name + ".anim"
                                );
                                overrideClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(overrideClipPath);
                            }

                            // load from previous evolve
                            if (overrideClip == null) {
                                var currentEvolve = evolve;
                                while (currentEvolve-- > 1) {
                                    var previousJobName = jobTypes[currentEvolve][i] + "_" + sex;
                                    overrideClipPath = Path.Combine(
                                        clipBasePath, 
                                        previousJobName,
                                        baseClip.name + ".anim"
                                    );
                                    overrideClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(overrideClipPath);
                                    if (overrideClip != null) {
                                        break;
                                    }
                                }
                            }

                            var pair = new KeyValuePair<AnimationClip, AnimationClip>(baseClip, overrideClip);
                            overrides.Add(pair);
                        }

                        overrideController.ApplyOverrides(overrides);
                        if (needCreate) {
                            AssetDatabase.CreateAsset(overrideController, overrideControllerPath);
                        }

                        var controllerGameObject = new GameObject(overrideController.name);
                        controllerGameObject.transform.SetParent(modelTypeGameObject.transform);
                        controllerGameObject.transform.localPosition = Vector3.zero;
                        controllerGameObject.transform.localScale = Vector3.one;
                        controllerGameObject.transform.rotation = Quaternion.identity;

                        var animator = controllerGameObject.AddComponent<Animator>();
                        animator.runtimeAnimatorController = overrideController;

                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
        
        PrefabUtility.SaveAsPrefabAsset(attachedPrefab, animatorsAttachedPrefabPath);
        DestroyImmediate(childObject);
        
        AssetDatabase.Refresh();
    }
}
