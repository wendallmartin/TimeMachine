using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using MySql.Data.MySqlClient;

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
        
        public enum SqlType
        {
            MySql,
            Azure
        }

        public SqlType Type { get; set; }
        
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
        
        public void AddParameter(MySqlParameter param)
        {
            Params.Add(new KeyValuePair<string, string>(param.ParameterName,param.Value == null ? "" : param.Value.ToString()));
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

        public MySqlCommand GetMySqlCommand
        {
            get{
                try
                {
                    MySqlCommand cmd = new MySqlCommand {CommandText = CommandText};
                    foreach (var param in Params)
                    {
                        cmd.Parameters.Add(new MySqlParameter(){ParameterName = param.Key, Value = param.Value});    
                    }

                    return cmd;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public void Dispose()
        {
            
        }
    }
}