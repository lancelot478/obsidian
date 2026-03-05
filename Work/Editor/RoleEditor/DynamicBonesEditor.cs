using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


public class DynamicBonesEditor
{
    private static string mainBone;
    private static Dictionary<string, DynamicBoneCollider> boneColls;
    private static List<string> roots;
    private static List<string> exclusions;
    private static DynamicBone dynamicBone;
    private static string rootBone;

    private static GameObject tmpObj;

    public static void CheckCreateObjDynamicBonesBefore(UnityEngine.Object objSrc, string pfbPath)
    {
        dynamicBone = null;
        roots = new List<string>();
        exclusions = new List<string>();
        mainBone = String.Empty;
        rootBone = String.Empty;
        boneColls = new Dictionary<string, DynamicBoneCollider>();
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(pfbPath);
       // Debug.Log($"@@@@ {pfbPath}       {obj}" );
        if (obj != null)
        {
            var mainBoneComp = obj.GetComponent<DynamicBone>();
           // Debug.Log($"@@@@ {mainBoneComp}   " );

            if (mainBoneComp)
            {
//                Debug.Log($"@@@@ {mainBoneComp.m_Root == null }  { mainBoneComp.m_Roots.IsNullOrEmpty()}   " );
                if (mainBoneComp.m_Root == null && mainBoneComp.m_Roots.Count==0 )
                {
                    dynamicBone = null;
                    mainBoneComp = null;
                    if (tmpObj!=null) Object.DestroyImmediate(tmpObj);
                    tmpObj = null;
                    return;
                }
                tmpObj = new GameObject("CreateRoleTmpObj");
                dynamicBone = tmpObj.AddComponent<DynamicBone>();
                //root 
                if (mainBoneComp.m_Root != null)
                    rootBone = mainBoneComp.m_Root.gameObject.name;
                
                //roots
                foreach (var VARIABLE in mainBoneComp.m_Roots)
                {
                    roots.Add(VARIABLE.gameObject.name);
                }
                
                //exclusions
                foreach (var VARIABLE in mainBoneComp.m_Exclusions)
                {
                    exclusions.Add(VARIABLE.gameObject.name);
                }
                
                mainBone = mainBoneComp.gameObject.name;
                dynamicBone.m_UpdateRate = mainBoneComp.m_UpdateRate;
                dynamicBone.m_UpdateMode = mainBoneComp.m_UpdateMode;
                dynamicBone.m_Damping = mainBoneComp.m_Damping;
                dynamicBone.m_DampingDistrib = mainBoneComp.m_DampingDistrib;
                dynamicBone.m_Elasticity = mainBoneComp.m_Elasticity;
                dynamicBone.m_ElasticityDistrib = mainBoneComp.m_ElasticityDistrib;
                dynamicBone.m_Stiffness = mainBoneComp.m_Stiffness;
                dynamicBone.m_StiffnessDistrib = mainBoneComp.m_StiffnessDistrib;
                dynamicBone.m_Inert = mainBoneComp.m_Inert;
                dynamicBone.m_InertDistrib = mainBoneComp.m_InertDistrib;
                dynamicBone.m_Friction = mainBoneComp.m_Friction;
                dynamicBone.m_FrictionDistrib = mainBoneComp.m_FrictionDistrib;
                dynamicBone.m_Radius = mainBoneComp.m_Radius;
                dynamicBone.m_RadiusDistrib = mainBoneComp.m_RadiusDistrib;
                dynamicBone.m_EndLength = mainBoneComp.m_EndLength;
                dynamicBone.m_EndOffset = mainBoneComp.m_EndOffset;
                dynamicBone.m_Gravity = mainBoneComp.m_Gravity;
                dynamicBone.m_Force = mainBoneComp.m_Force;
                dynamicBone.m_BlendWeight = mainBoneComp.m_BlendWeight;
                dynamicBone.m_FreezeAxis = mainBoneComp.m_FreezeAxis;
                dynamicBone.m_DistantDisable = mainBoneComp.m_DistantDisable;
                dynamicBone.m_DistanceToObject = mainBoneComp.m_DistanceToObject;
            }

            var colliders = obj.GetComponentsInChildren<DynamicBoneCollider>();
            if (colliders != null)
            {
                foreach (var col in colliders)
                {
                    var coll = new DynamicBoneCollider();
                    coll.m_Direction = col.m_Direction;
                    coll.m_Center = col.m_Center;
                    coll.m_Bound = col.m_Bound;
                    coll.m_Radius = col.m_Radius;
                    coll.m_Height = col.m_Height;
                    coll.m_Radius2 = col.m_Radius2;
                    boneColls.Add(col.gameObject.name, coll);
                }
            }
        }
    }

