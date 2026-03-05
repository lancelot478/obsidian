using System.Collections;
using System.Collections.Generic;
using SLua;
using UnityEngine;
using UnityEngine.Playables;

public class WwiseAudioTrackBehaviour : PlayableBehaviour
{
    public string wwiseEventStart;
    public string wwiseEventStop;
    private bool isPlaying = false;
    private bool canStop = false;

    private LuaTable table;
    public LuaFunction startFunc, endFunc;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        base.OnBehaviourPlay(playable, info);
        isPlaying = true;
        canStop = false;
        if (!string.IsNullOrEmpty(wwiseEventStart))
        {
            startFunc?.call(table, wwiseEventStart);
            wwiseEventStart = null;
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        base.OnBehaviourPause(playable, info);
        if (canStop)
        {
            if (!string.IsNullOrEmpty(wwiseEventStop))
            {
                endFunc?.call(table, wwiseEventStop);
                wwiseEventStop = null;
            }
        }
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (playable.GetDuration() - playable.GetTime() <=Time.unscaledDeltaTime && canStop==false )
        {
            canStop = true;
        }
    }

    public void RegisterLuaFunction(LuaTable table,LuaFunction startFunc,LuaFunction endFunc)
    {
        this.table = table;
        this.startFunc = startFunc;
        this.endFunc = endFunc;
    }
}
