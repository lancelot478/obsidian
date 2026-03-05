using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Cinemachine;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.PlayerLoop;
using Color = UnityEngine.Color;
using ed = UnityEditor.EditorGUILayout;
using FontStyle = UnityEngine.FontStyle;

[CustomEditor(typeof(TimelineCreator))]
public sealed class TimelineCreatorEditor : Editor
{
    private enum directorPosType
    {
        NONE = 0,
        ZERO = 1,
        STAY_WITH_PLAYER = 2,
        BOSS = 3,
    }

    private PlayableAsset timelineAsset;

    //TODO暂时只有一个怪物/角色一般
    private GameObject rolePrefab1;

    private GameObject playTimelineObj;

    #region Create

    private string createPrefabName;
    private string createBundleName;
    private string createShotType;
    private string createwwiseEvent;
    private string createwwiseStopEvent;
    private string createBindAnimClipsName;
    private bool createIsBossTimeline = true;
    private bool createIsUsePrefabAsset = true;
    private bool createIsStopMuffin = false;
    private bool createIsBindInGamePlayerToTrack = false;
    private Vector3Int createMuffinLocalPosition = Vector3Int.zero;
    private Vector3Int createMuffinWorldEulerAngle = Vector3Int.zero;
    private Vector3Int createMainPlayerWorldEulerAngle = Vector3Int.zero;
    private Vector3Int createPlayerOneLocalPosition = Vector3Int.zero;
    private Vector3Int createPlayerOneLocalEulerAngle = Vector3Int.zero;
    private directorPosType createDirectorType = directorPosType.ZERO;
    GUIStyle btnStyle = new GUIStyle();

    #endregion

    private List<TimelineDataEntity> localDataLst;
    private int currSelectIndex = 0;
    private List<string> currPopUpStrLst = new List<string>();

    #region Class

    private class TimelineDataEntity
    {
        public string PrefabName;
        public string ShotType;
        public string WwiseEvent;
        public string WwiseStopEvent;
        public string BindAnimClipsName;
        public bool IsBossTimeline;
        public bool IsBindInGamePlayerToTrack;
        public int DirectorPosType;
        public bool IsUsePrefabAsset = false;
        public bool IsStopMuffin = false;
        public int[] MuffinLocalPosition;
        public int[] MuffinWorldEulerAngle;
        public int[] MainPlayerWorldEulerAngle;
        public int[] PlayerOneLocalPosition;
        public int[] PlayerOneLocalEulerAngle;
    }

    #endregion


    #region Camera

    private List<Animator> vcamLst;
    private CinemachineBrain mainCamera;

    #endregion

