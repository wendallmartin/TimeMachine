using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DataEncrypterDecrypter;
using NLog;

namespace TheTimeApp
{
    public class AppSettings
    {
        private static readonly string SettingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TheTimeApp\\settings.xml";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static AppSettings _instance;

        public static AppSettings Instance
        {
            get{
                if (_instance == null)
                {
                    _instance = new AppSettings();
                    _instance.Validate();
                    _instance.Load();
                }
                
                return _instance;
            }
            set => _instance = value;// can set singleton to anything(for testing only!)
        }
        
        public string DataPath { get; set; }
        public string CurrentUser { get; set; }
        public string FromAddress { get; set; }
        public string FromUser { get; set; }
        public string FromPass { get; set; }
        public string FromPort { get; set; }
        public string EmailHost { get; set; }
        public string ToAddress { get; set; }
        public string MilitaryTime { get; set; }
        public string SslEmail { get; set; }
        public string AzureDateSource { get; set; }
        public string AzureUser { get; set; }
        public string AzurePassword { get; set; }
        public string AzureCateloge { get; set; }
        public string AzurePort { get; set; }
        public string MySqlServer { get; set; }
        public string MySqlUserId { get; set; }
        public string MySqlPassword { get; set; }
        public string MySqlDatabase { get; set; }
        public int MySqlPort { get; set; }
        public string MySqlSsl { get; set; }
        public string SqlEnabled { get; set; }
        public string SqlType { get; set; }
        public string MainPermission { get; set; }
        public string GitRepoPath { get; set; }
        public string GitUserName { get; set; }
        public bool GitEnabled { get; set; }
        
        public void Load()
        {
            DataPath = ReadValueFromXml("uwnmnnvvkgsfghks", false) == "" ? "time" : ReadValueFromXml("uwnmnnvvkgsfghks", false);
            CurrentUser = ReadValueFromXml("ahkakljdfgj");
            FromAddress = ReadValueFromXml("ufhgawh");
            FromUser = ReadValueFromXml("klaosof");
            FromPass = ReadValueFromXml("wasllefa");
            FromPort = ReadValueFromXml("adjflegad");
            EmailHost = ReadValueFromXml("slllejfas");
            ToAddress = ReadValueFromXml("aaljgjlkej");
            MilitaryTime = ReadValueFromXml("keslkwkjw");
            SslEmail = ReadValueFromXml("ljowoiislo");
            AzureDateSource = ReadValueFromXml("kwkvuesav");
            AzureUser = ReadValueFromXml("aslejfooowgh");
            AzurePassword = ReadValueFromXml("lsoenwowdjf");
            AzureCateloge = ReadValueFromXml("lafjedoioowowfowof");
            AzurePort = ReadValueFromXml("dhjahhdjh");
            MySqlServer = ReadValueFromXml("aksdfajhfgjh");
            MySqlUserId = ReadValueFromXml("jgufsedasdfhjkfhif");
            MySqlPassword = ReadValueFromXml("jghjefdhguishsifpfafsdjkuh");
            MySqlDatabase = ReadValueFromXml("igaujjshjsdfhhalfjlffhio");
            MySqlPort = !string.IsNullOrEmpty(ReadValueFromXml("fvdjgfghjkjsdfhjgknklhfzsd")) ? int.Parse(ReadValueFromXml("fvdjgfghjkjsdfhjgknklhfzsd")) : 0;
            MySqlSsl = ReadValueFromXml("aedfjfgjafklahjfgahfap");
            SqlEnabled = ReadValueFromXml("dkjfgjpwhjpo");
            SqlType = ReadValueFromXml("jgfhgjasgjfajfghj");
            MainPermission = ReadValueFromXml("dlajwugpasdh");
            GitRepoPath = ReadValueFromXml("lewinsaaowtwe", false);
            GitUserName = ReadValueFromXml("aagaajfafsdf");
            GitEnabled = ReadValueFromXml("djahjeuasdjl") == "true";
        }

        

