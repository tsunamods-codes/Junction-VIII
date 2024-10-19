using AppCore;
using Iros.Workshop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValveKeyValue;

namespace AppUI.Classes
{
    /// <summary>
    /// An instance of <see cref="GameConverter"/> is used to perform actions necessary to make FF8 compatible with mods.
    /// </summary>
    public class GameConverter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public delegate void OnMessageSent(string message, NLog.LogLevel logLevel);
        public event OnMessageSent MessageSent;

        public const string BackupFolderName = "J8-BACKUP";

        public string InstallPath { get; set; }

        public GameConverter(string installPath)
        {
            InstallPath = installPath;
        }

        public static string GetInstallLocation(FF8Version installedVersion)
        {
            string installPath = "";

            switch (installedVersion)
            {
                case FF8Version.Steam:

                    // Detect if Steam is installed
                    string steamPath = GetSteamPath();

                    if (steamPath != null)
                    {
                        var stream = File.OpenRead(Path.Combine(steamPath, "steamapps\\libraryfolders.vdf"));
                        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                        KVObject data = kv.Deserialize(stream);

                        // Look through multiple libraries
                        foreach (var section in data)
                        {
                            var libraryPath = section["path"].ToString().Replace("\\\\", "\\");

                            if (File.Exists(Path.Combine(libraryPath, "steamapps\\appmanifest_39150.acf")))
                            {
                                installPath = Path.Combine(libraryPath, "steamapps\\common\\FINAL FANTASY VIII");
                            }
                        }
                    }

                    break;

                case FF8Version.Original2K:
                    installPath = RegistryHelper.GetValue(FF8RegKey.FF8AppKeyPath, "Path", "") as string;
                    break;
            }

            return installPath;
        }

        public static string GetSteamPath()
        {
            string ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath32Bit, "SteamPath") as string;

