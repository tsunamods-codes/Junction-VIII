/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using AppCore;
using Iros.Workshop.ConfigSettings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Iros.Workshop
{
    public class ModStatusEventArgs : EventArgs
    {
        public ModStatus OldStatus { get; set; }
        public ModStatus Status { get; set; }
        public Guid ModID { get; set; }
    }

    public class MessageEventArgs : EventArgs
    {
        public WMessage Message { get; set; }
    }

    public enum WMessageLogLevel
    {
        Error,
        Info,
        LogOnly,
        StatusOnly
    }

    public class WMessage
    {
        public WMessage()
        {
            IsImportant = false;
            LogLevel = WMessageLogLevel.Info;
            TextTranslationKey = null;
        }

        public WMessage(string message)
        {
            Text = message;
            IsImportant = false;
            LogLevel = WMessageLogLevel.Info;
        }

        public WMessage(string message, bool isImportant)
        {
            Text = message;
            IsImportant = isImportant;
            LogLevel = WMessageLogLevel.Info;
        }

        public WMessage(string message, WMessageLogLevel level, Exception exceptionToLog = null)
        {
            Text = message;
            LogLevel = level;
            IsImportant = false;
            LoggedException = exceptionToLog;
        }

        public WMessage(StringKey message, WMessageLogLevel level, Exception exceptionToLog = null)
        {
            Text = null;
            TextTranslationKey = message;
            LogLevel = level;
            IsImportant = false;
            LoggedException = exceptionToLog;
        }


        public StringKey? TextTranslationKey { get; set; }
        public string Text { get; set; }
        public bool IsImportant { get; set; }
        public WMessageLogLevel LogLevel { get; set; }

        public Exception LoggedException { get; set; }
    }

    public interface IDownloader
    {
        void AddToDownloadQueue(DownloadItem download);
        void Download(string link, DownloadItem downloadInfo);
        void Download(IEnumerable<string> links, DownloadItem downloadInfo);
    }

    public static class Sys
    {
        private static Dictionary<Type, object> _single = new Dictionary<Type, object>();

        public static AppWrapper.LoaderContext _context;

        public static bool IsImporting = false;

        public static object CatalogLock = new object();

        public static T GetSingle<T>()
        {
            object o;
            _single.TryGetValue(typeof(T), out o);
            return (T)o;
        }

        public static void SetSingle<T>(T t)
        {
            _single[typeof(T)] = t;
        }

        public static Settings Settings { get; private set; }
        public static string _J8Folder { get; private set; }
        public static string _J8Exe { get; private set; }
        public static string SysFolder { get; private set; }
        public static string InstallPath
        {
            get
            {
                return Path.GetDirectoryName(Settings.FF8Exe);
            }
        }

        public static string PathToFF8Data
        {
            get
            {
                return Path.Combine(InstallPath, "data");
            }
        }

        public static string PathToFF8Textures
        {
            get
            {
                return Path.Combine(InstallPath, "mods", "Textures");
            }
        }

        public static string PathToApplog
        {
            get
            {
                return Path.Combine(SysFolder, "applog.txt");
            }
        }

        public static string PathToSettings
        {
            get
            {
                return Path.Combine(SysFolder, "settings.xml");
            }
        }

        public static string PathToCurrentProfileFile
        {
            get
            {
                return Path.Combine(PathToProfiles, $"{Settings.CurrentProfile}.xml");
            }
        }

        public static string PathToProfiles
        {
            get
            {
                return Path.Combine(SysFolder, "profiles");
            }
        }

        public static string PathToCacheFolder
        {
            get
            {
                return Path.Combine(SysFolder, "cache");
            }
        }

        public static string PathToTempFolder
        {
            get
            {
                return Path.Combine(SysFolder, "temp");
            }
        }

        public static string PathToLibraryTempFolder
        {
            get
            {
                return Path.Combine(Sys.Settings.LibraryLocation, "temp");
            }
        }

        public static string PathToCrashReports
        {
            get
            {
                return Path.Combine(SysFolder, "crashreports");
            }
        }

        public static string PathToControlsFolder
        {
            get
            {
                return Path.Combine(_J8Folder, "Resources", "Controls");
            }
        }

        public static string PathToGameDriverFolder
        {
            get
            {
                return Path.Combine(SysFolder, "GameDriver");
            }
        }

        public static string PathToPatchedExeFolder
        {
            get
            {
                return Path.Combine(_J8Folder, "Resources", "FF8_1.2_Eng_NVPatch");
            }
        }

        public static string PathToUlgpExe
        {
            get
            {
                return Path.Combine(_J8Folder, "Resources", "ulgp_v1.3.2", "ulgp.exe");
            }
        }

        public static string PathToVBusDriver
        {
            get
            {
                return Path.Combine(_J8Folder, "Resources", "VBusDriver");
            }
        }

        public static string PathToWinCDEmuExe
        {
            get
            {
                return Path.Combine(_J8Folder, "Resources", "WinCDEmu", "PortableWinCDEmu.exe");
            }
        }
        public static string PathToScpDriverExe
        {
            get
            {
                return Path.Combine(PathToVBusDriver, "ScpDriver.exe");
            }
        }
        public static string PathToFFNxToml
        {
            get
            {
                return Sys.InstallPath != null ? Path.Combine(Sys.InstallPath, "FFNx.toml") : null;
            }
        }

        public static string PathToFFNxLog
        {
            get
            {
                return Sys.InstallPath != null ? Path.Combine(Sys.InstallPath, "FFNx.log") : null;
            }
        }

        public static string PathToGameDriverUiXml(string appLanguage = null)
        {
            if (string.IsNullOrWhiteSpace(appLanguage) || appLanguage.StartsWith("en") || appLanguage.StartsWith("ja"))
            {
                return Path.Combine(Sys._J8Folder, "Resources", "J8_GameDriver_UI.xml");
            }
            else
            {
                appLanguage = (appLanguage == "pt-BR") ? "br" : appLanguage.Substring(0, 2);

                return Path.Combine(Sys._J8Folder, "Resources", "Languages", $"J8_GameDriver_UI.{appLanguage}.xml");
            }
        }

        public static Catalog Catalog { get; set; }
        public static Library Library { get; set; }
        public static ImageCache ImageCache { get; private set; }
        public static IDownloader Downloads { get; set; }
        public static Profile ActiveProfile { get; set; }
        public static Version AppVersion { get; set; }
        public static FFNxConfigManager FFNxConfig { get; private set; }


        public static event EventHandler<ModStatusEventArgs> StatusChanged;
        public static event EventHandler<MessageEventArgs> MessageReceived;

        private static Dictionary<Guid, ModStatus> _statuses;
        private static List<WMessage> _pendingMessages = new List<WMessage>();

        public static void Message(WMessage m)
        {
            if (MessageReceived != null)
            {
                foreach (var pending in _pendingMessages)
                    MessageReceived(null, new MessageEventArgs() { Message = pending });
                _pendingMessages.Clear();
                MessageReceived(null, new MessageEventArgs() { Message = m });
            }
            else
                _pendingMessages.Add(m);
        }

        public static void Ping(Guid modID)
        {
            ModStatus st;
            _statuses.TryGetValue(modID, out st);
            ModStatusEventArgs e = new ModStatusEventArgs() { ModID = modID, Status = st, OldStatus = st };
            StatusChanged?.Invoke(null, e);
        }

        public static void PingInfoChange(Guid modID)
        {
            ModStatusEventArgs e = new ModStatusEventArgs() { ModID = modID, Status = ModStatus.InfoChanged, OldStatus = ModStatus.InfoChanged };
            StatusChanged?.Invoke(null, e);
        }
        public static void SetStatus(Guid modID, ModStatus status)
        {
            ModStatus olds;
            _statuses.TryGetValue(modID, out olds);
            _statuses[modID] = status;
            ModStatusEventArgs e = new ModStatusEventArgs() { ModID = modID, Status = status, OldStatus = olds };
            StatusChanged?.Invoke(null, e);
        }
        public static void RevertStatus(Guid modID)
        {
            var lib = Library.GetItem(modID);
            SetStatus(modID, lib == null ? ModStatus.NotInstalled : ModStatus.Installed);
        }
        public static ModStatus GetStatus(Guid modID)
        {
            ModStatus s;
            _statuses.TryGetValue(modID, out s);
            return s;
        }

        /// <summary>
        /// Saves settings.xml, library.xml, and image cache.xml to disk
        /// </summary>
        public static void Save()
        {
            SaveLibrary();

            SaveSettings();

            ImageCache.Save();
        }

        /// <summary>
        /// Serializes <see cref="Library"/> and saves to library.xml on disk
        /// </summary>
        public static void SaveLibrary()
        {
            string lfile = Path.Combine(SysFolder, "library.xml");

            Directory.CreateDirectory(Path.GetDirectoryName(lfile));

            using (var fs = new FileStream(lfile, FileMode.Create))
                Util.Serialize(Library, fs);
        }

        /// <summary>
        /// Serializes <see cref="Settings"/> and saves to settings.xml
        /// </summary>
        public static void SaveSettings()
        {
            string sfile = PathToSettings;

            using (var fs = new FileStream(sfile, FileMode.Create))
                Util.Serialize(Settings, fs);
        }

        /// <summary>
        /// Updates <see cref="Catalog"/> to <paramref name="newCatalog"/>; 
        /// thread safe by locking the object
        /// </summary>
        /// <param name="newCatalog"></param>
        public static void SetNewCatalog(Catalog newCatalog)
        {
            lock (CatalogLock)
            {
                Catalog = newCatalog;
            }
        }

        /// <summary>
        /// Returns a Mod in <see cref="Catalog"/>
        /// ... uses <see cref="CatalogLock"/> to ensure the catalog does not change
        /// when multiple threads are accessing/modifying it at once.
        /// </summary>
        /// <param name="modId"></param>
        /// <returns></returns>
        public static Mod GetModFromCatalog(Guid modId)
        {
            Mod m = null;

            lock (CatalogLock)
            {
                m = Catalog.GetMod(modId);
            }

            return m;
        }

        static Sys()
        {
            _J8Exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            _J8Folder = Path.GetDirectoryName(_J8Exe);

            SysFolder = Path.Combine(_J8Folder, "J8Workshop");
            Directory.CreateDirectory(SysFolder);


            string sfile = PathToSettings;
            if (File.Exists(sfile))
            {
                try
                {
                    Settings = Util.Deserialize<Settings>(sfile);
                }
                catch(Exception e)
                {
                    Sys.Message(new WMessage(StringKey.ErrorLoadingSettingsPleaseConfigureJ8, WMessageLogLevel.Error, e));
                }
            }
            if (Settings == null)
            {
                Settings = Settings.UseDefaultSettings();
                Settings.IsFirstStart = true;
            }
            else
            {
                /* Force default catalog entries to the built-in URL to ensure catalog integrity */

                // Get default settings, including catalogs
                Settings defaultSettings = Settings.UseDefaultSettings();

                foreach(var sub in Settings.Subscriptions)
                {
                    foreach (var defSub in defaultSettings.Subscriptions)
                    {
                        if (defSub.Name == sub.Name)
                        {
                            sub.Url = defSub.Url;
                        }
                    }
                }
            }

            string lfile = Path.Combine(SysFolder, "library.xml");
            if (File.Exists(lfile))
            {
                try
                {
                    Library = Util.Deserialize<Library>(lfile);
                }
                catch(Exception e)
                {
                    Sys.Message(new WMessage(StringKey.ErrorLoadingLibraryFile, WMessageLogLevel.Error, e));
                }
            }

            if (Library == null)
                Library = new Library();

            if (Settings.HasOption(GeneralOptions.AutoUpdateMods))
            {
                Library.DefaultUpdate = UpdateType.Install;
            }

            _statuses = Library.Items.ToDictionary(i => i.ModID, _ => ModStatus.Installed);

            ImageCache = new ImageCache(PathToCacheFolder);

            AppVersion = new Version();

            FFNxConfig = new FFNxConfigManager();

            // Create the temp folder if does not exist
            if (!Directory.Exists(PathToTempFolder))
            {
                Directory.CreateDirectory(PathToTempFolder);
            }
            else
            {
                // Cleanup the temp folder and recreate it
                Directory.Delete(PathToTempFolder, true);
                Directory.CreateDirectory(PathToTempFolder);
            }
        }

        /// <summary>
        /// Opens applog.txt in default text editor program
        /// </summary>
        public static void OpenAppLog()
        {
            if (!File.Exists(Sys.PathToApplog))
            {
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(Sys.PathToApplog)
            {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }

        public static void OpenLibraryFolderInExplorer()
        {
            if (Directory.Exists(Settings.LibraryLocation))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(Settings.LibraryLocation)
                {
                    UseShellExecute = true,
                };
                Process.Start(startInfo);
            }
        }

        public static void InitLoaderContext()
        {
            _context = new AppWrapper.LoaderContext();

            rebuildVars();
        }

        private static List<AppWrapper.Variable> baseVars;

        public static void rebuildVars()
        {
            List<AppWrapper.Variable> vars;

            // construct dynamic vars file
            if (baseVars == null)
            {
                baseVars = new List<AppWrapper.Variable>();
                string baseVarFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Resources\\JunctionVIII.var");
                vars = new List<AppWrapper.Variable>();

                // create the list in memory
                foreach (string line in File.ReadAllLines(baseVarFile))
                {
                    string[] defVar = line.Split('=');
                    var currentVariable = new AppWrapper.Variable() { Name = defVar[0], Value = defVar[1] };
                    vars.Add(currentVariable);
                    baseVars.Add(currentVariable);
                }
            }
            else
            {
                vars = new List<AppWrapper.Variable>();
                baseVars.ForEach(var => vars.Add(var));
            }


            // go this installed mods
            foreach (var itm in Sys.Library.Items)
            {
                var mod = Sys.Library.GetItem(itm.ModID);
                if (mod == null) continue;
                string location = System.IO.Path.Combine(Sys.Settings.LibraryLocation, mod.LatestInstalled.InstalledLocation);

                // is this an IRO
                if (mod.LatestInstalled.InstalledLocation.EndsWith(".iroj"))
                {
                    try
                    {
                        using (var arc = new AppWrapper.IrosArc(location))
                        {
                            if (arc.HasFile("mod.xml"))
                            {
                                var doc = new System.Xml.XmlDocument();
                                doc.Load(arc.GetData("mod.xml"));
                                foreach (XmlNode xmlNode in doc.SelectNodes("/ModInfo/Variable"))
                                {
                                    vars.Add(new AppWrapper.Variable() { Name = xmlNode.Attributes.GetNamedItem("Name").Value, Value = xmlNode.InnerText.Trim() });
                                }
                            }
                        }
                    }catch(Exception) { }
                }
                else
                {
                    string mfile = System.IO.Path.Combine(location, "mod.xml");
                    if (System.IO.File.Exists(mfile))
                    {
                        var doc = new System.Xml.XmlDocument();
                        doc.Load(mfile);
                        foreach (XmlNode xmlNode in doc.SelectNodes("/ModInfo/Variable"))
                        {
                            vars.Add(new AppWrapper.Variable() { Name = xmlNode.Attributes.GetNamedItem("Name").Value, Value = xmlNode.InnerText.Trim() });
                        }
                    }

                }
            }

            _context.VarAliases = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (AppWrapper.Variable var in vars)
            {
                _context.VarAliases[var.Name] = var.Value;
            }
        }

        public static Task TryAutoImportModsAsync()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                TryAutoImportMods();
            });

            return t;
        }

        public static void TryAutoImportMods()
        {
            Sys.IsImporting = true;

            if (Sys.Settings.HasOption(GeneralOptions.AutoImportMods) && Directory.Exists(Sys.Settings.LibraryLocation))
            {
                foreach (string folder in Directory.GetDirectories(Sys.Settings.LibraryLocation))
                {
                    string name = Path.GetFileName(folder);
                    if (!name.EndsWith("temp", StringComparison.InvariantCultureIgnoreCase) && !Sys.Library.PendingDelete.Contains(name, StringComparer.InvariantCultureIgnoreCase))
                    {
                        if (!Sys.Library.Items.SelectMany(ii => ii.Versions).Any(v => v.InstalledLocation.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            Sys.Message(new WMessage("Trying to auto-import file " + folder, WMessageLogLevel.LogOnly));
                            try
                            {
                                string modName = ModImporter.ParseNameFromFileOrFolder(Path.GetFileNameWithoutExtension(folder));
                                ModImporter.ImportMod(folder, modName, false, true);
                            }
                            catch (Exception ex)
                            {
                                Sys.Message(new WMessage($"[{StringKey.FailedToImportMod}] {name}: " + ex.ToString(), true) { TextTranslationKey = StringKey.FailedToImportMod, LoggedException = ex });
                                continue;
                            }

                            Sys.Message(new WMessage() { Text = $"[{StringKey.AutoImportedMod}] {name}", TextTranslationKey = StringKey.AutoImportedMod });
                        }
                    }
                }

                foreach (string iro in Directory.GetFiles(Sys.Settings.LibraryLocation, "*.iroj"))
                {
                    string name = Path.GetFileName(iro);
                    if (!name.EndsWith("temp", StringComparison.InvariantCultureIgnoreCase) && !Sys.Library.PendingDelete.Contains(name, StringComparer.InvariantCultureIgnoreCase))
                    {
                        if (!Sys.Library.Items.SelectMany(ii => ii.Versions).Any(v => v.InstalledLocation.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            Sys.Message(new WMessage($"Trying to auto-import file {iro}", WMessageLogLevel.LogOnly));

                            try
                            {
                                string modName = ModImporter.ParseNameFromFileOrFolder(Path.GetFileNameWithoutExtension(iro));
                                ModImporter.ImportMod(iro, modName, true, true);
                            }
                            catch (AppWrapper.IrosArcException ae)
                            {
                                Sys.Message(new WMessage($"[{StringKey.CouldNotImportIroFileIsCorrupt}] - {Path.GetFileNameWithoutExtension(iro)}", true) { TextTranslationKey = StringKey.CouldNotImportIroFileIsCorrupt, LoggedException = ae });
                                continue;
                            }
                            catch (Exception ex)
                            {
                                Sys.Message(new WMessage($"[{StringKey.FailedToImportMod}] {name}: " + ex.ToString(), true) { TextTranslationKey = StringKey.FailedToImportMod, LoggedException = ex });
                                continue;
                            }

                            Sys.Message(new WMessage() { Text = $"[{StringKey.AutoImportedMod}] {name}", TextTranslationKey = StringKey.AutoImportedMod });
                        }
                    }
                }
            }

            // validate imported mod files exist - remove them if they do not exist
            ValidateAndRemoveDeletedMods();

            Sys.Library.AttemptDeletions();

            Sys.IsImporting = false;
        }

        public static bool ValidateAndRemoveDeletedMods()
        {
            foreach (InstalledItem mod in Sys.Library.Items.ToList())
            {
                if (!mod.ModExistsOnFileSystem())
                {
                    Sys.Library.RemoveInstall(mod);
                    Sys.ActiveProfile.Items.RemoveAll(p => p.ModID == mod.ModID);
                    Mod details = mod.CachedDetails ?? new Mod();
                    Sys.Message(new WMessage { Text = $"{details.Name} - [{StringKey.ModCouldNotBeFoundHasItBeenDeleted}]", TextTranslationKey = StringKey.ModCouldNotBeFoundHasItBeenDeleted });
                }
            }

            return false;
        }

        public static bool IsRunningAppAsAdministrator()
        {
            // reference: https://stackoverflow.com/questions/11660184/c-sharp-check-if-run-as-administrator
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
