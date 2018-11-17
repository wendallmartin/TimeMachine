using System;

namespace TheTimeApp.TimeData
{
    /// <summary>
    /// `Committer` TEXT, `Date` TEXT, `Message` TEXT, `Branch` Text, `Url` VARCHAR(150), `Id` VARCHAR(100)
    /// </summary>
    public class GitCommit
    {
        private string _committer;

        private DateTime _date;

        private string _message;

        private string _id;
        
        public string Committer => _committer;
        
        public DateTime Date => _date;

        public string Message => _message;
        
        public string Id => _id;

        public string Branch { get; set; } = "";

        public string Url { get; set; } = "";
        
        public GitCommit(string committer, DateTime date, string message, string id)
        {
            _committer = committer;
            _date = date;
            _message = message;
            _id = id;
        }

        public bool Equals(GitCommit commit)
        {
            if (_committer != commit.Committer) return false;

            if (_date.ToString() != commit.Date.ToString()) return false;

            if (_message != commit._message) return false;
            
            if (Branch != commit.Branch) return false;
            
            if (Url != commit.Url) return false;

            if (_id != commit.Id) return false;

            return true;
        }
    }
}