            if (ret == null) ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath64Bit, "SteamPath") as string;

            return ret != null ? ret.Replace("/","\\") : ret;
        }

        public static string GetSteamExePath()
        {
            string ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath32Bit, "SteamExe", "") as string;
            
            if (ret == null) ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath64Bit, "SteamExe", "") as string;

            return ret.Replace("/","\\");
        }

        public static string GetSteamFF8UserPath()
        {
            string ret = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Square Enix\\FINAL FANTASY VIII Steam";

            return ret;
        }

        public bool IsGamePirated()
        {
            string[] foldersToExclude = new string[] { "mods", "direct" }; // folders to skip in check

            // check all folders at root of InstallPath (excluding some)
            foreach (string subfolder in Directory.GetDirectories(InstallPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(subfolder);

                if (foldersToExclude.Any(f => dirInfo.Name.Equals(f, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue; // don't check these folders for signs of pirated files
                }

                bool isPirated = DirectoryHasPirates(subfolder);
                if (isPirated)
                {
                    return true;
                }
            }

            // check files at root of InstallPath
            foreach (string file in Directory.GetFiles(InstallPath))
            {
                bool isPirated = IsFileOrFolderAPirate(file);
                if (isPirated)
                {
                    return true;
                }
            }

            // check if given exe is a genuine one
            if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
            {
                byte[][] requiredHashes = {
                    Convert.FromHexString("03230C11328F8A1F9635435096E1659D9BFD002D"), // Steam
                    Convert.FromHexString("269C12BC1E4A127F60ABFF47A525F87B503DE615"), // Steam 4GB NTCore
                    Convert.FromHexString("3E6B9D8D70BEB51C0208021CC3164AD804B7FA7F"), // Steam 4GB JunctionVIII auto-patch
                };
                using (FileStream fs = new FileStream(Path.Combine(InstallPath, "ff8_en.exe"), FileMode.Open))
                {
                    bool matchesAtLeastOne = false;
                    byte[] currentHash = SHA1.HashData(fs);
                    foreach (byte[] hash in requiredHashes)
                    {
                        if (currentHash.SequenceEqual(hash)) { matchesAtLeastOne = true; break; }
                    }

                    if (!matchesAtLeastOne) return true;
                }
            }
            else
            {
                byte[][] requiredHashes = {
                    Convert.FromHexString("3FE6964B842CEEF85BA8080AE5D9B706D237C6F9"), // 1.0
                    Convert.FromHexString("C3E1CDDADBA30C3577325696D3969EEB078AA153"), // 1.2 NV
                    Convert.FromHexString("EC0CF23BA0CE54F6DF1BD046B217E6A127F1C088"), // 1.2 NV 4GB
                };
                using (FileStream fs = new FileStream(Path.Combine(InstallPath, "ff8.exe"), FileMode.Open))
                {
                    bool matchesAtLeastOne = false;
                    byte[] currentHash = SHA1.HashData(fs);
                    foreach (byte[] hash in requiredHashes)
                    {
                        if (currentHash.SequenceEqual(hash)) { matchesAtLeastOne = true; break; }
                    }

                    if (!matchesAtLeastOne) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks all files, folders, and sub-folders for signs of pirated files 
        /// </summary>
        /// <param name="folderPath"> folder to loop over and check </param>
        private bool DirectoryHasPirates(string folderPath)
        {

            string[] filesToAllow = new string[] { "00422 [F - Crackling fire, looped].ogg" };

            foreach (string fileEntry in Directory.GetFileSystemEntries(folderPath, "*", SearchOption.AllDirectories))
            {
                FileInfo info = new FileInfo(fileEntry);

                if (filesToAllow.Any(f => info.Name.Equals(f, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue; // this file has been marked as allowed by us so we skip the file
                }

                bool isPirated = IsFileOrFolderAPirate(fileEntry);

                if (isPirated)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the given folder or file is pirated by checking against specific keywords.
        /// </summary>
        private bool IsFileOrFolderAPirate(string pathToFileOrFolder)
        {
            string[] pirateKeyWords = new string[] { "crack", "warez", "torrent", "skidrow", "goodies" }; // folders and keywords usually found in files when the game is pirated
            string[] pirateExtensions = new string[] { ".nfo" };                                          // file extensions that indicate the game could be pirated
            string[] pirateExactKeywords = new string[] { "ali213.ini", "rld.dll", "gameservices.dll" };  // files that indicate the game is pirated

            string name;
            string ext;

            if (Directory.Exists(pathToFileOrFolder))
            {
                name = new DirectoryInfo(pathToFileOrFolder).Name;
                ext = new DirectoryInfo(pathToFileOrFolder).Extension;
            }
            else
            {
                name = new FileInfo(pathToFileOrFolder).Name;
                ext = new FileInfo(pathToFileOrFolder).Extension;
            }


            if (pirateExactKeywords.Any(s => s.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            if (pirateExtensions.Any(s => s == ext))
            {
                return true;
            }

            if (pirateKeyWords.Any(s => pathToFileOrFolder.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0))
            {
                return true;
            }

            return false;
        }

        public bool IsGameLocatedInSystemFolders()
        {
            return FileUtils.IsLocatedInSystemFolders(InstallPath);
        }

        public bool CopyGame(string targetPath = @"C:\Games\Final Fantasy VIII")
        {
            if (!Directory.Exists(InstallPath))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(targetPath);
                FileUtils.CopyDirectoryRecursively(InstallPath, targetPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }

            return true;
        }

        #region Backup And Cleanup Related Methods

        internal bool BackupExe(string backupFolderPath)
        {
            Directory.CreateDirectory(backupFolderPath);

            string ff8ExePath = Sys.Settings.FF8Exe;

            try
            {
                if (File.Exists(ff8ExePath))
                {
                    File.Copy(ff8ExePath, Path.Combine(backupFolderPath, "ff8.exe"), true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        internal bool BackupFF8ConfigExe(string backupFolderPath)
        {
            string ff8ConfigPath = Path.Combine(InstallPath, "FF8Config.exe");

            try
            {
                if (File.Exists(ff8ConfigPath))
                {
                    Directory.CreateDirectory(backupFolderPath);
                    File.Copy(ff8ConfigPath, Path.Combine(backupFolderPath, "FF8Config.exe"), true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Backup registry keys to .reg files in 'BackupGC2020' folder
        /// </summary>
        /// <param name="installPath"></param>
        public void BackupRegistry(string pathToBackup)
        {
            Directory.CreateDirectory(pathToBackup);

            if (Environment.Is64BitOperatingSystem)
            {
                // check which registry key exists for the steam release (could be in 32-bit or 64-bit area
                if (RegistryHelper.GetValue(RegistryHelper.SteamKeyPath64Bit, "InstallLocation") != null)
                {
                    RegistryHelper.ExportKey(RegistryHelper.SteamKeyPath64Bit, Path.Combine(pathToBackup, "FF8-01.reg"));
                }
                else
                {
                    RegistryHelper.ExportKey(RegistryHelper.SteamKeyPath32Bit, Path.Combine(pathToBackup, "FF8-01.reg"));
                }
            }

            RegistryHelper.ExportKey(RegistryHelper.GetKeyPath(FF8RegKey.FF8AppKeyPath), Path.Combine(pathToBackup, "FF8-03.reg"));
        }

        public void MoveOriginalConverterFilesToBackup(string pathToBackup)
        {
            Directory.CreateDirectory(pathToBackup);

            List<string> foldersToMove = new List<string>() { "DLL_in", "Hext_in", "LOADR", "Multi_DLL", "FF8anyCDv2", "BackupGC" };

            foreach (string folder in foldersToMove)
            {
                string fullPath = Path.Combine(InstallPath, folder);
                if (Directory.Exists(fullPath))
                {
                    FileUtils.MoveDirectoryRecursively(fullPath, Path.Combine(pathToBackup, folder));
                    Directory.Delete(fullPath, true);
                }
            }
        }

        /// <summary>
        /// Delete all cache files (S*D.P and T*D.P files) in <see cref="InstallPath"/>
        /// </summary>
        public bool DeleteCacheFiles()
        {
            if (!Directory.Exists(InstallPath))
            {
                return true;
            }

            try
            {
                foreach (string file in Directory.GetFiles(InstallPath, "S*D.P"))
                {
                    File.Delete(file);
                }

                foreach (string file in Directory.GetFiles(InstallPath, "T*D.P"))
                {
                    File.Delete(file);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }


        }

        #endregion

        public bool CopyFF8ExeToGame()
        {
            if (!Directory.Exists(InstallPath))
            {
                return false;
            }

            string ff8ExePath = Path.Combine(Sys.PathToPatchedExeFolder, "ff8.exe");

            try
            {
                File.Copy(ff8ExePath, Path.Combine(InstallPath, "ff8.exe"), true);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Verifies a FF8 install is a Full/Max install by checking if specific files are in the game dir.
        /// They will automatically be copied from discs if not found. 
        /// Returns false if failed to find/copy all files
        /// </summary>
        /// <returns> Returns true if all files found and/or copied; false otherwise </returns>
        public bool VerifyFullInstallation()
        {
            bool foundAllFiles = true;

            string[] expectedFiles = new string[]
            {
                @"battle.fs",
                @"magic.fs",
                @"main.fs",
                @"menu.fs",
                @"world.fs",
            };

            string[] volumeLabels = new string[]
            {
                "ff8_install",
                "ff8_disc1",
                "ff8_disc2",
                "ff8_disc3",
                "ff8_disc4"
            };

            foreach (string file in expectedFiles)
            {
                string fullTargetPath = Path.Combine(InstallPath, "data", file);

                if (File.Exists(fullTargetPath))
                {
                    // file already exists at install path so continue
                    continue;
                }

                SendMessage($"...\t {file} {ResourceHelper.Get(StringKey.NotFoundScanningDiscsForFiles)}", NLog.LogLevel.Warn);

                // search all drives for the file
                bool foundFileOnDrive = false;
                foreach (string label in volumeLabels)
                {
                    string driveLetter = GameLauncher.GetDriveLetter(label);

                    if (!string.IsNullOrWhiteSpace(driveLetter))
                    {
                        string fullSourcePath = Path.Combine(driveLetter, "ff8", file);
                        if (label == "ff8_install")
                        {
                            fullSourcePath = Path.Combine(driveLetter, "data", file); // ff8install disc has a different path then disc1-4
                        }


                        if (File.Exists(fullSourcePath))
                        {
                            foundFileOnDrive = true;
                            SendMessage($"... \t {string.Format(ResourceHelper.Get(StringKey.FoundFileOnAtCopyingFile), label, driveLetter)}");
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(fullTargetPath)); // ensure all subfolders are created
                                File.Copy(fullSourcePath, fullTargetPath, true);
                                SendMessage($"... \t\t {ResourceHelper.Get(StringKey.Copied)}!");
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                                SendMessage($"... \t\t {ResourceHelper.Get(StringKey.FailedToCopyErrorHasBeenLogged)}", NLog.LogLevel.Warn);
                            }
                        }
                    }

                    if (foundFileOnDrive)
                    {
                        break; // done searching drives as file found/copied
                    }
                }

                // at this point if file not found/copied on any drive then failed verification
                if (!foundFileOnDrive)
                {
                    SendMessage($"... \t {string.Format(ResourceHelper.Get(StringKey.FailedToFindOnAnyDisc), file)}", NLog.LogLevel.Warn);
                    foundAllFiles = false;
                }
            }

            return foundAllFiles;
        }

        /// <summary>
        /// Verifies specific files exist in /data/[subfolder] where [subfolder] is battle, kernel, and movies.
        /// If files not found then they are copied from /data/lang-en/[subfolder] (language folder could be different based on languaged installed)
        /// </summary>
        /// <returns></returns>
        internal bool VerifyAdditionalFilesExist()
        {
            string[] expectedFiles = new string[]
            {
                @"field.fs",
            };

            foreach (string file in expectedFiles)
            {
                string fullTargetPath = Path.Combine(InstallPath, "data", file);

                if (!File.Exists(fullTargetPath))
                {
                    SendMessage($"... \t{file} {ResourceHelper.Get(StringKey.FileNotFound)}", NLog.LogLevel.Warn);
                    return false;
                }
            }

            return true;
        }

        public void SendMessage(string message, NLog.LogLevel logLevel = null)
        {
            if (logLevel == null)
            {
                logLevel = NLog.LogLevel.Info;
            }

            MessageSent?.Invoke(message, logLevel);
        }

        /// <summary>
        /// Creates 'direct' and subfolders, 'mods/Textures' folders if missing
        /// </summary>
        public void CreateMissingDirectories()
        {
            string[] requiredFolders = new string[]
            {
                Path.Combine(InstallPath, "direct"),
                Path.Combine(InstallPath, "mods", "Textures"),
            };

            foreach (string dir in requiredFolders)
            {
                if (!Directory.Exists(dir))
                {
                    Logger.Info($"\t{ResourceHelper.Get(StringKey.DirectoryMissingCreating)} {dir}");
                    Directory.CreateDirectory(dir);
                }
            }
        }

        internal bool IsExeDifferent()
        {
            string ff8ExePath = Path.Combine(Sys.PathToPatchedExeFolder, "ff8.exe");
            return !FileUtils.AreFilesEqual(ff8ExePath, Path.Combine(InstallPath, "ff8.exe"));
        }

        /// <summary>
        /// Runs ulgp v1.3.2 with given arguments.
        /// if <paramref name="sourcePath"/> is *.lgp then it will be dumped to <paramref name="destPath"/>,
        /// otherwise <paramref name="sourcePath"/> is assumed to be a directory containing files to add to a new or existing .lgp
        /// </summary>
        private void RunUlgp(string sourcePath, string destPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = Sys.PathToUlgpExe,
                    WorkingDirectory = Path.GetDirectoryName(Sys.PathToUlgpExe),
                    Arguments = $"\"{sourcePath}\" \"{destPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas", // ensures the process is started as Admin
                };

                using (Process proc = Process.Start(startInfo))
                {
                    proc.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                SendMessage($"{ResourceHelper.Get(StringKey.AnErrorOccurredStartingULGP)}", NLog.LogLevel.Error);
            }
        }
    }
}
