/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using AppCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Iros.Workshop {

    public enum FF8Version
    {
        Unknown = -1,
        Steam,
        Original2K
    }

    public enum GeneralOptions {
        None = 0,
        KeepOldVersions,
        AutoActiveNewMods,
        AutoImportMods,
        CheckForUpdates,
        BypassCompatibility,
        OpenIrosLinksWithJ8,
        OpenModFilesWithJ8,
        ShowJ8InFileExplorerContextMenu,
        WarnAboutModCode,
        AutoUpdateMods,
        AutoSortMods
    }

    public enum AppUpdateChannelOptions
    {
        Stable = 0,
        Canary
    }

    public enum FFNxUpdateChannelOptions
    {
        Stable = 0,
        Canary
    }

    [Flags]
    public enum InterfaceOptions {
        None = 0,
        ProfileCollapse = 0x1,
    }

    public class SavedWindow {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public WindowState State { get; set; }
    }

    public class Subscription {
        public DateTime LastSuccessfulCheck { get; set; }
        public int FailureCount { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }

    }

    public class ProgramLaunchInfo
    {
        public string PathToProgram { get; set; }
        public string ProgramArgs { get; set; }
    }

    public class Settings {

        public IEnumerable<string> VerifySettings() {
            bool validexe = System.IO.File.Exists(FF8Exe);
            if (!validexe) yield return "FF8 Exe: " + FF8Exe + " not found";
            foreach (var al in ProgramsToLaunchPrior.Where(s => !String.IsNullOrWhiteSpace(s.PathToProgram)))
                if (!System.IO.File.Exists(al.PathToProgram)) yield return "AlsoLaunch: " + al.PathToProgram + " not found";
            if (validexe) {
                string ff8folder = System.IO.Path.GetDirectoryName(FF8Exe);
                foreach (string extra in ExtraFolders.Where(s => !String.IsNullOrWhiteSpace(s))) {
                    string path = System.IO.Path.Combine(ff8folder, extra);
                    if (!System.IO.Directory.Exists(path)) yield return "Extra Folder: " + path + " not found";
                }
            }
        }

        public string AppLanguage { get; set; }

        public List<string> SubscribedUrls
        {
            get
            {
                return Subscriptions?.Select(s => s.Url).ToList();
            }
        }

        public List<string> ExtraFolders { get; set; }
        public List<Subscription> Subscriptions { get; set; }

        public string LibraryLocation { get; set; } = string.Empty;
        
        public string FF8Exe { get; set; }
        [System.Xml.Serialization.XmlElement("AlsoLaunch")]
        public List<ProgramLaunchInfo> ProgramsToLaunchPrior { get; set; }
        public FF8Version FF8InstalledVersion { get; set; }
        public FFNxUpdateChannelOptions FFNxUpdateChannel { get; set; }
        public AppUpdateChannelOptions AppUpdateChannel { get; set; }
        public DateTime LastUpdateCheck { get; set; }
        public List<GeneralOptions> Options { get; set; }
        public InterfaceOptions IntOptions { get; set; }
        public string CurrentProfile { get; set; }
        public SavedWindow MainWindow { get; set; }
        public string AutoUpdateSource { get; set; }
        public decimal AutoUpdateOffered { get; set; }

        public string DateTimeStringFormat { get; set; }
        public string DateTimeCulture { get; set; }

        /// <summary>
        /// Flag to determine if the app is being launched for the first time.
        /// </summary>
        public bool IsFirstStart { get; set; }

        public LaunchSettings GameLaunchSettings { get; set; }

        public ColumnSettings UserColumnSettings { get; set; }

        public Settings() {
            ExtraFolders = new List<string>();
            ProgramsToLaunchPrior = new List<ProgramLaunchInfo>();
            Subscriptions = new List<Subscription>();
            Options = new List<GeneralOptions>();
            AutoUpdateSource = "#F!yBlHTYiJ!SFpmT2xII7iXcgXAmNYLJg";
            DateTimeStringFormat = "MM/dd/yyyy";
            DateTimeCulture = "en-US";
            IsFirstStart = false;
            GameLaunchSettings = LaunchSettings.DefaultSettings();
            UserColumnSettings = new ColumnSettings();
        }

        public bool HasOption(GeneralOptions option)
        {
            return Options != null && Options.Any(o => o == option);
        }

        public void RemoveOption(GeneralOptions option)
        {
            if (Options.Contains(option))
            {
                Options.Remove(option);
            }
        }

        public static Settings UseDefaultSettings()
        {
            Settings defaultSettings = new Settings();

            defaultSettings.Options.Add(GeneralOptions.AutoSortMods);
            defaultSettings.Options.Add(GeneralOptions.AutoImportMods);
            defaultSettings.Options.Add(GeneralOptions.AutoActiveNewMods);
            defaultSettings.Options.Add(GeneralOptions.WarnAboutModCode);
            defaultSettings.Options.Add(GeneralOptions.OpenIrosLinksWithJ8);
            defaultSettings.Options.Add(GeneralOptions.OpenModFilesWithJ8);
            defaultSettings.Options.Add(GeneralOptions.CheckForUpdates);

            defaultSettings.Subscriptions.Add(new Subscription() { Url = "iroj://Url/https$github.com/tsunamods-codes/Junction-VIII-Catalogs/releases/download/canary/tsunamods.xml", Name = "Tsunamods Catalog" });

            defaultSettings.ExtraFolders.Add("ambient");
            defaultSettings.ExtraFolders.Add("direct");
            defaultSettings.ExtraFolders.Add("save");
            defaultSettings.ExtraFolders.Add("sfx");
            defaultSettings.ExtraFolders.Add("voice");

            FileVersionInfo appVersion = FileVersionInfo.GetVersionInfo(Sys._J8Exe);
            if (appVersion.FilePrivatePart > 0 || appVersion.ProductPrivatePart > 0)
            {
                defaultSettings.FFNxUpdateChannel = FFNxUpdateChannelOptions.Canary;
                defaultSettings.AppUpdateChannel = AppUpdateChannelOptions.Canary;
            }
            else
            {
                defaultSettings.FFNxUpdateChannel = FFNxUpdateChannelOptions.Stable;
                defaultSettings.AppUpdateChannel = AppUpdateChannelOptions.Stable;
            }

            defaultSettings.UserColumnSettings = ColumnSettings.GetDefaultSettings();

            defaultSettings.FF8InstalledVersion = FF8Version.Unknown;

            return defaultSettings;
        }

        /// <summary>
        /// Sets the default paths for an FF8 Install i.e 'mods/Textures', 'mods/Junction VIII', 'data/movies'
        /// and creates the folders if they do not exist. Default extra folders like 'direct' and 'music' are also created
        /// </summary>
        /// <param name="pathToFf8Install"></param>
        public void SetPathsFromInstallationPath(string pathToFf8Install)
        {
            FF8Exe = Sys.Settings.FF8InstalledVersion == FF8Version.Original2K ? Path.Combine(pathToFf8Install, "FF8.exe") : Path.Combine(pathToFf8Install, "ff8_en.exe");
            if (LibraryLocation == string.Empty) LibraryLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Junction VIII");

            LogAndCreateFolderIfNotExists(LibraryLocation);

            foreach (string folder in Settings.UseDefaultSettings().ExtraFolders)
            {
                LogAndCreateFolderIfNotExists(Path.Combine(pathToFf8Install, folder));
            }
        }

        private static void LogAndCreateFolderIfNotExists(string pathToFolder)
        {
            if (!Directory.Exists(pathToFolder))
            {
                Sys.Message(new WMessage($"directory missing. creating {pathToFolder}", WMessageLogLevel.LogOnly));
                Directory.CreateDirectory(pathToFolder);
            }
        }
    }
}