    public override void OnInspectorGUI()
    {
        btnStyle.fontSize = 14;
        btnStyle.fontStyle = FontStyle.Normal;
        btnStyle.normal.textColor = Color.red;

        base.OnInspectorGUI();
        ed.BeginVertical();
        EditorGUI.BeginChangeCheck();
        currSelectIndex = ed.Popup("本地数据列表:", currSelectIndex, currPopUpStrLst.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            OnSelectOneTimelineData(currSelectIndex);
        }

        ed.BeginHorizontal("box");
        bool onLoadDataClick = GUILayout.Button("加载所有数据", GUILayout.Width(100), GUILayout.Height(30));
        if (onLoadDataClick)
        {
            LoadLocalData();
        }

        //  bool onSaveDataClick = GUILayout.Button("保存单条数据", GUILayout.Width(100), GUILayout.Height(30));
        bool onSaveAllDataClick = GUILayout.Button("保存所有数据", GUILayout.Width(100), GUILayout.Height(30));
        if (onSaveAllDataClick)
        {
            SaveAllData();
        }

        bool onDeleteDataClick = GUILayout.Button("删除选中数据项", GUILayout.Width(100), GUILayout.Height(30));
        if (onDeleteDataClick)
        {
            DeleteSelectData(currSelectIndex);
        }

        // bool test = GUILayout.Button("Test", GUILayout.Width(100), GUILayout.Height(30));
        // if (test)
        // {
        //     // AssetImporter importer=AssetImporter.GetAtPath("Assets/Config/ttdbl2_config/TimelineConfig.json");
        //     // importer.assetBundleName=""
        // }
        ed.Space(50);
        GUI.color = Color.green;
        bool check = GUILayout.Button("检查Timeline资源", GUILayout.Width(150), GUILayout.Height(30));
        if (check)
        {
            EditorUtility.DisplayDialog("Editor", "开发中..", "关闭");
        }

        GUI.color = Color.white;

        ed.EndHorizontal();
        ed.EndVertical();


        ed.BeginVertical("box");
        ed.LabelField("制作区", btnStyle);
        timelineAsset = ed.ObjectField("Timeline文件", timelineAsset, typeof(PlayableAsset)) as PlayableAsset;
        rolePrefab1 = ed.ObjectField("人物模型预制体", rolePrefab1, typeof(GameObject)) as GameObject;
        ed.HelpBox("只有在生成预制体的时候才需要指定TimelineAsset文件和人物模型预制体,修改数据时无需指定", MessageType.Warning);
        createPrefabName = ed.TextField("生成资源名", createPrefabName);
        createShotType = ed.TextField("镜头类型", createShotType);
        createwwiseEvent = ed.TextField("音频事件名", createwwiseEvent);
        createwwiseStopEvent = ed.TextField("音频结束事件名", createwwiseStopEvent);
        createBindAnimClipsName = ed.TextField("绑定动画名称", createBindAnimClipsName);
        createIsBossTimeline = ed.Toggle("是否BossTimeline", createIsBossTimeline);
        createIsBindInGamePlayerToTrack = ed.Toggle("运行时绑定角色到PlayerTrack", createIsBindInGamePlayerToTrack);

        ed.BeginVertical("box");
        btnStyle.normal.textColor = Color.green;
        ed.LabelField("特殊功能区", btnStyle);
        createDirectorType = (directorPosType)ed.EnumPopup("播放时director位置运动类型", createDirectorType);
        createIsUsePrefabAsset = ed.Toggle("是否创建Prefab(一般默认勾选)", createIsUsePrefabAsset);
        createIsStopMuffin = ed.Toggle("播放时是否停止麦芬移动(新手引导使用)", createIsStopMuffin);
        createMuffinLocalPosition = ed.Vector3IntField("麦芬本地坐标", createMuffinLocalPosition);
        createMuffinWorldEulerAngle = ed.Vector3IntField("麦芬世界旋转欧拉角", createMuffinWorldEulerAngle);
        createMainPlayerWorldEulerAngle = ed.Vector3IntField("主角世界旋转欧拉角", createMainPlayerWorldEulerAngle);
        createPlayerOneLocalPosition = ed.Vector3IntField("第一个角色本地位移", createPlayerOneLocalPosition);
        createPlayerOneLocalEulerAngle = ed.Vector3IntField("第一个角色本地旋转欧拉角", createPlayerOneLocalEulerAngle);

        ed.EndVertical();

        bool onCreateClick = GUILayout.Button("点击生成预制体,尝试新增新数据并保存", GUILayout.Width(300), GUILayout.Height(30));
        if (onCreateClick)
        {
            CreatePrefab();
        }

        ed.EndVertical();

        ed.Space(20f);


        ed.BeginVertical("box");
        btnStyle.normal.textColor = Color.yellow;
        ed.LabelField("浏览区", btnStyle);
        playTimelineObj = ed.ObjectField("浏览Timeline文件", playTimelineObj, typeof(GameObject)) as GameObject;
        bool viewPrefab = GUILayout.Button("点击预览预制体", GUILayout.Width(100), GUILayout.Height(30));
        if (viewPrefab)
        {
            PlayOneTimelineAsset();
        }

        ed.EndVertical();
        ed.BeginVertical("box");

        ed.EndVertical();
        UpdateAction();
    }

