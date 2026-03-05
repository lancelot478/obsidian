using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
    public class RoleObjectBindEditor
    {
        private static string fxAssetPath = "Assets/Prefabs/SkillEffect/Accessory/";
        private struct unityTransform
        {
            public string ParentName;
            public string AssetPath;
            public Vector3 Pos;
            public Vector3 EulerAngles;
            public Vector3 LocalScale;
        }
        //guid unityTransform
        private static Dictionary<string, unityTransform> fxGUIDDict = new Dictionary<string, unityTransform>();

        public static void CheckRoleObjBindBefore(string pfbPath)
        {
            fxGUIDDict.Clear();
            var unityObj = AssetDatabase.LoadAssetAtPath<GameObject>(pfbPath);
            if (unityObj)
            {
                var trans = unityObj.transform.GetComponentsInChildren<Transform>();
                foreach (var tra in trans)
                {
                    if (!tra.gameObject.name.Contains("SubFX_")&&tra.gameObject.name.Contains("FX_")  )
                    {
                        // string GUID;
                        // long fileID;
                        // var find = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tra.gameObject, out GUID, out fileID);
                        //
                        // var assetPath = AssetDatabase.GUIDToAssetPath(GUID);
                        string fxPath = fxAssetPath + tra.gameObject.name + ".prefab";
                        if (AssetDatabase.LoadAssetAtPath<GameObject>(fxPath) != null)
                        {
                            string parentName = string.Empty;
                            if (tra.parent)
                            {
                                parentName = tra.parent.gameObject.name;
                            }

                            var add = fxGUIDDict.TryAdd(fxPath, new unityTransform()
                            {
                                ParentName = parentName,
                                AssetPath = fxPath,
                                Pos = tra.localPosition,
                                EulerAngles = tra.localEulerAngles,
                                LocalScale = tra.localScale
                            });
                            if (add)
                                Debug.Log(
                                    $"记录VFX {fxPath} {tra.gameObject.name} {tra.parent.gameObject.name}");
                        }
                    }
                }
            }
        }

        public static void CheckRoleObjBindAfter(UnityEngine.GameObject unityObj)
        {
            if (unityObj)
            {
                foreach (var pair in fxGUIDDict)
                {
                    //挂载根节点
                    if (pair.Value.ParentName == string.Empty)
                    {
                        var objAsset = AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value.AssetPath);
                  var fxObj = GameObject.Instantiate(objAsset);
                                         fxObj.transform.SetParent(unityObj.transform);
                        fxObj.transform.localPosition = pair.Value.Pos;
                        fxObj.transform.localEulerAngles = pair.Value.EulerAngles;
                        fxObj.transform.localScale = pair.Value.LocalScale;
                    }
                }

                var trans = unityObj.transform.GetComponentsInChildren<Transform>();
                foreach (var tra in trans)
                {
                    foreach (var pair in fxGUIDDict)
                    {
                        if (pair.Value.ParentName != string.Empty && tra.gameObject.name == pair.Value.ParentName)
                        {
                            
                            var objAsset = AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value.AssetPath);
                            Debug.Log(objAsset+"  "+pair.Value.AssetPath);
                            var fxObj = GameObject.Instantiate(objAsset);
                            fxObj.transform.SetParent(tra.transform);
                            fxObj.transform.localPosition = pair.Value.Pos;
                            fxObj.transform.localEulerAngles = pair.Value.EulerAngles;
                            fxObj.transform.localScale = pair.Value.LocalScale;
                            fxObj.name = fxObj.name.Replace("(Clone)", string.Empty);
                        }
                    }
                }
            }

            fxGUIDDict.Clear();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
