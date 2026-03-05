using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiniJSON2;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace MuffinGitLab {
    public sealed class BranchesClient {
        [Serializable]
        public class BranchData {
            public string name;
        }

        private HttpRequestFacade facade;
        
        public BranchesClient(HttpRequestFacade _facade) {
            facade = _facade;
        }

        public async Task<List<BranchData>> GetBranches(string id) {
            var resp = await facade.DoRequest<List<BranchData>>($"projects/{id}/repository/branches");
            return resp.data;
        }
        
        public async Task<BranchData> GetBranchInfo(string id, string branchName) {
            var resp = await facade.DoRequest<BranchData>($"projects/{id}/repository/branches/{UnityWebRequest.EscapeURL(branchName)}");
            return resp.data;
        }
    }
}