    private void PlayOneTimelineAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"Assets/Animations/Timeline/Prefab/{createPrefabName}.prefab");
        var objIns = GameObject.Instantiate(asset);
        var objInsPar = GameObject.Find("生成历史----不可删除----");
        var childCnt = objInsPar.transform.childCount;
        for (int j = childCnt; j > 0; j--)
        {
            GameObject.DestroyImmediate(objInsPar.transform.GetChild(j - 1).gameObject);
        }

        objIns.transform.SetParent(objInsPar.transform, true);
        objIns.transform.position = Vector3.zero;
        //Virtual Cam 绑定
        var dir = GameObject.Find("Director");
        if (vcamLst == null)
        {
            vcamLst = new List<Animator>();
            mainCamera = GameObject.Find("Camera").GetComponent<CinemachineBrain>();
            for (int i = 1; i < 7; i++)
            {
                vcamLst.Add(dir.transform.Find("CM vcam" + i).GetComponent<Animator>());
            }
        }

        var timelinePlayableDir =
            objIns.transform.Find("content_Timeline").GetChild(0).GetComponent<PlayableDirector>();
        objIns.transform.Find("content_Monster").GetChild(0).gameObject.SetActive(true);
        var monsterAnim = objIns.transform.Find("content_Monster").GetChild(0).Find("tex_Anim")
            .GetComponent<Animator>();
        var playableAsset = timelinePlayableDir.playableAsset;

        foreach (PlayableBinding binding in playableAsset.outputs)
        {
            if (binding.streamName.Contains("CinemachineTrack"))
            {
                CinemachineTrack track = binding.sourceObject as CinemachineTrack;
                foreach (var cam in vcamLst)
                {
                    cam.gameObject.SetActive(false);
                }

                var index = 0;
                for (int i = 0; i < track.GetClips().ToList().Count(); i++)
                {
                    vcamLst[index].gameObject.SetActive(true);
                    track.GetClips().ToList()[i].VirtualCamera = new ExposedReference<CinemachineVirtualCameraBase>()
                    {
                        defaultValue = vcamLst[index++].GetComponent<CinemachineVirtualCamera>()
                    };
                }

                foreach (var shot in track.GetClips())

                    timelinePlayableDir.SetGenericBinding(binding.sourceObject, mainCamera);
            }
            else if (binding.streamName.Equals("Cam1Track"))
            {
                timelinePlayableDir.SetGenericBinding(binding.sourceObject, vcamLst[0]);
            }
            else if (binding.streamName.Equals("Cam2Track"))
            {
                timelinePlayableDir.SetGenericBinding(binding.sourceObject, vcamLst[1]);
            }
            else if (binding.streamName.Equals("Cam3Track"))
            {
                timelinePlayableDir.SetGenericBinding(binding.sourceObject, vcamLst[2]);
            }
            else if (binding.streamName.Equals("Cam4Track"))
            {
                timelinePlayableDir.SetGenericBinding(binding.sourceObject, vcamLst[3]);
            }
            else if (binding.streamName.Equals("Cam5Track"))
            {
                timelinePlayableDir.SetGenericBinding(binding.sourceObject, vcamLst[4]);
            }
            else if (binding.streamName.Equals("Cam6Track"))
            {
                timelinePlayableDir.SetGenericBinding(binding.sourceObject, vcamLst[5]);
            }
        }

        monsterAnim.gameObject.SetActive(true);
        timelinePlayableDir.enabled = true;
        timelinePlayableDir.playOnAwake = true;
        if (EditorApplication.isPlaying)
        {
            timelinePlayableDir.gameObject.SetActive(false);
            timelinePlayableDir.gameObject.SetActive(true);
        }
        else
        {
            EditorApplication.isPlaying = true;
        }
    }


    private void UpdateAction()
    {
    }

    #region DateModel

    private void LoadLocalData()
    {
        byte[] buffer = FileHelper.GetTimelineJsonBuffer();
        if (buffer != null)
        {
            string str = Encoding.UTF8.GetString(buffer);
            localDataLst = new List<TimelineDataEntity>();
            localDataLst = JsonConvert.DeserializeObject<List<TimelineDataEntity>>(str);
            currPopUpStrLst.Clear();
            foreach (var v in localDataLst)
            {
                currPopUpStrLst.Add(v.ShotType + "  对应资源:" + v.PrefabName);
            }

            currSelectIndex = 0;
            OnSelectOneTimelineData(currSelectIndex);
            Debug.Log("加载数据完毕\n" + str + "总长度:" + localDataLst.Count);
        }
    }

    private void OnSelectOneTimelineData(int index)
    {
        if (localDataLst.Count - 1 >= index)
        {
            TimelineDataEntity et = localDataLst[index];
            createPrefabName = et.PrefabName;
            createShotType = et.ShotType;
            createwwiseEvent = et.WwiseEvent;
            createwwiseStopEvent = et.WwiseStopEvent;
            createBindAnimClipsName = et.BindAnimClipsName;
            createIsBossTimeline = et.IsBossTimeline;
            createIsBindInGamePlayerToTrack = et.IsBindInGamePlayerToTrack;
            createDirectorType = (directorPosType)et.DirectorPosType;
            createIsUsePrefabAsset = et.IsUsePrefabAsset;
            createIsStopMuffin = et.IsStopMuffin;
            if (et.MuffinLocalPosition != null)
                createMuffinLocalPosition = new Vector3Int(et.MuffinLocalPosition[0],
                    et.MuffinLocalPosition[1], et.MuffinLocalPosition[2]);
            if (et.MuffinWorldEulerAngle != null)
                createMuffinWorldEulerAngle = new Vector3Int(et.MuffinWorldEulerAngle[0],
                    et.MuffinWorldEulerAngle[1], et.MuffinWorldEulerAngle[2]);
            if (et.MainPlayerWorldEulerAngle != null)
                createMainPlayerWorldEulerAngle = new Vector3Int(et.MainPlayerWorldEulerAngle[0],
                    et.MainPlayerWorldEulerAngle[1], et.MainPlayerWorldEulerAngle[2]);
            if (et.PlayerOneLocalPosition != null)
                createPlayerOneLocalPosition = new Vector3Int(et.PlayerOneLocalPosition[0],
                    et.PlayerOneLocalPosition[1], et.PlayerOneLocalPosition[2]);
            if (et.PlayerOneLocalEulerAngle != null)
                createPlayerOneLocalEulerAngle = new Vector3Int(et.PlayerOneLocalEulerAngle[0],
                    et.PlayerOneLocalEulerAngle[1], et.PlayerOneLocalEulerAngle[2]);
        }
        else
        {
            Debug.LogError("异常,请联系开发人员");
        }
    }

    private void OnSaveOneTimelineData(int index)
    {
        if (localDataLst.Count >= index)
        {
            TimelineDataEntity et = localDataLst[index];
            et.PrefabName = createPrefabName;
            et.ShotType = createShotType;
            et.WwiseEvent = createwwiseEvent;
            et.WwiseStopEvent = createwwiseStopEvent;
            et.BindAnimClipsName = createBindAnimClipsName;
            et.IsBossTimeline = createIsBossTimeline;
            et.IsBindInGamePlayerToTrack = createIsBindInGamePlayerToTrack;
            et.DirectorPosType = (int)createDirectorType;
            et.IsUsePrefabAsset = createIsUsePrefabAsset;
            et.IsStopMuffin = createIsStopMuffin;

            et.MuffinLocalPosition = new[]
            {
                createMuffinLocalPosition.x,
                createMuffinLocalPosition.y,
                createMuffinLocalPosition.z
            };

            et.MuffinWorldEulerAngle = new[]
            {
                createMuffinWorldEulerAngle.x,
                createMuffinWorldEulerAngle.y,
                createMuffinWorldEulerAngle.z
            };
            et.MainPlayerWorldEulerAngle = new[]
            {
                createMainPlayerWorldEulerAngle.x,
                createMainPlayerWorldEulerAngle.y,
                createMainPlayerWorldEulerAngle.z
            };
            et.PlayerOneLocalPosition = new[]
            {
                createPlayerOneLocalPosition.x,
                createPlayerOneLocalPosition.y,
                createPlayerOneLocalPosition.z
            };
            et.PlayerOneLocalEulerAngle = new[]
            {
                createPlayerOneLocalEulerAngle.x,
                createPlayerOneLocalEulerAngle.y,
                createPlayerOneLocalEulerAngle.z
            };
        }
    }

    private void SaveAllData()
    {
        //先修改本地内存里的数据
        if (currSelectIndex != -1)
            OnSaveOneTimelineData(currSelectIndex);

        if (localDataLst.Count > 0)
        {
            string jsonStr = JsonConvert.SerializeObject(localDataLst);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonStr);
            FileHelper.CreateOrUpdateTimelineJsonFile(bytes);
            //保存所有ShotType
            StringBuilder sbr = new StringBuilder();
            sbr.AppendLine("ShotType={}");
            sbr.AppendLine("ShotType.SHOT_2 = \"2\" ");
            sbr.AppendLine("ShotType.SHOT_11 = \"11\" ");
            foreach (var v in localDataLst)
            {
                sbr.AppendLine($"ShotType.SHOT_{v.ShotType}={"\"" + v.ShotType + "\""}");
            }

            bytes = Encoding.UTF8.GetBytes(sbr.ToString());
            FileHelper.CreateShotTypeFile(bytes);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            //重加载
            LoadLocalData();
        }
    }

    private void DeleteSelectData(int i)
    {
        //指针置空
        currSelectIndex = -1;
        if (localDataLst.Count >= i)
        {
            localDataLst.RemoveAt(i);
        }

        //保存
        SaveAllData();
        //重加载
        LoadLocalData();
    }

    private void CreatePrefab()
    {
        //新增数据项到本地内存
        localDataLst.Add(new TimelineDataEntity());
        //指针指向最后一位
        currSelectIndex = localDataLst.Count - 1;
        //资源处理
        GameObject obj = new GameObject(createPrefabName);
        GameObject tlObj = new GameObject("content_Timeline");
        GameObject tlMonster = new GameObject("content_Monster");
        GameObject tlFx = new GameObject("content_Fx");
        tlObj.transform.SetParent(obj.transform);
        tlMonster.transform.SetParent(obj.transform);
        tlFx.transform.SetParent(obj.transform);
        GameObject tlChildObj = new GameObject(createPrefabName);
        //timeline组件
        var dir = tlChildObj.AddComponent<PlayableDirector>();
        dir.playOnAwake = false;
        dir.enabled = false;
        dir.playableAsset = timelineAsset;
        tlChildObj.transform.SetParent(tlObj.transform);

        //怪物组件
        if (rolePrefab1)
        {
            var role1 = GameObject.Instantiate(rolePrefab1);
            role1.transform.SetParent(tlMonster.transform);
            role1.transform.localScale = Vector3.one;
            role1.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        //保存本地
        GameObject AssetObj = PrefabUtility.SaveAsPrefabAsset(obj,
            FileHelper.timelineAssetSavePathPrefix + createPrefabName + ".prefab");
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        //bundle设置
        string fullPath = FileHelper.timelineAssetSavePathPrefix;
        int AssetStrStartIdx = FileHelper.timelineAssetSavePathPrefix.IndexOf("Assets/");
        string AssetPath = fullPath.Remove(0, AssetStrStartIdx);


        AssetImporter importer = AssetImporter.GetAtPath(AssetPath + createPrefabName + ".prefab");
        importer.assetBundleName = "tl/" + createPrefabName;
        //数据段保存
        SaveAllData();
    }

    #endregion
}