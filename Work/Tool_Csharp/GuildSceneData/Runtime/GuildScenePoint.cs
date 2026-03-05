using System;
using UnityEngine;
using UnityEngine.Serialization;


public class GuildScenePoint : MonoBehaviour
{
    public float durationMin;
    public float durationMax;
    public bool moveNext;
    public bool syncRotation;
    public GuildSceneActivity[] activities;
}

[Serializable]
public class GuildScenePointData
{
    public int id;
    public string name;
    public Vec3 position;
    public Vec3? rotation;

    public int pointType;
    public float durationMin;
    public float durationMax;
    public bool moveNext;
    public GuildSceneActivity[] activities;
}

[Serializable]
public class GuildSceneActivity
{
    public GuildSceneActivityType type;
    public GuildSceneActivityTriggerType triggerType;
    public float durationMin;
    public float durationMax;
    public string param;
    public bool syncPointDuration;
}

public enum GuildSceneActivityType
{
    Anim = 1,
    Bubble = 2,
}

public enum GuildSceneActivityTriggerType
{
    Enter = 1,
    Leave = 2,
}

[Serializable]
public struct Vec3
{
    public float x;
    public float y;
    public float z;
}