using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MuffinGitLab;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class GitLabEditor : EditorWindow {
    [Serializable]
    public class GitLabProjectEditorConfig {
        public string ProjectID;
        public string ProjectName;
        
        public List<string> CherryPickFromBranches;
        public List<string> CherryPickToBranches;
    }
    
    [Serializable]
    public class GitLabEditorConfig {
        public string GitLabHost;
        public string DefaultCommitsInDays;

        public List<GitLabProjectEditorConfig> Projects;
    }
    
    public static string gitAddress;
    private static string defaultCommitsInDays;
    
    private static string defaultCherryPickFromBranch = "2023-11-28-Unity2021-3-13";
    private readonly static string defaultCherryPickToBranch = "archive/branch/tw/tw-obt-design-2024-1-25";
    
    
    public static string tokenSaveKey = "gitlabtoken";
    private static int perPage = 100;

    private static GitLabEditorConfig gitlabConfig;
    private static GitLabClient client;
    private static int currentCommitPage = 1;
    private static List<CommitsClient.CommitData> allCommits;
    private static ListView currentListView;

    private static DropdownField fromBranchNameDropDown;
    // private static TextField searchInputField;
    private static VisualElement searchInputFieldsView;
    private static TextField commitsInDaysInputField;
    private static DropdownField toBranchNameDropDown;
    
    private static List<CommitsClient.CommitData> selected;
    
    private static string selectedProjectId = null;
    private static string selectedBranchNameFrom = null;
    private static string selectedBranchNameTo = null;
    private static bool haveNoMore = false;

    private static Dictionary<string, List<CommitsClient.CommitRefData>> commitRefDataCache;


    [MenuItem("GitLab/GitLabEditor")]
    public static void ShowExample() {
        GitLabEditor wnd = GetWindow<GitLabEditor>();
        wnd.titleContent = new GUIContent("GitLabEditor");
    }

    private void OnInspectorUpdate() {

    }

    private VisualElement GetLoginView() {
        VisualElement loginView = new VisualElement();
        var dropDownList = gitlabConfig.Projects.Select(project => $"{project.ProjectName}-{project.ProjectID}").ToList();
        var projectDropDown = new DropdownField() {
            label = "选择项目", 
            choices = dropDownList,
        };
        projectDropDown.RegisterValueChangedCallback(evt => {
            var selectedProject = gitlabConfig.Projects[dropDownList.IndexOf(evt.newValue)];
            if (selectedProject != null) {
                selectedProjectId = selectedProject.ProjectID;
            }
        });
        
        loginView.Add(projectDropDown);

        var tokensView = new VisualElement();
        tokensView.Add(new Label($"输入[{gitAddress}]token:"));
        var tokenInputField = new TextField();
        if (PlayerPrefs.HasKey(tokenSaveKey)) {
            tokenInputField.value = PlayerPrefs.GetString(tokenSaveKey);
        }

        tokensView.Add(tokenInputField);
        var getTokenButton = new Button {
            text = "获取token..."
        };

        void GetToken() {
            Application.OpenURL($"{gitAddress}/-/user_settings/personal_access_tokens");
        }

        getTokenButton.RegisterCallback<ClickEvent>(evt => { GetToken(); });
        tokensView.Add(getTokenButton);
        loginView.Add(tokensView);

        var loginButton = new Button {
            text = "登录..."
        };
        loginButton.RegisterCallback<ClickEvent>(async evt => {
            string token = tokenInputField.value.Trim();
            if (string.IsNullOrEmpty(token)) {
                int choose = EditorUtility.DisplayDialogComplex("请输入Gitlab Token", "是否前往获取Token", "知道了", "取消", "前往获取Token");
                if (choose == 2) {
                    GetToken();
                }
            } else {
                string baseUrl = gitAddress + "/api/v4";
                client = new GitLabClient(baseUrl, token);
                bool result = await client.Login(selectedProjectId);

                if (result) {
                    // Debug.Log("登录成功!");
                    PlayerPrefs.SetString(tokenSaveKey, token);
                    loginView.RemoveFromHierarchy();
                    rootVisualElement.Add(GetCommitsView());
                } else {
                    EditorUtility.DisplayDialog("登陆失败!", "请检查是否拥有项目权限", "知道了", "取消");
                }
            }
        });
        loginView.Add(loginButton);
        
        // default
        projectDropDown.index = 0;
        selectedProjectId = gitlabConfig.Projects[0].ProjectID;
        
        return loginView;
    }

    private async Task BindItem(CommitBriefInfoVisualElement elem, int i) {
        var titleLabel = elem.Q<Label>(name: "title");
        var hashIdLabel = elem.Q<Label>(name: "hash_id");
        var authorLabel = elem.Q<Label>(name: "author");
        var emailLabel = elem.Q<Label>(name: "email");
        var timeLabel = elem.Q<Label>(name: "time");
        var refLabel = elem.Q<Label>(name: "ref");
        var selectedToggle = elem.Q<Toggle>(name: "selected");

        var info = allCommits[i];
        titleLabel.text = info.title;
        hashIdLabel.text = info.id;
        authorLabel.text = info.author_name;
        emailLabel.text = info.author_email;
        timeLabel.text = info.authored_date;
        selectedToggle.value = selected.Contains(info);
        selectedToggle.RegisterValueChangedCallback(evt => {
            var contains = selected.Contains(info);
            if (evt.newValue) {
                if (!contains) {
                    selected.Add(info);
                }
            } else {
                if (contains) {
                    selected.Remove(info);
                }
            }
        });

        var key = $"{selectedProjectId}-{info.id}";
        var refData = commitRefDataCache.TryGetValue(key, out var cachedRefValue) ? cachedRefValue : await client.commitsClient.GetCommitRefData(selectedProjectId, info.id);
        commitRefDataCache[key] = refData;

        var refString = string.Empty;
        var refCount = 0;
        if (refData != null)
            foreach (CommitsClient.CommitRefData refd in refData) {
                // if (++refCount <= 3) {
                refString += $"\n{refd.type}:{refd.name}";
                // }
            }

        refLabel.text = refString;
    }

    void onNewCommitsGet(List<CommitsClient.CommitData> commits) {
        var inputs = searchInputFieldsView.Query<TextField>("searchInputField").ForEach(field => field.value.ToLower());
        
        // var keyword = searchInputField.text.ToLower();
        // commits.Reverse();
        commits.Sort((commit1, commit2) => {
            if (DateTime.TryParse(commit1.authored_date, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date1) 
                && DateTime.TryParse(commit2.authored_date, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date2)) {
                return date2.CompareTo(date1);
            }

            return 0;
        });

        allCommits.AddRange(inputs.Count == 0
            ? commits
            : commits.FindAll(data => inputs.Any(s => data.title.ToLower().Contains(s))));
    }

    void resetStatus() {
        allCommits.Clear();
        selected.Clear();
        commitRefDataCache.Clear();
        haveNoMore = false;
        currentCommitPage = 1;
    }

    void selectAll() {
        selected.Clear();
        allCommits.ForEach(data => selected.Add(data));

        currentListView.Clear();
        currentListView.Rebuild();
    }

    void deSelectAll() {
        selected.Clear();
        currentListView.Clear();
        currentListView.Rebuild();
    }

    async Task startSearch() {
        resetStatus();
        var currentDateTime = DateTime.Now;

        var searchLayout = rootVisualElement.Q<VisualElement>("searchLayout");
        var loadMoreButton = rootVisualElement.Q<VisualElement>("loadMoreButton");
        var cherryPickLayout = rootVisualElement.Q<VisualElement>("cherryPickLayout");
        searchLayout.SetEnabled(false);
        loadMoreButton.SetEnabled(false);
        cherryPickLayout.SetEnabled(false);

        int commitsInDays = Convert.ToInt32(commitsInDaysInputField.value);
        while (true) {
            if ((DateTime.Now - currentDateTime).TotalMinutes >= 3) {
                break;
            }
            var commits = await client.commitsClient.GetCommits(selectedProjectId, selectedBranchNameFrom, currentCommitPage, perPage);
            if (commits != null) {
                onNewCommitsGet(commits);
                
                if (commits.Count >= perPage) {
                    currentCommitPage++;
                    var latestCommit = commits[1];
                    if (DateTime.TryParse(latestCommit.authored_date, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDate)) {
                        // Debug.Log($"{commits.Count}, {parsedDate.ToString(CultureInfo.InvariantCulture)}");
                        if ((currentDateTime - parsedDate).TotalDays >= commitsInDays ) {
                            break;
                        }
                    }
                } else {
                    haveNoMore = true;
                    // reach end
                    break;
                }
            } else {
                // Debug.Log("-------------------->");
                // no commits
                // break;
            }
        }
        
        currentListView.Rebuild();
        searchLayout.SetEnabled(true);
        loadMoreButton.SetEnabled(true);
        cherryPickLayout.SetEnabled(true);

        selectAll();
    }

    private VisualElement GetCommitsView() {
        resetStatus();

        Func<VisualElement> makeItem = () => {
            var commitInfoVisualElement = new CommitBriefInfoVisualElement();
            return commitInfoVisualElement;
        };

        Action<VisualElement, int> bindItem = async (e, i) => await BindItem(e as CommitBriefInfoVisualElement, i);

        VisualElement view = new VisualElement {
            style = {
                flexDirection = FlexDirection.Column,
                paddingLeft = 15f,
                paddingRight = 15f
            }
        };
        var searchLayout = new VisualElement {
            style = {
                flexDirection = FlexDirection.Column,
                paddingLeft = 15f,
                paddingRight = 15f
            },
            name = "searchLayout"
        };

        var searchInputLayout = new VisualElement() {
            style = {
                flexDirection = FlexDirection.Row,
                // paddingLeft = 15f,
                // paddingRight = 15f
            },
        };
        
        var fromDropDownList = gitlabConfig.Projects
            .Find(projectConfig => projectConfig.ProjectID == selectedProjectId)
            .CherryPickFromBranches.ToList();
        fromBranchNameDropDown =  new DropdownField() {
            label = "从哪个分支:", 
            choices = fromDropDownList,
        };
        fromBranchNameDropDown.RegisterValueChangedCallback(evt => {
            selectedBranchNameFrom = fromDropDownList[fromDropDownList.IndexOf(evt.newValue)];
        });
        fromBranchNameDropDown.index = 0;
        selectedBranchNameFrom = fromDropDownList[0];
        
        // searchInputField = new TextField {
        //     style = {
        //         flexGrow = 0.1f
        //     }
        // };
        
        var daysNameLabel = new Label { text = "多少天之内:" };
        commitsInDaysInputField = new TextField {
            style = {
                flexGrow = 0.1f
            },
            value = defaultCommitsInDays
        };
        
        searchInputLayout.Add(fromBranchNameDropDown);
        // searchInputLayout.Add(keywordNameLabel);
        // searchInputLayout.Add(searchInputField);
        searchInputLayout.Add(daysNameLabel);
        searchInputLayout.Add(commitsInDaysInputField);
        
        searchInputFieldsView = new SearchInputFieldsView();

        var searchButtonsLayout = new VisualElement() {
            style = {
                flexDirection = FlexDirection.Row,
            },
        };
        var searchButton = new Button { text = "搜索...", name = "search_button" };
        searchButton.RegisterCallback<ClickEvent>(async evt => { await startSearch(); });

        var selectAllButton = new Button { text = "选择所有" };
        selectAllButton.RegisterCallback<ClickEvent>(async evt => {
            selectAll();
        });

        var clearSelectedButton = new Button { text = "清除所有选择" };
        clearSelectedButton.RegisterCallback<ClickEvent>(async evt => {
            deSelectAll();
        });

        var goBackButton = new Button() { text = "返回" };
        goBackButton.RegisterCallback<ClickEvent>(evt => {
            rootVisualElement.Clear();
            rootVisualElement.Add(GetLoginView());
        });
        
        searchButtonsLayout.Add(searchButton);
        searchButtonsLayout.Add(selectAllButton);
        searchButtonsLayout.Add(clearSelectedButton);
        searchButtonsLayout.Add(goBackButton);
        
        searchLayout.Add(searchInputLayout);
        searchLayout.Add(searchInputFieldsView);
        searchLayout.Add(searchButtonsLayout);

        view.Add(searchLayout);

        var listLayout = new VisualElement {
            style = {
                flexShrink = 100f,
                flexDirection = FlexDirection.Column,
                paddingLeft = 15f,
                paddingRight = 15f,
                flexGrow = 0f
            }
        };
        currentListView = new ListView(allCommits, 45, makeItem, bindItem) {
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
        };
        var loadMoreButton = new Button { text = "加载更多...", name = "loadMoreButton" };
        loadMoreButton.RegisterCallback<ClickEvent>(async evt => {
            if (haveNoMore) {
                EditorUtility.DisplayDialog("找不到更多提交了...", "找不到更多提交了...", "知道了");
                return;
            }

            var commits = await client.commitsClient.GetCommits(selectedProjectId, selectedBranchNameFrom, currentCommitPage, perPage);
            if (commits.Count == perPage) {
                currentCommitPage++;
            } else if (commits.Count < perPage) {
                haveNoMore = true;
            }

            onNewCommitsGet(commits);
            currentListView.Rebuild();
            selectAll();
        });

        listLayout.Add(currentListView);
        listLayout.Add(loadMoreButton);
        view.Add(listLayout);

        var cherryPickLayout = new VisualElement {
            style = {
                flexDirection = FlexDirection.Row,
                paddingLeft = 15f,
                paddingRight = 15f
            },
            name = "cherryPickLayout"
        };
        
        var toDropDownList = gitlabConfig.Projects
            .Find(projectConfig => projectConfig.ProjectID == selectedProjectId)
            .CherryPickToBranches.ToList();
        toBranchNameDropDown =  new DropdownField() {
            label = "到哪个分支:", 
            choices = toDropDownList,
        };
        toBranchNameDropDown.RegisterValueChangedCallback(evt => {
            selectedBranchNameTo = toDropDownList[toDropDownList.IndexOf(evt.newValue)];
        });
        toBranchNameDropDown.index = 0;
        selectedBranchNameTo = toDropDownList[0];
        
        var confirmCherryPickButton = new Button() { text = "开始CherryPick..." };
        confirmCherryPickButton.RegisterCallback<ClickEvent>(async evt => {
            if (selected.Count == 0) {
                EditorUtility.DisplayDialog("所选提交数量为0", "所选提交数量为0", "知道了");
                return;
            }
            
            if (!EditorUtility.DisplayDialog("是否进行Cherry Pick?", $"是否Cherry Pick所选提交到{selectedBranchNameTo}?", "是", "否")) {
                return;
            }

            var branchInfo = await client.branchesClient.GetBranchInfo(selectedProjectId, selectedBranchNameTo);
            if (branchInfo != null) {
                selected.Sort((data1, data2) => allCommits.IndexOf(data2).CompareTo(allCommits.IndexOf(data1)));

                bool allCompleted = true;
                foreach (CommitsClient.CommitData commit in selected) {
                    if (commit.parent_ids != null && commit.parent_ids.Length > 1) {
                        var message = $"{commit.id}-{commit.title} 是一条Merge, 跳过...";
                        Debug.Log(message);
                        continue;
                    }
                    
                    var refData = await client.commitsClient.GetCommitRefData(selectedProjectId, commit.id);
                    var key = $"{selectedProjectId}-{commit.id}";
                    commitRefDataCache[key] = refData;
                    
                    if (refData.Find(refBranch => refBranch.name == branchInfo.name) != null) {
                        var message = $"{commit.id}-{commit.title} 已经在 {branchInfo.name} 中...";
                        Debug.Log(message);
                    } else {
                        var message = $"Cherry picking {commit.id}-{commit.title} 到 {branchInfo.name}";
                        var newCommitInfo = await client.commitsClient.CherryPickCommit(selectedProjectId, branchInfo.name, commit.id);
                        
                        allCompleted = allCompleted && newCommitInfo != null;
                        if (newCommitInfo == null) {
                            Debug.LogError($"{message} 失败, 请手动处理!");
                        
                            // break;
                        } else { 
                            Debug.Log($"{message} 成功!");
                        }
                    }
                }

                Debug.Log("Cherry pick 结束!");

                if (!allCompleted) {
                    EditorUtility.DisplayDialog("Cherry pick 结束!", "Cherry pick 结束! 有部分提交未完成cherry pick, 请查看日志。", "知道了");
                } else {
                    EditorUtility.DisplayDialog("Cherry pick 完成!", "Cherry pick 完成!", "知道了");
                }

                await startSearch();
            } else {
                EditorUtility.DisplayDialog("分支不存在", "请检查分支名", "知道了");
            }
        });

        cherryPickLayout.Add(toBranchNameDropDown);
        cherryPickLayout.Add(confirmCherryPickButton);
        view.Add(cherryPickLayout);

        return view;
    }

    void loadConfig() {
        gitlabConfig = JsonUtility.FromJson<GitLabEditorConfig>(Resources.Load<TextAsset>("GitLabEditorConfig").text);
        gitAddress = gitlabConfig.GitLabHost;
        defaultCommitsInDays = gitlabConfig.DefaultCommitsInDays;
    }
    
    public void OnEnable() {
        loadConfig();
        
        allCommits = new List<CommitsClient.CommitData>();
        selected = new List<CommitsClient.CommitData>();
        commitRefDataCache = new Dictionary<string, List<CommitsClient.CommitRefData>>();

        VisualElement root = rootVisualElement;
        VisualElement loginView = GetLoginView();
        root.Add(loginView);
    }
    
    public class SearchInputFieldsView : VisualElement {
        public SearchInputFieldsView() {
            var root = new VisualElement {
                style = {
                    paddingTop = 3f,
                    paddingRight = 0f,
                    paddingBottom = 3f,
                    paddingLeft = 3f,
                    flexDirection = FlexDirection.Column
                }
            };
            
            var inputsView = new VisualElement();
            var addButton = new Button() {
                name = "add", text = "点击添加搜索关键字(可以添加多个)"
            };
            addButton.RegisterCallback<ClickEvent>(evt => root.Add(new SearchInputFieldVisualElement()));
            inputsView.Add(addButton);
            inputsView.Add(new SearchInputFieldVisualElement());
            
            root.Add(inputsView);
            Add(root);
        }
    }

    public class SearchInputFieldVisualElement : VisualElement {
        public SearchInputFieldVisualElement() {
            var root = new VisualElement {
                style = {
                    paddingTop = 3f,
                    paddingRight = 0f,
                    paddingBottom = 3f,
                    paddingLeft = 3f,
                    borderBottomColor = Color.gray,
                    borderBottomWidth = 1f
                }
            };
            
            var hContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    // paddingLeft = 15f,
                    // paddingRight = 15f
                }
            };
            hContainer.Add(new TextField() {
                name = "searchInputField",
                style = {
                    flexGrow = 1f
                },
                // value = "   "
            });
            var deleteButton = new Button() {
                name = "delete",
                text = "x",
            };
            deleteButton.RegisterCallback<ClickEvent>(evt => RemoveFromHierarchy());
            hContainer.Add(deleteButton);
            
            root.Add(hContainer);
            Add(root);
        }

        public string GetInputText() {
            return this.Q<TextField>("input").value.Trim();
        }
    }
    
    public class CommitBriefInfoVisualElement : VisualElement {
        public CommitBriefInfoVisualElement() {
            var root = new VisualElement {
                style = {
                    paddingTop = 3f,
                    paddingRight = 0f,
                    paddingBottom = 3f,
                    paddingLeft = 3f,
                    borderBottomColor = Color.gray,
                    borderBottomWidth = 1f
                }
            };

            var hContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 15f,
                    paddingRight = 15f
                }
            };
            var selectToggle = new Toggle { name = "selected" };
            hContainer.Add(selectToggle);

            var titleLabel = new Label {
                name = "title",
                style = {
                    fontSize = 14f
                }
            };
            hContainer.Add(titleLabel);

            var infoContainer = new VisualElement() {
                style = {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 15f,
                    paddingRight = 15f
                }
            };
            infoContainer.Add(new Label() { name = "hash_id" });
            
            var authorContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 0f,
                    paddingRight = 15f
                }
            };
            authorContainer.Add(new Label() { name = "author" });
            authorContainer.Add(new Label() { name = "email" });
            authorContainer.Add(new Label() { name = "time" });
            
            infoContainer.Add(authorContainer);

            var refContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 15f,
                    paddingRight = 15f
                }
            };
            refContainer.Add(new Label() { name = "ref" });

            var vContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 15f,
                    paddingRight = 15f
                }
            };

            root.Add(hContainer);
            vContainer.Add(infoContainer);
            vContainer.Add(refContainer);
            root.Add(vContainer);
            Add(root);
        }
    }
}