using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiniJSON2;

namespace MuffinGitLab {
    public sealed class CommitsClient {
        
        [Serializable]
        public class CommitData {
            public string id;
            public string created_at;
            public string title;
            public string message;
            
            public string author_name;
            public string author_email;
            public string authored_date;
            
            public string committer_name;
            public string committer_email;
            public string committed_date;
            public string[] parent_ids;
        }
        
        [Serializable]
        public class CommitRefData {
            public string type;
            public string name;
        }
        
        private HttpRequestFacade facade;
        public CommitsClient(HttpRequestFacade _facade) {
            facade = _facade;
        }

        public async Task<List<CommitData>> GetCommits(string projectId, string branch, int page, int per_page) {
            Dictionary<string, string> sendData = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(branch)) {
                sendData["all"] = "true";
            } else {
                sendData["ref_name"] = branch;
            }
            var respData = await facade.DoRequest<List<CommitData>>($"projects/{projectId}/repository/commits", page: page, per_page: per_page, sendData: sendData);
            return respData.data;
        }

        public async Task<List<CommitRefData>> GetCommitRefData(string projectId, string hash) {
            var respData = await facade.DoRequest<List<CommitRefData>>($"projects/{projectId}/repository/commits/{hash}/refs");
            return respData.data;
        }

        public async Task<CommitData> CherryPickCommit(string projectId, string toBranch, string commitHash) {
            Dictionary<string, string> sendData = new Dictionary<string, string>() {
                { "branch", toBranch }
            };
            var respData = await facade.DoRequest<CommitData>($"projects/{projectId}/repository/commits/{commitHash}/cherry_pick", Method.Post, sendData: sendData);
            return respData.data;
        }
        
        public async Task<List<CommitData>> CherryPickCommits(string projectId, string toBranch, List<string> hashes) {
            List<CommitData> result = new List<CommitData>();
            foreach (string commit in hashes) {
                result.Add(await CherryPickCommit(projectId, toBranch, commit));
            }
            return result;
        }
    }
}