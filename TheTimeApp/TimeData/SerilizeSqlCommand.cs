using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;

namespace TheTimeApp.TimeData
{
    [Serializable]
    public class SerilizeSqlCommand : IDisposable
    {
        public enum CommandType
        {
            InsertTime,
            DeleteTime,
            UpdateTime,
            UpdateDetails,
        }
        
        public CommandType Type { get; set; }
        
        public List<KeyValuePair<string, string>> Params = new List<KeyValuePair<string, string>>();
        
        public string CommandText;
        
        public DateTime CommandAddTime { get; set; }

        public SerilizeSqlCommand(string text)
        {
            CommandText = text;
        }

        public void AddParameter(SqlParameter param)
        {
            Params.Add(new KeyValuePair<string, string>(param.ParameterName,param.Value.ToString()));
        }

        public SqlCommand GetSqlCommand
        {
            get{
                SqlCommand command = new SqlCommand();
                command.CommandText = CommandText;
                foreach (var param in Params)
                {
                    command.Parameters.Add(new SqlParameter(){ParameterName = param.Key, Value = param.Value});    
                }

                return command;
            }
        }

        public void Dispose()
        {
            
        }
    }
}