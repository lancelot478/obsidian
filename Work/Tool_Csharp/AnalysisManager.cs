using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalysisManager : MonoBehaviour
{
    private int currFrame = 0;
    private float lastRealTime = 0;
    private static float currFrameActiveCostTime = 0;

    //Active 
    //是否统计每帧Active数据
    public static bool IsRecordActivePerFrame = false;
    public static List<KeyValuePair<string, float>> activeObjNameLstPerFrame = new List<KeyValuePair<string, float>>();

    // Update is called once per frame
#if ANALYSIS
    void Update()
    {
        if (IsRecordActivePerFrame)
        {
            if (Time.frameCount != currFrame)
            {
                float coastTime = Time.realtimeSinceStartup - lastRealTime;
                if (coastTime > 0.1f)
                {
                    //前后两帧>100ms
                    // print($"当前帧 耗时长:{Time.frameCount},active当前调用次数:{activeObjNameLstPerFrame.Count },耗时:{coastTime}  ");
                }

                if (currFrameActiveCostTime * 1000 > 1 ||activeObjNameLstPerFrame.Count>30 )
                {
                    print(
                        $"当前帧:{Time.frameCount},active调用次数:{activeObjNameLstPerFrame.Count},耗时:{coastTime} active耗时:{currFrameActiveCostTime * 1000} ms");
                    activeObjNameLstPerFrame.Sort((x, y) => x.Value.CompareTo(y.Value));
                    foreach (var v in activeObjNameLstPerFrame)
                    {
                        print("active obj name:" + v.Key + "    time:  " + v.Value * 1000 + " ms");
                    }
                }

                lastRealTime = Time.realtimeSinceStartup;
                currFrameActiveCostTime = 0;

                activeObjNameLstPerFrame.Clear();
                currFrame = Time.frameCount;
            }
        }
    }
#endif

    public static void LogOneObjActive(string name, float time)
    {
        currFrameActiveCostTime += time;
        activeObjNameLstPerFrame.Add(new KeyValuePair<string, float>(name, time));
    }
}