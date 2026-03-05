using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SAGA.Editor
{
    public static class EditorHelper
    {
        [MenuItem("Edit/ReEnterPlaymode &s")]
        public static void ReEnter()
        {
            if (EditorApplication.isPlaying)
            {
                void OnChanged(PlayModeStateChange change)
                {
                    if (change != PlayModeStateChange.EnteredEditMode) return;
                    EditorApplication.playModeStateChanged -= OnChanged;
                    AssetDatabase.Refresh();
                    EditorApplication.EnterPlaymode();
                }

                EditorApplication.ExitPlaymode();
                EditorApplication.playModeStateChanged += OnChanged;
                return;
            }

            EditorApplication.EnterPlaymode();
        }

        public static void ShowDialog(string content, Action okFunc = null, Action cancelFunc = null, string title = "Info", string ok = "Ok", string cancel = "Cancel")
        {
            var ret = EditorUtility.DisplayDialog(title, content, ok, cancel);
            if (ret)
            {
                okFunc?.Invoke();
            }
            else
            {
                cancelFunc?.Invoke();
            }
        }

        public static bool ShowProgressBar(string content, float progress, bool cancelable = true, string title = "Info")
        {
            if (cancelable)
            {
                var state = EditorUtility.DisplayCancelableProgressBar(title, content, progress);
                return state;
            }

            EditorUtility.DisplayProgressBar(title, content, progress);
            return false;
        }

        public static void SetSymbol(BuildTargetGroup targetGroup, string symbol, bool state)
        {
            var symbolsString = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var symbols = new List<string>();
            if (!string.IsNullOrEmpty(symbolsString))
            {
                var splitSymbols = symbolsString.Split(';');
                symbols.AddRange(splitSymbols);
            }

            if (state)
            {
                symbols.Add(symbol);
            }
            else
            {
                symbols.Remove(symbol);
            }

            var symbolsArr = symbols.Distinct().ToArray();
            var newSymbolArr = string.Join(";", symbolsArr);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newSymbolArr);
        }
    }
}