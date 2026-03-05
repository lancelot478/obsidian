using System;
using SLua;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class WwiseAudioTrackAsset : PlayableAsset
{
    
    public string wwiseEventStart;
    public string wwiseEventStop;
    private WwiseAudioTrackBehaviour behaviour;
    private LuaTable table;
    private LuaFunction startFunc, endFunc;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<WwiseAudioTrackBehaviour>.Create(graph);
        behaviour = playable.GetBehaviour();
        if (behaviour != null)
        {
            behaviour.wwiseEventStart = wwiseEventStart;
            behaviour.wwiseEventStop = wwiseEventStop;
        }
        behaviour.RegisterLuaFunction(table,startFunc,endFunc);
        return playable;
    }

    public void RegisterLuaFunction(LuaTable table,LuaFunction startFunc,LuaFunction endFunc)
    {
        this.table = table;
        this.startFunc = startFunc;
        this.endFunc = endFunc;
    }

}