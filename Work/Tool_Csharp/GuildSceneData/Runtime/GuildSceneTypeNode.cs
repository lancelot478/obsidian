using System;
using UnityEngine;

public class GuildSceneTypeNode : MonoBehaviour
{
    public int weight;
    public int pointType;
    public CharacterType characterType;
}

[Serializable]
public class GuildSceneTypeNodeData
{
    public int weight;
    public int pointType;
    public int characterType;
    public GuildScenePointData[][] points;
}

[Flags]
public enum CharacterType
{
    Npc = 0x1,
    MainRole = 0x2,
    Member = 0x4,
}