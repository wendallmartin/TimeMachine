using System;
using System.Collections.Generic;

namespace TheTimeApp.TimeData
{
    [Serializable]
    public class User
    {
        private string _userName;

        private string _password;

        private List<Day> _days;

        public User(string user, string pass, List<Day> days)
        {
            _userName = user;
            _password = pass;
            _days = days;
        }

        public string UserName => _userName;

        public string Password => _password;

        public List<Day> Days
        {
            get => _days;
            set => _days = value;
        }
    }
}