﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using StashClient.Objects;
using Newtonsoft.Json;

namespace StashClient
{
    public class StashRestClient
    {        
        private string ApiUrl { get; set; }
        private RestClient ApiClient { get; set; }

        public StashRestClient(RestClient restClient)
        {
            ApiClient = restClient;
        }

        public StashRestClient(string stashUrl)
        {
            stashUrl = stashUrl.EnsureEndsWithForwardSlash();
            ApiUrl = $"{stashUrl}rest/api/latest/";
            ApiClient = new RestClient(ApiUrl);
        }

        public StashRestClient(string stashUrl, string username, string password)
        {
            stashUrl = stashUrl.EnsureEndsWithForwardSlash();
            ApiUrl = $"{stashUrl}rest/api/latest/";

            ApiClient = new RestClient(ApiUrl)
            {
                Authenticator = new HttpBasicAuthenticator(username, password)
            };
        }

        public IEnumerable<Project> GetAllProjects()
        {
            var request = new RestRequest("projects", Method.GET);
            request.AddParameter("limit", "10000");
            var response = ApiClient.Execute(request);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                var projectList = StashProjectList.FromJson(response.Content);
                if(projectList.Size > 0)
                {
                    foreach(var project in projectList.Projects)
                    {
                        yield return project;
                    }
                }
            }

            yield break;            
        }

        public IEnumerable<Repository> GetAllRepositories(Project project)
        {
            var request = new RestRequest("projects/{key}/repos", Method.GET);
            request.AddUrlSegment("key", project.Key);
            request.AddParameter("limit", "1000");
            var response = ApiClient.Execute(request);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                var repositoryList = StashRepositoryList.FromJson(response.Content);
                if(repositoryList.Size > 0)
                {
                    foreach(var repo in repositoryList.Repositories)
                    {
                        yield return repo;
                    }
                }
            }

            yield break;
        }

        public IEnumerable<Branch> GetAllBranches(Repository repo)
        {
            var request = new RestRequest("projects/{key}/repos/{slug}/branches", Method.GET);
            request.AddUrlSegment("key", repo.Project.Key);
            request.AddUrlSegment("slug", repo.Slug);
            request.AddParameter("limit", "1000");
            var response = ApiClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var branchList = StashBranchList.FromJson(response.Content);
                if (branchList.Size > 0)
                {
                    foreach (var branch in branchList.Branches)
                    {
                        yield return branch;
                    }
                }
            }

            yield break;
        }

        //Size in bytes
        public double GetSize(Repository repo)
        {
            var result = 0D;
            //do not use the rest api becuase the method is not there -> therefore change the baseurl temporarly
            var temp = ApiClient.BaseUrl;
            var server = temp.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);
            ApiClient.BaseUrl = new Uri(server);

            var request = new RestRequest("/stash/projects/{key}/repos/{slug}/sizes/", Method.GET);
            request.AddUrlSegment("key", repo.Project.Key);
            request.AddUrlSegment("slug", repo.Slug);
            var response = ApiClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, double>>(response.Content);
                result = dict["repository"];
            }
            ApiClient.BaseUrl = temp;

            return result;
        }

        public IEnumerable<Tag> GetAllTags(Repository repo)
        {
            var request = new RestRequest("projects/{key}/repos/{slug}/tags", Method.GET);
            request.AddUrlSegment("key", repo.Project.Key);
            request.AddUrlSegment("slug", repo.Slug);
            request.AddParameter("limit", "1000");
            var response = ApiClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var tagList = StashTagList.FromJson(response.Content);
                if (tagList.Size > 0)
                {
                    foreach (var tag in tagList.Tags)
                    {
                        yield return tag;
                    }
                }
            }

            yield break;
        }

        public IEnumerable<string> GetFileNames(Repository repo, int limit)
        {
            var request = new RestRequest("projects/{key}/repos/{RepositoryName}/files", Method.GET);
            request.AddUrlSegment("key", repo.Project.Key);
            request.AddUrlSegment("RepositoryName", repo.Name.Replace(" ", "-"));
            request.AddParameter("limit", limit.ToString());
            request.AddParameter("at", "refs/heads/master");
            var response = ApiClient.Execute(request);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                var fileList = StashRepositoryFileList.FromJson(response.Content);
                if(fileList.Size > 0)
                {
                    foreach(var file in fileList.Files)
                    {
                        yield return file;
                    }
                }
            }

            yield break;
        }

        public IEnumerable<StashRepositoryFile> GetFiles(Repository repo, int limit)
        {
            var request = new RestRequest("projects/{key}/repos/{RepositoryName}/files", Method.GET);
            request.AddUrlSegment("key", repo.Project.Key);
            request.AddUrlSegment("RepositoryName", repo.Name.Replace(" ", "-"));
            request.AddParameter("limit", limit.ToString());
            request.AddParameter("at", "refs/heads/master");
            var response = ApiClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var fileList = StashRepositoryFileList.FromJson(response.Content);
                if (fileList.Size > 0)
                {
                    foreach (var file in fileList.Files)
                    {
                        yield return GetStashFile(repo,file);
                    }
                }
            }

            yield break;
        }

        public StashRepositoryFile GetStashFile(Repository repository, string file)
        {
            var request = new RestRequest("projects/{key}/repos/{RepositoryName}/browse/" + file, Method.GET);
            request.AddUrlSegment("key", repository.Project.Key);
            request.AddUrlSegment("RepositoryName", repository.Name.Replace(" ", "-"));
            var response = ApiClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {                
                var stashFile = StashRepositoryFile.FromJson(response.Content);
                stashFile.Path = file;
                stashFile.Repository = repository;
                return stashFile;
            }

            return null;
        }

    }
}
