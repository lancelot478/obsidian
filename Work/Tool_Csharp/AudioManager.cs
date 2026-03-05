using UnityEngine;
using System;
using SLua;
using System.Collections.Generic;
using System.Linq;

[CustomLuaClassAttribute]
public class AudioManager : MonoBehaviour
{
    static AudioManager mInstance;
    static GameObject mGlobalObject;
    static AkAudioListener mListener;
    public static readonly string[] AUDIO_FILE_EXTENSIONS = new string[] { ".wem", ".bnk" };

    void Awake() {
        string path = Config.VersionPackageDirectoryName;
        if (mInstance)
        {
            DestroyImmediate(this);
            return;
        }
        mInstance = this;
        mGlobalObject = GameObject.FindWithTag("AudioManager");
        mListener = Camera.main.gameObject.GetComponent<AkAudioListener>();
        if (mListener == null)
        {
            mListener = Camera.main.gameObject.AddComponent<AkAudioListener>();
        }
    }

    /// <summary>
    /// 加载bank,todo:热更加载测试
    /// </summary>
    /// <param name="bankName"></param>
    /// <param name="callback"></param>
    public static void LoadBank(string bankName, AkCallbackManager.BankCallback callback = null)
    {
        if (!string.IsNullOrEmpty(bankName))
        {
            if (callback != null)
            {
                AkBankManager.LoadBankAsync(bankName, callback);
            }
            else
            {
                AkBankManager.LoadBank(bankName, false, false);
            }
        }
    }

    /// <summary>
    /// 卸载bank
    /// </summary>
    /// <param name="bankName"></param>
    public static void UnloadBank(string bankName)
    {
        if (!string.IsNullOrEmpty(bankName))
        {
            AkBankManager.UnloadBank(bankName);
        }
    }

    /// <summary>
    /// 播放音频事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="gameObj"></param>
    /// <param name="callBack"></param>
    /// <returns></returns>
    public static uint PostEvent(string eventName, GameObject gameObj = null, Action callBack = null)
    {
        if (gameObj == null)
            gameObj = mGlobalObject;
        uint playingId = AkSoundEngine.AK_INVALID_PLAYING_ID;
        if (!string.IsNullOrEmpty(eventName))
        {
            if (callBack != null)
            {
                playingId = AkSoundEngine.PostEvent(eventName, gameObj, (uint)AkCallbackType.AK_EndOfEvent, EventCallback, callBack);
            }
            else
            {
                playingId = AkSoundEngine.PostEvent(eventName, gameObj);
            }

        }
        return playingId;
    }

