using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace TheTimeApp.TimeData
{
    [Serializable]
    public class SqlBuffer
    {
        private List<SerilizeSqlCommand> _buffer;
        
        [NonSerialized]
        private static readonly object ReadWrite = new object();
        
        [NonSerialized]
        private static readonly string Location = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TheTimeApp\\buffer.sbf";
        
        [NonSerialized]
        private static readonly byte[] Iv = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xCD, 0xEF };
        
        [NonSerialized]
        private static readonly byte[] BKey = { 27, 35, 75, 232, 73, 52, 87, 99 };
        
        public SqlBuffer()
        {
            _buffer = new List<SerilizeSqlCommand>();
        }

        /// <summary>
        /// Adds serializable command to buffer.
        /// </summary>
        /// <param name="cmd"></param>
        public void Add(SerilizeSqlCommand cmd)
        {
            lock (ReadWrite)
            {
                _buffer.Add(cmd);    
            }
        }
        
        /// <summary>
        /// Removes command from buffer.
        /// </summary>
        /// <param name="command"></param>
        public void Remove(SerilizeSqlCommand command)
        {
            lock (ReadWrite)
            {
                _buffer.Remove(command);    
            }
        }
        
        /// <summary>
        /// Returns copy of buffer.
        /// </summary>
        /// <returns></returns>
        public List<SerilizeSqlCommand> Buffer()
        {
            var commands = new List<SerilizeSqlCommand>();
            lock (ReadWrite)
            {
                foreach (SerilizeSqlCommand command in _buffer)
                {
                    commands.Add(command);
                }
            }
            return commands;
        }

        public void ClearBuffer()
        {
            lock (ReadWrite)
            {
                _buffer.Clear();    
            }
        }

        /// <summary>
        /// Loads buffer from disk.
        /// </summary>
        /// <returns></returns>
        public static SqlBuffer Load()
        {
            SqlBuffer buffer = new SqlBuffer();
            if (File.Exists(Location))
            {
                lock (ReadWrite)
                {
                    using (var fs = new FileStream(Location, FileMode.Open))
                    {
                        using (var des = new DESCryptoServiceProvider())
                        {
                            using (Stream cryptoStream = new CryptoStream(fs, des.CreateDecryptor(BKey, Iv), CryptoStreamMode.Read))
                            {
                                var binaryFormatter = new BinaryFormatter();
                                buffer = (SqlBuffer) binaryFormatter.Deserialize(cryptoStream);
                            }
                        }
                    }
                }
            }

            return buffer;
        }

        /// <summary>
        /// Saves the buffer to disk.
        /// </summary>
        public void Save()
        {
            if (!File.Exists(Location)) File.Create(Location).Close();
                    
            using (var fs = new FileStream(Location, FileMode.Create))
            {
                using (var des = new DESCryptoServiceProvider())
                {
                    using (Stream cryptoStream = new CryptoStream(fs, des.CreateEncryptor(BKey, Iv), CryptoStreamMode.Write))
                    {
                        var binaryFormatter = new BinaryFormatter();
                        binaryFormatter.Serialize(cryptoStream, this);
                        cryptoStream.Flush();
                    }
                }
            }
        }
    }
}