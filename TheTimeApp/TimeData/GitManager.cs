using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace TheTimeApp.TimeData
{
    public class GitManager : IDisposable
    {
        private readonly Repository _repo;
        
        private readonly AppSettings _settings;

        private readonly string _url;
        
        private static GitManager _instance;

        public static GitManager Instance => _instance ?? (_instance = new GitManager());

        public GitManager()
        {
            _settings = AppSettings.Instance;
            
            _repo = new Repository(_settings.GitRepoPath);
            
            _url = _repo.Network.Remotes.First().Url;
        }

        /// <summary>
        /// Returns list of type Commit of all
        /// commits of current user.
        /// </summary>
        /// <returns></returns>
        public List<GitCommit> Commits()
        {
            List<GitCommit> commits = new List<GitCommit>();

            try
            {
                foreach (Commit commit in _repo.Commits)
                {
                    try
                    {
                        if (commit.Author.Name.Contains(AppSettings.Instance.GitUserName)) 
                            commits.Add(ToGitCommit(commit));
                    }
                    catch (Exception)
                    {
                        // eat it
                    }
                }
            }
            catch (Exception)
            {
                // eat it
            }

            return commits;
        }

        /// <summary>
        /// Returns list of type Commit of all
        /// commits of current user on given date.
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public List<GitCommit> CommitsOnDate(DateTime datetime)
        {
            List<GitCommit> result = new List<GitCommit>();
            var commits = Commits();
            try
            {
                foreach (GitCommit commit in commits)
                {
                    try
                    {
                        if (TimeServer.DateString(commit.Date) == TimeServer.DateString(datetime))
                            result.Add(commit);
                    }
                    catch (Exception)
                    {
                        // eat it
                    }
                }
            }
            catch (Exception)
            {
                // eat it
            }
            return result;
        }

        /// <summary>
        /// Returns list of type string of all commit
        /// messages of current user.
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public List<string> Messages()
        {
            var result = new List<string>();
            var commits = Commits();
            foreach (GitCommit commit in commits)
            {
                try
                {
                    result.Add(commit.Message);
                }
                catch (Exception)
                {
                    // eat it
                }
            }

            return result;
        }

        /// <summary>
        /// Returns list of type string of all
        /// commit messages of current user on given date.
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public List<string> MessagesOnDate(DateTime datetime)
        {
            var result = new List<string>();
            var commits = CommitsOnDate(datetime);
            foreach (GitCommit commit in commits)
            {
                try
                {
                    result.Add(commit.Message);
                }
                catch (Exception e)
                {
                    // eat it
                }
            }

            return result;
        }

        public List<Branch> Branches(Commit commit)
        {
            return _repo.Branches.Where(b => b.Commits.Contains(commit)).ToList();
        }

        public GitCommit ToGitCommit(Commit commit)
        {
            GitCommit gitCommit = new GitCommit(commit.Committer.Name, commit.Committer.When.DateTime, commit.Message, commit.Id.ToString())
            {
                Url = _url
            };
            return gitCommit;
        }

        public void Dispose()
        {
            _repo.Dispose();
        }
    }
}