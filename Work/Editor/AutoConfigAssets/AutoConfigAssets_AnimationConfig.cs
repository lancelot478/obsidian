using System;
using System.Collections.Generic;
using UnityEditor;

namespace SAGA.Editor
{
    public static class AutoConfigAssets_AnimationConfig
    {
        //右脚脚印事件 int类型参数为右脚踩到地面的那一帧的帧数
        public static List<KeyValuePair<string, int>> rightFootEventsLst = new()
        {
            new("Swordman_F", 3),
            new("Swordman_M", 3),
            new("Ranger_M", 13),
            new("Ranger_F", 13),
            new("Magician_M", 3),
            new("Magician_F", 4),
            new("Assassin_M", 3),
            new("Assassin_F", 3),
            new("Reverend_M", 3),
            new("Reverend_F", 3),
            //坐骑脚印
            new("walk_ride", 3),
        };

        //左脚脚印事件 int类型参数为左脚踩到地面的那一帧的帧数
        public static List<KeyValuePair<string, int>> leftFootEventsLst = new()
        {
            new("Swordman_F", 13),
            new("Swordman_M", 13),
            new("Ranger_M", 3),
            new("Ranger_F", 3),
            new("Magician_M", 13),
            new("Magician_F", 14),
            new("Assassin_M", 13),
            new("Assassin_F", 13),
            new("Reverend_M", 13),
            new("Reverend_F", 13),
            //坐骑脚印
            new("walk_ride", 13),
        };
    }
}