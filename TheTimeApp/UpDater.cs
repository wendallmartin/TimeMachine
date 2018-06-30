using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;

namespace TheTimeApp
{
    public class UpDater
    {
        public static string FtpUrl = "ftp://208.113.130.91/files/Wendall/TimeApp/";
        public static NetworkCredential FtpCredentials = new NetworkCredential("abcodeblocks","Coding4HisGlory!");
        
        public static List<string> DontTouchExt = new List<string>(){".dtf", ".png", ".ico"};
        
        public static List<string> IgnoredNames = new List<string>(){"settings"};
        
        //using System.Deployment.Application;
        //using System.Reflection;
        public static Version CurrentVersion => new Version(ApplicationDeployment.IsNetworkDeployed ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString() : Assembly.GetExecutingAssembly().GetName().Version.ToString());

        public static void RemoveOldMoveNewFiles()
        {
            var files = GetFiles(Directory.GetCurrentDirectory());

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);

                if(IsOldName(name))
                {
                    File.Delete(file);
                }
            }
            foreach (string folder in Directory.GetDirectories(Directory.GetCurrentDirectory()))
            {
                if (IsNewName(folder))
                {
                    string newName = folder.Replace("_NEW", "");
                    if (Directory.Exists(newName))
                    {
                        Directory.Delete(newName, true);
                    }           
                    
                    Directory.Move(folder, newName);
                }
            }
        }

        /// <summary>
        /// Excepts file or folder name without extention
        /// and returns true if name is old program name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsOldName(string name)
        {
            return name != null && name.Length > 3 &&
                 name[name.Length - 4] == '_' && 
                 name[name.Length - 3] == 'O' && 
                 name[name.Length - 2] == 'L' && 
                 name[name.Length - 1] == 'D';
        }
        
        /// <summary>
        /// Excepts file or folder name without extention
        /// and returns true if name is old program name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsNewName(string name)
        {
            return name != null && name.Length > 3 &&
                   name[name.Length - 4] == '_' && 
                   name[name.Length - 3] == 'N' && 
                   name[name.Length - 2] == 'E' && 
                   name[name.Length - 1] == 'W';
        }

        /// <summary>
        /// Adds "_OLD" to all files and folders
        /// </summary>
        public static void AddOld()
        {
            string currentDir = Directory.GetCurrentDirectory();
            var files = GetFiles(currentDir);

            foreach (string file in files)
            {
                string filenamewithoutextention = Path.GetFileNameWithoutExtension(file);
                string extention = Path.GetExtension(file);
                
                if (string.IsNullOrEmpty(file) || IgnoredNames.Any(n => n == filenamewithoutextention))
                    continue;

                if (DontTouchExt.Any(e => e == extention))// if this extention needs to wait and be deleted later, continue..........
                    continue;

                // rename file
                try
                {
                    string fileOld = filenamewithoutextention + "_OLD" + extention;
                        
                    if (File.Exists(fileOld))
                        File.Delete(fileOld);

                    File.Move(file, fileOld);
                }
                catch (Exception)
                {
                    MessageBox.Show($"Move {file} failed!");
                }
            }

            foreach (var folder in Directory.GetDirectories(currentDir))
            {
                string folderOld = folder + "_OLD";

                try
                {
                    if (Directory.Exists(folderOld))
                        Directory.Delete(folderOld);

                    Directory.Move(folder, folderOld);
                }
                catch (Exception)
                {
                    MessageBox.Show($"Move {folder} failed!");
                }
            }
        }

        /// <summary>
        /// Removes "_OLD" from files and folders
        /// </summary>
        public static void RemoveOld()
        {
            string currentDir = Directory.GetCurrentDirectory();
            var files = GetFiles(currentDir);
            foreach (string file in files)
            {
                string filenamewithoutextention = Path.GetFileNameWithoutExtension(file);
                string extention = Path.GetExtension(file);
                
                if (string.IsNullOrEmpty(file))
                    continue;

                string newname = RemoveOldFromString(filenamewithoutextention) + extention;
                
                if (file != newname)// since we do this check we don't need to exclude files in DontTouchExt or IgnoredNames
                {
                    File.Move(file, newname);                    
                }
            }
            foreach (string folder in Directory.GetDirectories(Directory.GetCurrentDirectory()))
            {
                if (IsOldName(folder))
                {
                    Directory.Move(folder, RemoveOldFromString(folder));
                }
            }
            
            string RemoveOldFromString(string name)
            {
                while (IsOldName(name))
                {
                    name = name.Replace("_OLD", "");
                }
                return name;
            }
        }


        /// <summary>
        /// Returns list of Versions available on ftp update server.
        /// </summary>
        /// <returns></returns>
        public static List<Version> GetUpdateVersions()
        {
            try
            {
                List<Version> directorys = new List<Version>();
                FtpWebRequest ftpWebRequest = (FtpWebRequest) WebRequest.Create(FtpUrl);
                ftpWebRequest.Credentials = FtpCredentials;
                ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                FtpWebResponse response = (FtpWebResponse) ftpWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(response.GetResponseStream() ?? throw new Exception("Respose is null"));
                string line = streamReader.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    directorys.Add(new Version(line));
                    line = streamReader.ReadLine();
                }

                streamReader.Close();
                return directorys;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void DownloadFtpDirectory(string url, string localPath)
        {
            FtpWebRequest listRequest = (FtpWebRequest) WebRequest.Create(url);
            listRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            listRequest.Credentials = FtpCredentials;
            List<string> lines = new List<string>();
            using (FtpWebResponse listResponse = (FtpWebResponse) listRequest.GetResponse())
            using (Stream listStream = listResponse.GetResponseStream())
            using (StreamReader listReader = new StreamReader(listStream))
            {
                while (!listReader.EndOfStream)
                {
                    lines.Add(listReader.ReadLine());
                }
            }

            foreach (string line in lines)
            {
                string[] tokens = line.Split(new[] {' '}, 9, StringSplitOptions.RemoveEmptyEntries);
                string name = tokens[8];
                string permissions = tokens[0];
                string localFilePath = Path.Combine(localPath, name);
                string fileUrl = url + name;
                if (permissions[0] == 'd')// this is a directory
                {
                    localFilePath += "_NEW";
                    if (!Directory.Exists(localFilePath))
                    {
                        Directory.CreateDirectory(localFilePath);
                    }

                    DownloadFtpDirectory(fileUrl + "/", localFilePath);
                }
                else// this is a file
                {
                    FtpWebRequest downloadRequest = (FtpWebRequest) WebRequest.Create(fileUrl);
                    downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                    downloadRequest.Credentials = FtpCredentials;
                    try
                    {

                        using (FtpWebResponse downloadResponse = (FtpWebResponse) downloadRequest.GetResponse())
                        using (Stream sourceStream = downloadResponse.GetResponseStream())
                        using (Stream targetStream = File.Create(localFilePath))
                        {
                            byte[] buffer = new byte[10240];
                            int read;
                            while ((read = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                targetStream.Write(buffer, 0, read);
                            }
                        }
                    }
                    catch (WebException e)
                    {
                        MessageBox.Show(fileUrl + " : " + ((FtpWebResponse) e.Response).StatusDescription);
                    }
                }
            }
        }
        
        public static List<String> GetFiles(string sDir)
        {
            List<String> files = new List<String>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    files.Add(f);
                }
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
            
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(GetFiles(d));
                }
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }

            return files;
        }
        
        public static void Update(Version latest)
        {
            try
            {
                AddOld();
                DownloadFtpDirectory(FtpUrl + latest + "/", Directory.GetCurrentDirectory());
                MessageBox.Show("Update successful!");
            }
            catch (Exception)
            {
                RemoveOld();
                MessageBox.Show("Update failed!");
            }
        }

        public static bool CheckForUpdates()
        {
            List<Version> versions;
            try
            {
                versions = GetUpdateVersions();
            }
            catch (Exception)
            {
                MessageBox.Show("There is a problem on our end. Try again later.");
                return false;
            }

            if (versions.Count > 0)
            {
                versions.Sort();
                Version latest = versions[versions.Count - 1];
                if (latest.CompareTo(CurrentVersion) > 0)
                {
                    MessageBoxResult downloadUpdate = MessageBox.Show($"New version {latest} is available! Do you want to update?", "Update", MessageBoxButton.YesNo);
                    if (downloadUpdate == MessageBoxResult.Yes)
                    {
                        Update(latest);
                        return true;
                    }
                }
                else
                {
                    MessageBox.Show("You are up to date!");
                }
            }
            else
            {
                MessageBox.Show("No updates on server!");
            }

            return false;
        }
    }
}