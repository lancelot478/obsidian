using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowConsoleLogButton : MonoBehaviour
{
    private void Start()
    {
        var button = GetComponent<Button>();
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            var inst = ConsoleLog.Instance;
            if (inst == null) return;
            inst.IsShow = true;
        });
    }
}