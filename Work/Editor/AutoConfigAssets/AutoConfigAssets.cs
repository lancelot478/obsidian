using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace SAGA.Editor
{
    public static  partial class AutoConfigAssets
    {
        [MenuItem("Tools/AutoConfigAssets")]
        public static void Execute()
        {
            try
            {
                var prefabs = GetAllPrefab();
                for (var idx = 0; idx < prefabs.Count; idx++)
                {
                    var prefab = prefabs[idx];

                    var progress = (idx + 1.0f) / prefabs.Count;
                    var cancel = EditorHelper.ShowProgressBar($"[{idx + 1} - {prefabs.Count}] {prefab.name}", progress);
                    if (cancel)
                    {
                        break;
                    }

                    // 粒子系统
                    HandleParticleSystem(prefab);
                    // SkinnedMeshRenderer
                    HandleSkinnedMeshRenderer(prefab);

                    // 保存
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void HandleParticleSystem(GameObject prefab)
        {
            var allParticles = prefab.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particle in allParticles)
            {
                var main = particle.main;
                main.prewarm = false;
            }
        }

        private static void HandleSkinnedMeshRenderer(GameObject prefab)
        {
            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                skinnedMeshRenderer.skinnedMotionVectors = false;
            }
        }

        private static List<GameObject> GetAllPrefab(params string[] path)
        {
            var prefabList = new List<GameObject>();
            var guids = AssetDatabase.FindAssets("t:Prefab", path);
            foreach (var guid in guids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
                prefabList.Add(prefab);
            }

            return prefabList;
        }
    }
}