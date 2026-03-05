using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(2,5,1)]
[TrackClipType(typeof(WwiseAudioTrackAsset))]
[TrackBindingType(typeof(MonoBehaviour))]
public class WwiseAudioTrack : PlayableTrack
{
    
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return base.CreateTrackMixer(graph, go, inputCount);
    }

    protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
    {
        return base.CreatePlayable(graph, gameObject, clip);
    }
}
    
