using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName="ScriptableAvatarFashionData")]
public class ScriptableAvatarFashionData : ScriptableObject
{
    public enum FashionType
    {
        Suit,
        Head,
        Face,
        Wing,
        Tail,
        Weapon,
        BigHead,
        Assistant
        
    }

    public enum SexType
    {
        UnLimit=-1,
        Female=0,
        Male=1
    }

    public enum WeaponColorType
    {
        Weapon = 0,
        Assistant = 1,
        Both = 2
    }

    public enum JobType
    {
        UnLimit=-1,
        Swordman=1000,
        Ranger=2000,
        Magician=3000,
        Assistant=4000,
        Reverend=5000
    }

    public enum AttachedPointType
    {
        Null=0,
        Ep2=1,
        Wp1=2
    }
    
    public List<AvatarFashionColorSetTypeConfig> FashionColorSetLst;
    
    //Editor
    [Header("仅供新版数据查看使用")]
    public AvatarFashionColorSetTypeConfig editorFashionColorSet;
 
    public Dictionary<int, List<AvatarFashionColorSetConfig>> editorOneTypeFashionColorPageDic;
    [Header("仅供新版数据查看使用")]
    public  List<AvatarFashionColorSetConfig> editorOneTypeFashionColorPageList;
    
    #region Configs
    
    [Serializable]
    public class AvatarFashionColorSetTypeConfig
    {
        [Header("时装类型")][HideInInspector]
        public FashionType FashionType;
        [Header("单件时装染色配置")]
        public List<AvatarFashionColorSetConfig> FashionColorSetLst;
    }
    [Serializable]
    public class AvatarFashionColorSetConfig
    {
        [Header("资源ID")]
        public string AssetID;
        // [Header("是否无性别时装")]
        // public bool IsUnLimitSexFashion;
        [Header("当前时装的第几套染色")]
        public int ColorSetIndex;
        [Header("染色配置")]
        public List<AvatarFashionColorSetSexConfig> SexConfigLst;

        
        
    }
    [Serializable]
    public class AvatarFashionColorSetSexConfig
    {
        [Header("备注,仅查阅")] 
        public string mark;
        [Header("性别")]
        public SexType Sex;
        [Header("职业派系")]
        public JobType Job;
        [Header("武器染色应用类型")]
        public WeaponColorType weaponColorType;
        [Header("[无需填写]资源包名,应用后自动生成")]
        public string BundleName;
        [Header("染色物体")]
        public Transform Obj;
        [Header("染色材质球")]
        public List<Material> MatNameIndexArr;
        [Header("是否使用Mask染色")]
        public bool IsUseMaskColor;
        [Header("Mask染色配置")]
        public List<AvatarFashionColorSetMaskMaterialConfig> MaskMaterialConfigLst;
        [Header("是否有特效")]
        public bool HasFX;
        [Header("特效Prefab物体")]
        public GameObject FXObj;
        [Header("特效ID (BattleConfig FX中)")]
        public int FXID;
        [Header("特效挂载节点")]
        public AttachedPointType FXAttachedType;
        [Header("[无需填写]特效资源包名,应用后自动生成")]
        public string FXBundleName;
    }
    [Serializable]
    public class AvatarFashionColorSetMaskMaterialConfig    
    {
        [Header("【Mask材质唯一时】")]
        public Material MaskMaterial;
        [Header("【Mask材质不唯一时】当前Mask参数应用在第几个材质上 (从1开始")]
        public int CurrMatIndex = -1;

        public Color Color1;
        public Color Color2;
        public Color Color3;
        public Color Color4;
    }

    #endregion 
}

