using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiniJSON2;
using UnityEngine;
using UnityEngine.Events;

namespace MuffinGitLab {
    public sealed class ProjectsClient {
        
        [Serializable]
        public class ProjectData {
            public string id;
            public string path_with_namespace;
        }

        private HttpRequestFacade facade;
        
        public ProjectsClient(HttpRequestFacade _facade) {
            facade = _facade;
        }

        public async Task<List<ProjectData>> GetProjects() {
            var resp = await facade.DoRequest<List<ProjectData>>("projects");
            return resp.data;
        }
        
        public async Task<ProjectData> GetProject(string id) {
            var resp = await facade.DoRequest<ProjectData>($"projects/{id}");
            return resp.data;
        }
    }
}