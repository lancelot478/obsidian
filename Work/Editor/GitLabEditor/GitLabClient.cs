using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MiniJSON2;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace MuffinGitLab {

    [Serializable]
    public enum RespCode {
        Ok = 0,
        Error = 1,
    }
    
    [Serializable]
    public enum Method {
        Get = 0,
        Post = 1,
    }
    
    public static class UnityWebRequestExtension {
        public static TaskAwaiter<object> GetAwaiter(this UnityWebRequestAsyncOperation op) {
            var tsc = new TaskCompletionSource<object>();
            op.completed += operation => { tsc.SetResult(null); };
            return tsc.Task.GetAwaiter();
        }
    }

    public sealed class HttpRequestFacade {
        private string HostUrl;
        private string Token;

        private Dictionary<string, string> baseData;

        public HttpRequestFacade(string host, string token) {
            HostUrl = host;
            Token = token;

            baseData = new Dictionary<string, string> {
                { "PRIVATE-TOKEN", Token }
            };
        }
        
        public struct CommonResult<T> {
            public RespCode code;
            public T data;
        }

        public async Task<CommonResult<T>> DoRequest<T>(string uri, Method method = Method.Get, int page = 1, int per_page = 50, Dictionary<string, string> sendData = null) {
            string requestUrl = HostUrl + "/" + uri;
            requestUrl += $"?page={page}&per_page={per_page}&order=default";

            UnityWebRequest request = null;
            if (method == Method.Get) {
                if (sendData != null) {
                    foreach (KeyValuePair<string,string> send in sendData) {
                        requestUrl += $"&{send.Key}={UnityWebRequest.EscapeURL(send.Value)}";
                    }
                }
                request = UnityWebRequest.Get(requestUrl);
            }
            else if(method == Method.Post) {
                request = UnityWebRequest.Post(requestUrl, sendData);
            }

            if (request != null) {             
                foreach (KeyValuePair<string,string> baseInfo in baseData) {
                    request.SetRequestHeader(baseInfo.Key, baseInfo.Value);
                }
                await request.SendWebRequest();
                if (request.isDone && request.error == null) {
                    string resultString = request.downloadHandler.text;
                    
                    return new CommonResult<T>() {
                        code = RespCode.Ok,
                        data = JsonConvert.DeserializeObject<T>(resultString),
                    };
                }
                else {
                    // Debug.LogError($"Request Error:{requestUrl}, {request.error}");
                }
            }

            return new CommonResult<T>() {
                code = RespCode.Error,
                data = default,
            };;
        }
    }

    public class GitLabClient {
        private HttpRequestFacade facade = null;

        public ProjectsClient projectsClient = null;
        public BranchesClient branchesClient = null;
        public CommitsClient commitsClient = null;

        public GitLabClient(string _gitlabAddress, string _token) {
            facade = new HttpRequestFacade(_gitlabAddress, _token);

            projectsClient = new ProjectsClient(facade);
            branchesClient = new BranchesClient(facade);
            commitsClient = new CommitsClient(facade);
        }

        public async Task<bool> Login(string projectId) {
            var project = await projectsClient.GetProject(projectId);
            return project != null;
        }
    }
}