    public static void CheckCreateObjDynamicBonesAfter(string pfbPath)
    {
      //  Debug.Log($"@@@@@@@   {dynamicBone == null}");
        if (dynamicBone == null) return ;
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(pfbPath);
        if (obj != null)
        {
            var mainBone = obj.AddComponent<DynamicBone>();
            var boneRoots = new List<Transform>();
            var exclusionsTra = new List<Transform>();
            List<DynamicBoneColliderBase> mainBoneColls = new List<DynamicBoneColliderBase>();
            //bone collider
            var tras = obj.GetComponentsInChildren<Transform>();
            foreach (var pair in boneColls)
            {
                foreach (var tra in tras)
                {
                    if (tra.gameObject.name == pair.Key)
                    {
                        var boneCol = tra.gameObject.AddComponent<DynamicBoneCollider>();
                        var targetBoneCol = pair.Value;
                        boneCol.m_Direction = targetBoneCol.m_Direction;
                        boneCol.m_Center = targetBoneCol.m_Center;
                        boneCol.m_Bound = targetBoneCol.m_Bound;
                        boneCol.m_Radius = targetBoneCol.m_Radius;
                        boneCol.m_Height = targetBoneCol.m_Height;
                        boneCol.m_Radius2 = targetBoneCol.m_Radius2;
                        mainBoneColls.Add(boneCol);
                        break;
                    }
                }
            }
            //main bone

            //roots
            foreach (var str in roots)
            {
                foreach (var tra in tras)
                {
                    if (tra.gameObject.name == str)
                    {
                        boneRoots.Add(tra);
                        break;
                    }
                }
            }   
            //exclusions
            foreach (var str in exclusions)
            {
                foreach (var tra in tras)
                {
                    if (tra.gameObject.name == str)
                    {
                        exclusionsTra.Add(tra);
                        break;
                    }
                }
            }

            //root
            if (!string.IsNullOrEmpty(rootBone))
            {
                foreach (var tra in tras)
                {
                    if (tra.gameObject.name == rootBone)
                    {
                        mainBone.m_Root = tra;
                        break;
                    }
                }
            }

            mainBone.m_Roots = boneRoots;
            mainBone.m_Colliders = mainBoneColls;
            mainBone.m_Exclusions = exclusionsTra;
            mainBone.m_UpdateRate = dynamicBone.m_UpdateRate;
            mainBone.m_UpdateMode = dynamicBone.m_UpdateMode;
            mainBone.m_Damping = dynamicBone.m_Damping;
            mainBone.m_DampingDistrib = dynamicBone.m_DampingDistrib;
            mainBone.m_Elasticity = dynamicBone.m_Elasticity;
            mainBone.m_ElasticityDistrib = dynamicBone.m_ElasticityDistrib;
            mainBone.m_Stiffness = dynamicBone.m_Stiffness;
            mainBone.m_StiffnessDistrib = dynamicBone.m_StiffnessDistrib;
            mainBone.m_Inert = dynamicBone.m_Inert;
            mainBone.m_InertDistrib = dynamicBone.m_InertDistrib;
            mainBone.m_Friction = dynamicBone.m_Friction;
            mainBone.m_FrictionDistrib = dynamicBone.m_FrictionDistrib;
            mainBone.m_Radius = dynamicBone.m_Radius;
            mainBone.m_RadiusDistrib = dynamicBone.m_RadiusDistrib;
            mainBone.m_EndLength = dynamicBone.m_EndLength;
            mainBone.m_EndOffset = dynamicBone.m_EndOffset;
            mainBone.m_Gravity = dynamicBone.m_Gravity;
            mainBone.m_Force = dynamicBone.m_Force;
            mainBone.m_BlendWeight = dynamicBone.m_BlendWeight;
            mainBone.m_FreezeAxis = dynamicBone.m_FreezeAxis;
            mainBone.m_DistantDisable = dynamicBone.m_DistantDisable;
            mainBone.m_DistanceToObject = dynamicBone.m_DistanceToObject;
        }

        mainBone = String.Empty;
        boneColls = null;
        dynamicBone = null;
        if (tmpObj!=null) Object.DestroyImmediate(tmpObj);
        tmpObj = null;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}