    /// <summary>
    /// 播放音频事件回调
    /// </summary>
    /// <param name="in_cookie"></param>
    /// <param name="in_type"></param>
    /// <param name="in_info"></param>
    static void EventCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        switch (in_type)
        {
            case AkCallbackType.AK_EndOfEvent:
                if (in_cookie != null)
                {
                    Action action = (Action)in_cookie;
                    action();
                }
                break;
            default:
                //AkSoundEngine.LogError("Callback Type not march.");
                break;
        }
    }

    /// <summary>
    /// 停止播放音频事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="gameObj"></param>
    /// <param name="transitionDuration"></param>
    /// <param name="curveInterpolation"></param>
    /// <returns></returns>
    public static AKRESULT StopEvent(string eventName, GameObject gameObj = null, int transitionDuration = 0)
    {
        if (!string.IsNullOrEmpty(eventName))
        {
            if (gameObj == null)
                gameObj = mGlobalObject;
            AKRESULT result = AkSoundEngine.ExecuteActionOnEvent(eventName, AkActionOnEventType.AkActionOnEventType_Stop, gameObj, transitionDuration, AkCurveInterpolation.AkCurveInterpolation_Linear);
            return result;
        }
        return AKRESULT.AK_Fail;
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public static void AudioPause()
    {
        AkSoundEngine.Suspend();
    }

    /// <summary>
    /// 恢复
    /// </summary>
    public static void AudioResume()
    {
        AkSoundEngine.WakeupFromSuspend();
    }

    /// <summary>
    /// 停止声音引擎
    /// </summary>
    public static void Term()
    {
        AkSoundEngineController.Instance.Terminate();
    }

    /// <summary>
    /// 停止发声体上所有声音
    /// </summary>
    public static void StopAll(GameObject gameObj)
    {
        AkSoundEngine.StopAll(gameObj);
    }

    /// <summary>
    /// 设置游戏实时参数
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static AKRESULT SetRTPC(string name, float value)
    {
        AKRESULT result = AkSoundEngine.SetRTPCValue(name, value);
        return result;
    }

    /// <summary>
    /// 设置switch参数
    /// </summary>
    /// <param name="switchGroup"></param>
    /// <param name="switchState"></param>
    /// <param name="gameObj"></param>
    /// <returns></returns>
    public static AKRESULT SetSwitch(string switchGroup, string switchState, GameObject gameObj = null)
    {
        if (gameObj == null)
            gameObj = mGlobalObject;
        AKRESULT result = AkSoundEngine.SetSwitch(switchGroup, switchState, gameObj);
        return result;
    }

    /// <summary>
    /// 设置state
    /// </summary>
    /// <param name="stateGroup"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public static AKRESULT SetState(string stateGroup, string state)
    {
        AKRESULT result = AkSoundEngine.SetState(stateGroup, state);
        return result;
    }

    /// <summary>
    /// event是否在播放
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="gameObj"></param>
    /// <returns></returns>
    public static bool IsEventPlayingOnGameObject(string eventName, GameObject gameObj)
    {
        uint checkID = AkSoundEngine.GetIDFromString(eventName);

        uint[] playingIds = new uint[10];
        uint count = (uint)playingIds.Length;
        AKRESULT result = AkSoundEngine.GetPlayingIDsFromGameObject(gameObj, ref count, playingIds);
        for (int i = 0; i < count; i++)
        {
            uint playingId = playingIds[i];
            uint eventId = AkSoundEngine.GetEventIDFromPlayingID(playingId);
            if (eventId == checkID)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 刷新环境声位置(自带原生脚本AkAmbient)
    /// </summary>
    public static void RefreshAmbienEventPositions()
    {
        foreach (var item in AkAmbient.multiPosEventTree)
        {
            AkMultiPosEvent eventPosList;
            if (AkAmbient.multiPosEventTree.TryGetValue(item.Key, out eventPosList))
            {
                var positionArray = new AkPositionArray((uint)eventPosList.list.Count);
                for (var i = 0; i < eventPosList.list.Count; i++)
                {
                    positionArray.Add(eventPosList.list[i].transform.position, eventPosList.list[i].transform.forward,
                        eventPosList.list[i].transform.up);
                }
                SetMultiplePositions(eventPosList.list[0].gameObject, positionArray);
            }
        }
    }

    private static void SetAmbienEventPositions(List<GameObject> ambientObjList, GameObject gameObj)
    {
        var positionArray = new AkPositionArray((uint)ambientObjList.Count);
        foreach (var item in ambientObjList)
        {
            positionArray.Add(item.transform.position, item.transform.forward, item.transform.up);
        }
        if (ambientObjList[0].GetComponent<AkGameObj>() == null)
        {
            ambientObjList[0].AddComponent<AkGameObj>();
        }
        SetMultiplePositions(gameObj, positionArray);
    }

    /// <summary>
    /// 设置播放点位置，播放环境声
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventTag"></param>
    public static void PostAmbientEvent(string eventName, string eventTag, GameObject gameObj)
    {
        List<GameObject> ambientObjList = GameObject.FindGameObjectsWithTag(eventTag).ToList<GameObject>();
        if (ambientObjList.Count > 0)
        {
            SetAmbienEventPositions(ambientObjList, gameObj);
            AudioManager.StopEvent(eventName, gameObj);
            AudioManager.PostEvent(eventName, gameObj);
        }
    }
 
    /// <summary>
    /// 从环境声的多长时间点播放
    /// </summary>
    /// <param name="eventID"></param>
    /// <param name="eventName"></param>
    /// <param name="startTime"> 毫秒</param>
    /// <param name="gameObj"> </param>
    public static void SeekOnEvent(uint eventID ,uint playingID, string eventName, int startTime, GameObject gameObj)
    {
        if(gameObj == null) {
             return ;
        }
        //TODO 先暂停
       // AudioManager.StopEvent(eventName, gameObj);
       // uint playingID = AudioManager.PostEvent(eventName, gameObj);
        ulong gameObjectID = (ulong)AkSoundEngine.GetAkGameObjectID(gameObj);
        AKRESULT result = AkSoundEngine.SeekOnEvent(
            eventID, // 事件ID
            gameObjectID, // 游戏对象ID 
            startTime,
            false, // 是否跳转到最近的标记
            playingID // 播放ID
        );
        // 检查结果
        if (result == AKRESULT.AK_Success)
        {
            Debug.Log("@@@@@@@@@@@成功: " + playingID);
        }
        else
        {
            Debug.LogError(" @@@@@@@@@@@ 定位失败，错误代码: " + result);
        }
    }

    /// <summary>
    /// 设置环境声点音源位置
    /// </summary>
    /// <param name="in_GameObjectID"></param>
    /// <param name="in_pPositions"></param>
    /// <returns></returns>
    public static AKRESULT SetMultiplePositions(GameObject in_GameObjectID, AkPositionArray in_pPositions)
    {
        return AkSoundEngine.SetMultiplePositions(in_GameObjectID, in_pPositions, (ushort)in_pPositions.Count, AkMultiPositionType.MultiPositionType_MultiSources);
    }

    /// <summary>
    /// 设置游戏发声体的bus音量
    /// </summary>
    public static void SetGameObjectOutputBusVolume(GameObject in_emitterObjID, GameObject in_listenerObjID, float in_fControlValue)
    {
        AkSoundEngine.SetGameObjectOutputBusVolume(in_emitterObjID, in_listenerObjID, in_fControlValue);
    }

    /// <summary>
    /// 设置发声体的位置接口1
    /// </summary>
    public static AKRESULT SetObjectPosition(GameObject gameObject, Transform transform)
    {
        return AkSoundEngine.SetObjectPosition(gameObject, transform);
    }

    /// <summary>
    /// 设置发声体的位置接口2
    /// </summary>
    public static AKRESULT SetObjectPosition(GameObject gameObject, Vector3 position, Vector3 forward, Vector3 up)
    {
        return AkSoundEngine.SetObjectPosition(gameObject, position, forward, up);
    }

    /// <summary>
    /// 设置发声体的位置接口3
    /// </summary>
    public static AKRESULT SetObjectPosition(GameObject gameObject, float posX, float posY, float posZ, float frontX, float frontY, float frontZ, float topX, float topY, float topZ)
    {
        return AkSoundEngine.SetObjectPosition(gameObject, posX, posY, posZ, frontX, frontY, frontZ, topX, topY, topZ);
    }

    /// <summary>
    /// 设置当前语言
    /// </summary>
    public static AKRESULT SetCurrentLanguage(string in_pszAudioSrcPath)
    {
        return AkSoundEngine.SetCurrentLanguage(in_pszAudioSrcPath);
    }

    /// <summary>
    /// 总线背景音乐是否静音
    /// </summary>
    public static void MuteBackgroundMusic(bool in_bMute)
    {
        AkSoundEngine.MuteBackgroundMusic(in_bMute);
    }

    /// <summary>
    /// 加载file package 头信息
    /// </summary>
    /// <param name="packageFileName"></param>
    /// <param name="packageId"></param>
    /// <returns></returns>
    public static AKRESULT LoadFilePackage(string packageFileName, out uint packageId)
    {
        return AkSoundEngine.LoadFilePackage(packageFileName, out packageId);
    }

    /// <summary>
    /// 卸载file package 头信息
    /// </summary>
    /// <param name="packageId"></param>
    /// <returns></returns>
    public static AKRESULT UnloadFilePackage(uint packageId)
    {
        return AkSoundEngine.UnloadFilePackage(packageId);
    }

    /// <summary>
    /// 添加一个base路径给到声音引擎，用来加载声音资源(热更新)
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static AKRESULT AddBasePath(string path)
    {
        return AkSoundEngine.AddBasePath(path);
    }

}
