using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DataEncrypterDecrypter;

namespace TheTimeApp
{
    public class AppSettings
    {
        private readonly Rfc2898DeriveBytes _DeriveBytes;
        private static readonly byte[] _InitVectorBytes;
        private static readonly byte[] _KeyBytes;
        private static string settingsFilePath = "settings.xml";
        
        public static string DataPath
        {
            get{
                if (ReadValueFromXML("uwnmnnvvkgsfghks", false) == "")
                {
                    WriteValueTOXML("uwnmnnvvkgsfghks", "time.dtf", false);
                }
                return ReadValueFromXML("uwnmnnvvkgsfghks", false);
            }
            set{ WriteValueTOXML("uwnmnnvvkgsfghks", value, false); }
        }
        public static string FromAddress
        {
            get { return ReadValueFromXML("ufhgawh"); }
            set { WriteValueTOXML("ufhgawh", value); }
        }

        public static string FromUser
        {
            get { return ReadValueFromXML("klaosof"); }
            set { WriteValueTOXML("klaosof", value); }
        }

        public static string FromPass
        {
            get { return ReadValueFromXML("wasllefa"); }
            set { WriteValueTOXML("wasllefa", value); }
        }
        public static string FromPort
        {
            get { return ReadValueFromXML("adjflegad"); }
            set { WriteValueTOXML("adjflegad", value); }
        }
        public static string EmailHost
        {
            get { return ReadValueFromXML("slllejfas"); }
            set { WriteValueTOXML("slllejfas", value); }
        }
        public static string ToAddress
        {
            get { return ReadValueFromXML("aaljgjlkej"); }
            set { WriteValueTOXML("aaljgjlkej", value); }
        }
        public static string MilitaryTime
        {
            get { return ReadValueFromXML("keslkwkjw"); }
            set { WriteValueTOXML("keslkwkjw", value); }
        }

        public static string SslEmail
        {
            get { return ReadValueFromXML("ljowoiislo"); }
            set { WriteValueTOXML("ljowoiislo", value); }
        }

        public static string SQLDataSource
        {
            get{ return ReadValueFromXML("kwkvuesav");}
            set{ WriteValueTOXML("kwkvuesav", value); }
        }
        
        public static string SQLPortNumber
        {
            get{ return ReadValueFromXML("dhjahhdjh");}
            set{ WriteValueTOXML("dhjahhdjh", value); }
        }

        public static string SQLUserId
        {
            get { return ReadValueFromXML("aslejfooowgh"); }
            set { WriteValueTOXML("aslejfooowgh", value); }
        }

        public static string SQLPassword
        {
            get { return ReadValueFromXML("lsoenwowdjf"); }
            set { WriteValueTOXML("lsoenwowdjf", value); }
        }

        public static string SQLCatelog
        {
            get { return ReadValueFromXML("lafjedoioowowfowof"); }
            set { WriteValueTOXML("lafjedoioowowfowof", value); }
        }

        public static string SQLEnabled
        {
            get{return ReadValueFromXML("dkjfgjpwhjpo");}
            set{ WriteValueTOXML("dkjfgjpwhjpo", value); }
        }

        public static string MainPermission
        {
            get { return ReadValueFromXML("dlajwugpasdh"); }
            set { WriteValueTOXML("dlajwugpasdh", value); }
        }

        public static void Validate()
        {
            if (!File.Exists(settingsFilePath))
            {
                SetSettingsToDefault();
            }
        }

        private static void SetSettingsToDefault()
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(settingsFilePath, null);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("settings");

            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 3;
            xmlWriter.IndentChar = ' ';

            xmlWriter.WriteStartElement("uwnmnnvvkgsfghks");
            xmlWriter.WriteValue("time.dtf");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ufhgawh");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("klaosof");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("wasllefa");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("adjflegad");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("slllejfas");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ljowoiislo");
            xmlWriter.WriteValue("false");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("aaljgjlkej");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("aaljgjlkej");
            xmlWriter.WriteValue("");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("keslkwkjw");
            xmlWriter.WriteValue("false");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("kwkvuesav");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("aslejfooowgh");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("lsoenwowdjf");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("lafjedoioowowfowof");
            xmlWriter.WriteValue("xxxxxxxx");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("dkjfgjpwhjpo");
            xmlWriter.WriteValue("false");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("dlajwugpasdh");
            xmlWriter.WriteValue("read");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        /// <summary>
        /// Reads the data of specified node provided in the parameter
        /// </summary>
        /// <param name="name">Node to be read</param>
        /// <returns>string containing the value</returns>
        private static string ReadValueFromXML(string name, bool encrypted = true)
        {
            try
            {
                XDocument doc = XDocument.Load(settingsFilePath);
                foreach (XElement element in doc.Descendants().Where(e => e.Name == name))
                {
                    string value = element.Value;
                    return encrypted ? CryptoEngine.Decrypt(element.Value, "some_guffy_thin4") : value;
                }
                return string.Empty;
            }
            catch(System.Exception e)
            {
                //do some error logging here. Leaving for you to do 
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Writes the updated value to XML
        /// </summary>
        /// <param name="pstrValueToRead">Node of XML to read</param>
        /// <param name="pstrValueToWrite">Value to write to that node</param>
        /// <returns></returns>
        private static bool WriteValueTOXML(string name, string value, bool encrypted = true)
        {
            try
            {
                XDocument doc = XDocument.Load(settingsFilePath);

                bool exist = false;
                foreach (XElement element in doc.Descendants().Where(e => e.Name == name))
                {
                    element.Value = encrypted ? CryptoEngine.Encrypt(value, "some_guffy_thin4") : value;
                    exist = true;
                }
                if (!exist)
                {
                    doc.Element("settings").Add(new XElement(name, encrypted ? CryptoEngine.Encrypt(value, "some_guffy_thin4") : value));
                }

                doc.Save(settingsFilePath);
                return true;
            }
            catch
            {
                //properly you need to log the exception here. But as this is just an
                //example, I am not doing that. 
                return false;
            }
        }
    }
}