        public void Save()
        {
            WriteValueToXml("uwnmnnvvkgsfghks", DataPath, false);
            WriteValueToXml("ahkakljdfgj", CurrentUser);
            WriteValueToXml("ufhgawh", FromAddress);
            WriteValueToXml("klaosof", FromUser);
            WriteValueToXml("wasllefa", FromPass);
            WriteValueToXml("adjflegad", FromPort);
            WriteValueToXml("slllejfas", EmailHost);
            WriteValueToXml("aaljgjlkej", ToAddress);
            WriteValueToXml("keslkwkjw", MilitaryTime);
            WriteValueToXml("ljowoiislo", SslEmail);
            WriteValueToXml("kwkvuesav", AzureDateSource);
            WriteValueToXml("aslejfooowgh", AzureUser);
            WriteValueToXml("lsoenwowdjf", AzurePassword);
            WriteValueToXml("lafjedoioowowfowof", AzureCateloge);
            WriteValueToXml("dhjahhdjh", AzurePort);
            WriteValueToXml("aksdfajhfgjh", MySqlServer);
            WriteValueToXml("jgufsedasdfhjkfhif", MySqlUserId);
            WriteValueToXml("jghjefdhguishsifpfafsdjkuh", MySqlPassword);
            WriteValueToXml("igaujjshjsdfhhalfjlffhio", MySqlDatabase);
            WriteValueToXml("fvdjgfghjkjsdfhjgknklhfzsd", MySqlPort.ToString());
            WriteValueToXml("aedfjfgjafklahjfgahfap", MySqlSsl);
            WriteValueToXml("dkjfgjpwhjpo", SqlEnabled);
            WriteValueToXml("jgfhgjasgjfajfghj", SqlType);
            WriteValueToXml("dlajwugpasdh", MainPermission);
            WriteValueToXml("lewinsaaowtwe", GitRepoPath, false);
            WriteValueToXml("aagaajfafsdf", GitUserName);
            WriteValueToXml("djahjeuasdjl", GitEnabled ? "true" : "false");
        }
                
        /// <summary>
        /// Creates new database if it does not exist.
        /// </summary>
        private void Validate()
        {
            if (!File.Exists(SettingsFilePath))
            {
                SetSettingsToDefault();
            }
        }

        /// <summary>
        /// Cannot set default value of encrypted
        /// field without encrypting value!
        /// </summary>
        private static void SetSettingsToDefault()
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(SettingsFilePath, null);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("settings");

            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 3;
            xmlWriter.IndentChar = ' ';

            xmlWriter.WriteStartElement("uwnmnnvvkgsfghks");
            xmlWriter.WriteValue(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\TheTimeApp\time.sqlite");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("ahkakljdfgj");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ufhgawh");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("klaosof");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("wasllefa");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("adjflegad");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("slllejfas");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ljowoiislo");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("aaljgjlkej");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("aaljgjlkej");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("keslkwkjw");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("kwkvuesav");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("aslejfooowgh");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("lsoenwowdjf");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("lafjedoioowowfowof");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("dkjfgjpwhjpo");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("jgfhgjasgjfajfghj");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("dlajwugpasdh");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("aksdfajhfgjh");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("jgufsedasdfhjkfhif");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("jghjefdhguishsifpfafsdjkuh");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
                
            xmlWriter.WriteStartElement("igaujjshjsdfhhalfjlffhio");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("fvdjgfghjkjsdfhjgknklhfzsd");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("aedfjfgjafklahjfgahfap");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("lewinsaaowtwe");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("aagaajfafsdf");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("djahjeuasdjl");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        /// <summary>
        /// Reads the data of specified node provided in the parameter
        /// </summary>
        /// <param name="name">Node to be read</param>
        /// <param name="encrypted"></param>
        /// <returns>string containing the value</returns>
        private static string ReadValueFromXml(string name, bool encrypted = true)
        {
            try
            {
                XDocument doc = XDocument.Load(SettingsFilePath);
                foreach (XElement element in doc.Descendants().Where(e => e.Name == name))
                {
                    string value = element.Value;
                    if (string.IsNullOrEmpty(element.Value))
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return encrypted ? CryptoEngine.Decrypt(element.Value, "some_guffy_thin4") : value;    
                    }
                }
                return string.Empty;
            }
            catch(Exception e)
            {
                Log.Info(e.ToString);
                throw;
            }
        }
        
        /// <summary>
        /// Writes the updated value to XML
        /// </summary>
        /// <returns></returns>
        private static void WriteValueToXml(string name, string value, bool encrypted = true)
        {
            try
            {
                XDocument doc = XDocument.Load(SettingsFilePath);
                
                if (doc.Element("settings") == null)
                {
                    throw new Exception("Settings corrupted!!!");    
                }
                
                bool exist = false;
                foreach (XElement element in doc.Descendants().Where(e => e.Name == name))
                {
                    element.Value = encrypted ? CryptoEngine.Encrypt(value, "some_guffy_thin4") : value;
                    exist = true;
                }
                if (!exist)
                {
                    doc.Element("settings")?.Add(new XElement(name, encrypted ? CryptoEngine.Encrypt(value, "some_guffy_thin4") : value));
                }

                doc.Save(SettingsFilePath);
            }
            catch(Exception e)
            {
                Log.Info(e.ToString);
                throw;
            }
        }
    }
}

