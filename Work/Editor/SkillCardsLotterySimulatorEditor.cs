using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SkillCardsLotterySimulatorEditor : EditorWindow {

    enum CardQuality {
        Green = 2,
        Blue = 3,
        Purple = 4,
        Orange = 5,
    }

    struct LotteryResult {
        public CardQuality quality;
        public int count;
    }

    struct QualityInfo {
        public string name;
        public Color color;
    }

    private Dictionary<CardQuality, QualityInfo> qualityInfoMap = new Dictionary<CardQuality, QualityInfo>() {
        {CardQuality.Green, new QualityInfo() { name = "绿色", color = Color.green}},
        {CardQuality.Blue, new QualityInfo() { name = "蓝色", color = Color.blue}},
        {CardQuality.Purple, new QualityInfo() { name = "紫色", color = Color.magenta}},
        {CardQuality.Orange, new QualityInfo() { name = "橙色", color = Color.red}},
    };


    private Vector2 resultsScrollPostion = Vector2.zero;
    private bool resultDetailVisible = false;

    private List<LotteryResult> results;

    private void OnEnable() {
        resultsScrollPostion = Vector2.zero;
        resultDetailVisible = false;
        results = new List<LotteryResult>() {
            new LotteryResult() {quality = CardQuality.Blue, count = 100},
            new LotteryResult() {quality = CardQuality.Orange, count = 200},
        };
    }

    private void OnGUI() {
        using (new EditorGUILayout.HorizontalScope(new GUIStyle())) {
            
            // left
            using (new EditorGUILayout.VerticalScope(new GUIStyle())) {
                using (new EditorGUILayout.HorizontalScope(new GUIStyle())) {
                    using (new EditorGUILayout.VerticalScope(new GUIStyle())) {
                        EditorGUILayout.TextField("输入卡池id:", String.Empty);
                        EditorGUILayout.TextField("输入抽卡次数:", String.Empty);
                    }
                    // mid
                    GUILayout.Button("开抽！");
                    using (new EditorGUILayout.VerticalScope(new GUIStyle())) {
                        GUILayout.Button("清除记录");
                        GUILayout.Button("一键导出");
                    }
                }

                using (new EditorGUILayout.VerticalScope(new GUIStyle())) {
                    using (new EditorGUILayout.HorizontalScope(new GUIStyle())) {
                        EditorGUILayout.LabelField("品质");
                        EditorGUILayout.LabelField("抽取数量");
                        EditorGUILayout.LabelField("数量占比");
                        EditorGUILayout.Separator();
                    }

                    using (var scrollView = new EditorGUILayout.ScrollViewScope(resultsScrollPostion)) {
                        resultsScrollPostion = scrollView.scrollPosition;
                        
                        // todo
                        results.Sort((result1, result2) => result1.quality.CompareTo(result2.quality));
                        int totalCount = results.Sum(result => result.count);
                        
                        foreach (var lotteryResult in results) {
                            var qualityInfo = qualityInfoMap[lotteryResult.quality];
                            using (new EditorGUILayout.HorizontalScope()) {
                                EditorGUILayout.LabelField(qualityInfo.name);
                                EditorGUILayout.LabelField(lotteryResult.count.ToString());
                                EditorGUILayout.LabelField((lotteryResult.count * 1.0f / totalCount).ToString("P"));
                            }
                        }
                    }
                }
            }

            if (GUILayout.Button("result")) {
                resultDetailVisible = !resultDetailVisible;
            }
            using (var group = new EditorGUILayout.FadeGroupScope(resultDetailVisible ? 1 : 0)) {
                if (group.visible) {
                    EditorGUILayout.Separator();
                    // right
                    using (new EditorGUILayout.VerticalScope(new GUIStyle())) {
                        EditorGUILayout.LabelField("抽卡详情");

                        using (new EditorGUILayout.HorizontalScope(new GUIStyle())) {
                            EditorGUILayout.LabelField("技能卡id");
                            EditorGUILayout.LabelField("技能卡名称");
                            EditorGUILayout.LabelField("数量");
                            EditorGUILayout.LabelField("占比");
                            EditorGUILayout.Separator();
                        }
                        
                        // todo
                    }
                }
            }
        }
        
    }

    [MenuItem(("Tools/SkillCardsLotterySimulator"))]
    public static void OpenSimulatorWindow() {
        SkillCardsLotterySimulatorEditor window = GetWindow<SkillCardsLotterySimulatorEditor>(false, "SkillCardsLotterySimulator");
        window.Show();
    }
}