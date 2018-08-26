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
        private const string SettingsFilePath = "settings.xml";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public static string DataPath
        {
            get{
                if (ReadValueFromXml("uwnmnnvvkgsfghks", false) == "")
                {
                    WriteValueToxml("uwnmnnvvkgsfghks", "time", false);
                }
                return ReadValueFromXml("uwnmnnvvkgsfghks", false);
            }
            set => WriteValueToxml("uwnmnnvvkgsfghks", value, false);
        }

        public static string CurrentUser
        {
            get => ReadValueFromXml("ahkakljdfgj");
            set => WriteValueToxml("ahkakljdfgj", value);
        }
        
        public static string FromAddress
        {
            get => ReadValueFromXml("ufhgawh");
            set => WriteValueToxml("ufhgawh", value);
        }

        public static string FromUser
        {
            get => ReadValueFromXml("klaosof");
            set => WriteValueToxml("klaosof", value);
        }

        public static string FromPass
        {
            get => ReadValueFromXml("wasllefa");
            set => WriteValueToxml("wasllefa", value);
        }
        public static string FromPort
        {
            get => ReadValueFromXml("adjflegad");
            set => WriteValueToxml("adjflegad", value);
        }
        public static string EmailHost
        {
            get => ReadValueFromXml("slllejfas");
            set => WriteValueToxml("slllejfas", value);
        }
        public static string ToAddress
        {
            get => ReadValueFromXml("aaljgjlkej");
            set => WriteValueToxml("aaljgjlkej", value);
        }
        public static string MilitaryTime
        {
            get => ReadValueFromXml("keslkwkjw");
            set => WriteValueToxml("keslkwkjw", value);
        }

        public static string SslEmail
        {
            get => ReadValueFromXml("ljowoiislo");
            set => WriteValueToxml("ljowoiislo", value);
        }

        public static string AzureDateSource
        {
            get => ReadValueFromXml("kwkvuesav");
            set => WriteValueToxml("kwkvuesav", value);
        }
        
        public static string AzureUser
        {
            get => ReadValueFromXml("aslejfooowgh");
            set => WriteValueToxml("aslejfooowgh", value);
        }

        public static string AzurePassword
        {
            get => ReadValueFromXml("lsoenwowdjf");
            set => WriteValueToxml("lsoenwowdjf", value);
        }

        public static string AzureCateloge
        {
            get => ReadValueFromXml("lafjedoioowowfowof");
            set => WriteValueToxml("lafjedoioowowfowof", value);
        }
        
        public static string AzurePort
        {
            get => ReadValueFromXml("dhjahhdjh");
            set => WriteValueToxml("dhjahhdjh", value);
        }
        
        
        public static string MySqlServer
        {
            get => ReadValueFromXml("aksdfajhfgjh");
            set => WriteValueToxml("aksdfajhfgjh", value);
        }
        
        public static string MySqlUserId
        {
            get => ReadValueFromXml("jgufsedasdfhjkfhif");
            set => WriteValueToxml("jgufsedasdfhjkfhif", value);
        }

        public static string MySqlPassword
        {
            get => ReadValueFromXml("jghjefdhguishsifpfafsdjkuh");
            set => WriteValueToxml("jghjefdhguishsifpfafsdjkuh", value);
        }

        public static string MySqlDatabase
        {
            get => ReadValueFromXml("igaujjshjsdfhhalfjlffhio");
            set => WriteValueToxml("igaujjshjsdfhhalfjlffhio", value);
        }
        
        /// <summary>
        /// Int port value
        /// </summary>
        public static int MySqlPort
        {
            get{
                if (!string.IsNullOrEmpty(ReadValueFromXml("fvdjgfghjkjsdfhjgknklhfzsd")))
                {
                    return int.Parse(ReadValueFromXml("fvdjgfghjkjsdfhjgknklhfzsd"));
                }
                else
                {
                    return 0;
                }
            }
            set => WriteValueToxml("fvdjgfghjkjsdfhjgknklhfzsd", value.ToString());
        }
        
        public static string SqlEnabled
        {
            get => ReadValueFromXml("dkjfgjpwhjpo");
            set => WriteValueToxml("dkjfgjpwhjpo", value);
        }

        public static string SqlType
        {
            get => ReadValueFromXml("jgfhgjasgjfajfghj");
            set => WriteValueToxml("jgfhgjasgjfajfghj", value);
        }

        public static string MainPermission
        {
            get => ReadValueFromXml("dlajwugpasdh");
            set => WriteValueToxml("dlajwugpasdh", value);
        }

        public static void Validate()
        {
            if (!File.Exists(SettingsFilePath))
            {
                SetSettingsToDefault();
            }
        }

        private static void SetSettingsToDefault()
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(SettingsFilePath, null);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("settings");

            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 3;
            xmlWriter.IndentChar = ' ';

            xmlWriter.WriteStartElement("uwnmnnvvkgsfghks");
            xmlWriter.WriteValue("time.sqlite");
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
        private static void WriteValueToxml(string name, string value, bool encrypted = true)
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

