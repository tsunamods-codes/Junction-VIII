using AppCore;
using Iros;
using Iros.Workshop;
using Microsoft.Win32;
using AppUI.Classes;
using AppUI.Windows;
using AppUI;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AppUI.ViewModels
{
    public class GeneralSettingsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _fF7ExePathInput;
        private string _libraryPathInput;
        private bool _autoSortModsByDefault;
        private bool _autoUpdateModsByDefault;
        private bool _activateInstalledModsAuto;
        private bool _importLibraryFolderAuto;
        private bool _checkForUpdatesAuto;
        private bool _bypassCompatibilityLocks;
        private bool _openIrosLinks;
        private bool _openModFilesWithJ8;
        private bool _warnAboutModCode;
        private bool _showContextMenuInExplorer;

        private ObservableCollection<SubscriptionSettingViewModel> _subscriptionList;
        private string _newUrlText;
        private string _newNameText;
        private bool _isSubscriptionPopupOpen;
        private bool _isResolvingName;
        private string _subscriptionNameHintText;
        private bool _subscriptionNameTextBoxIsEnabled;
        private ObservableCollection<string> _extraFolderList;
        private string _statusMessage;

        private FFNxUpdateChannelOptions _ffnxUpdateChannel;
        private AppUpdateChannelOptions _appUpdateChannel;

        public delegate void OnListDataChanged();

        /// <summary>
        /// Event raised when data is changed (added/edited/removed) from <see cref="SubscriptionList"/>
        /// </summary>
        public event OnListDataChanged ListDataChanged;

        public string FF8ExePathInput
        {
            get
            {
                return _fF7ExePathInput;
            }
            set
            {
                _fF7ExePathInput = value;
                NotifyPropertyChanged();
            }
        }

        public string LibraryPathInput
        {
            get
            {
                return _libraryPathInput;
            }
            set
            {
                _libraryPathInput = value;
                NotifyPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                NotifyPropertyChanged();
            }
        }

        public FFNxUpdateChannelOptions FFNxUpdateChannel
        {
            get
            {
                return _ffnxUpdateChannel;
            }
            set
            {
                _ffnxUpdateChannel = value;
                NotifyPropertyChanged();
            }
        }

        public AppUpdateChannelOptions AppUpdateChannel
        {
            get
            {
                return _appUpdateChannel;
            }
            set
            {
                _appUpdateChannel = value;
                NotifyPropertyChanged();
            }
        }

        public bool AutoSortModsByDefault
        {
            get
            {
                return _autoSortModsByDefault;
            }
            set
            {
                _autoSortModsByDefault = value;
                NotifyPropertyChanged();
            }
        }

        public bool AutoUpdateModsByDefault
        {
            get
            {
                return _autoUpdateModsByDefault;
            }
            set
            {
                _autoUpdateModsByDefault = value;
                NotifyPropertyChanged();
            }
        }

        public bool ActivateInstalledModsAuto
        {
            get
            {
                return _activateInstalledModsAuto;
            }
            set
            {
                _activateInstalledModsAuto = value;
                NotifyPropertyChanged();
            }
        }

        public bool ImportLibraryFolderAuto
        {
            get
            {
                return _importLibraryFolderAuto;
            }
            set
            {
                _importLibraryFolderAuto = value;
                NotifyPropertyChanged();
            }
        }

        public bool CheckForUpdatesAuto
        {
            get
            {
                return _checkForUpdatesAuto;
            }
            set
            {
                _checkForUpdatesAuto = value;
                NotifyPropertyChanged();
            }
        }

        public bool BypassCompatibilityLocks
        {
            get
            {
                return _bypassCompatibilityLocks;
            }
            set
            {
                _bypassCompatibilityLocks = value;
                NotifyPropertyChanged();
            }
        }

        public bool OpenIrosLinks
        {
            get
            {
                return _openIrosLinks;
            }
            set
            {
                _openIrosLinks = value;
                NotifyPropertyChanged();
            }
        }

        public bool OpenModFilesWithJ8
        {
            get
            {
                return _openModFilesWithJ8;
            }
            set
            {
                _openModFilesWithJ8 = value;
                NotifyPropertyChanged();
            }
        }

        public bool WarnAboutModCode
        {
            get
            {
                return _warnAboutModCode;
            }
            set
            {
                _warnAboutModCode = value;
                NotifyPropertyChanged();
            }
        }

        public bool ShowContextMenuInExplorer
        {
            get
            {
                return _showContextMenuInExplorer;
            }
            set
            {
                _showContextMenuInExplorer = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<string> ExtraFolderList
        {
            get
            {
                if (_extraFolderList == null)
                    _extraFolderList = new ObservableCollection<string>();

                return _extraFolderList;
            }
            set
            {
                _extraFolderList = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsSubscriptionPopupOpen
        {
            get
            {
                return _isSubscriptionPopupOpen;
            }
            set
            {
                _isSubscriptionPopupOpen = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<SubscriptionSettingViewModel> SubscriptionList
        {
            get
            {
                if (_subscriptionList == null)
                    _subscriptionList = new ObservableCollection<SubscriptionSettingViewModel>();

                return _subscriptionList;
            }
            set
            {
                _subscriptionList = value;
                NotifyPropertyChanged();
            }
        }

        public bool SubscriptionsChanged { get; set; }

        private bool IsEditingSubscription { get; set; }

        public string SubscriptionNameHintText
        {
            get
            {
                return _subscriptionNameHintText;
            }
            set
            {
                _subscriptionNameHintText = value;
                NotifyPropertyChanged();
            }
        }

        public bool SubscriptionNameTextBoxIsEnabled
        {
            get
            {
                return _subscriptionNameTextBoxIsEnabled;
            }
            set
            {
                _subscriptionNameTextBoxIsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsResolvingName
        {
            get
            {
                return _isResolvingName;
            }
            set
            {
                _isResolvingName = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotResolvingName));
            }
        }

        public bool IsNotResolvingName
        {
            get
            {
                return !IsResolvingName;
            }
        }

        public string NewUrlText
        {
            get
            {
                return _newUrlText;
            }
            set
            {
                _newUrlText = value.Trim(new char[] { '\n', ' ', '\r' });
                NotifyPropertyChanged();
            }
        }

        public string NewNameText
        {
            get
            {
                return _newNameText;
            }
            set
            {
                _newNameText = value.Trim(new char[] { '\n', ' ', '\r' });
                NotifyPropertyChanged();
            }
        }

        public bool HasChangedInstalledModUpdateTypes { get; set; }

        public GeneralSettingsViewModel()
        {
            NewUrlText = "";
            NewNameText = "";
            SubscriptionsChanged = false;
            IsResolvingName = false;
            SubscriptionNameTextBoxIsEnabled = true;
            SubscriptionNameHintText = ResourceHelper.Get(StringKey.EnterNameForCatalog);
            HasChangedInstalledModUpdateTypes = false;
        }

        internal void ResetToDefaults()
        {
            LoadSettings(Settings.UseDefaultSettings());
        }

        internal void LoadSettings(Settings settings)
        {
            AutoDetectSystemPaths(settings);

            SubscriptionList = new ObservableCollection<SubscriptionSettingViewModel>(settings.Subscriptions.Select(s => new SubscriptionSettingViewModel(s.Url, s.Name)));
            ExtraFolderList = new ObservableCollection<string>(settings.ExtraFolders.ToList());

            FF8ExePathInput = settings.FF8Exe;
            LibraryPathInput = settings.LibraryLocation;

            FFNxUpdateChannel = settings.FFNxUpdateChannel;
            AppUpdateChannel = settings.AppUpdateChannel;

            AutoSortModsByDefault = settings.HasOption(GeneralOptions.AutoSortMods);
            AutoUpdateModsByDefault = settings.HasOption(GeneralOptions.AutoUpdateMods);
            ActivateInstalledModsAuto = settings.HasOption(GeneralOptions.AutoActiveNewMods);
            ImportLibraryFolderAuto = settings.HasOption(GeneralOptions.AutoImportMods);
            CheckForUpdatesAuto = settings.HasOption(GeneralOptions.CheckForUpdates);
            BypassCompatibilityLocks = settings.HasOption(GeneralOptions.BypassCompatibility);
            OpenIrosLinks = settings.HasOption(GeneralOptions.OpenIrosLinksWithJ8);
            OpenModFilesWithJ8 = settings.HasOption(GeneralOptions.OpenModFilesWithJ8);
            WarnAboutModCode = settings.HasOption(GeneralOptions.WarnAboutModCode);
            ShowContextMenuInExplorer = settings.HasOption(GeneralOptions.ShowJ8InFileExplorerContextMenu);
        }

        public static void AutoDetectSystemPaths(Settings settings)
        {
            string ff8 = null;
            bool isRunningInWine = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINELOADER"));

            if (string.IsNullOrEmpty(settings.FF8Exe) || !File.Exists(settings.FF8Exe) && !isRunningInWine)
            {
                Logger.Info("FF8 Exe path is empty or ff8.exe is missing. Auto detecting paths ...");

                try
                {
                    // Reset state
                    Sys.Settings.FF8InstalledVersion = FF8Version.Unknown;

                    // First try to autodetect the Steam installation if any
                    ff8 = GameConverter.GetInstallLocation(FF8Version.Steam);
                    Sys.Settings.FF8InstalledVersion = !string.IsNullOrWhiteSpace(ff8) ? FF8Version.Steam : FF8Version.Unknown;

                    // Finally as a last attempt try to autodetect the 2000 release
                    if (Sys.Settings.FF8InstalledVersion == FF8Version.Unknown)
                    {
                        // Try to detect 2000 game or a "converted" game from the old J8 game converter
                        string registry_path = $"{RegistryHelper.GetKeyPath(FF8RegKey.SquareSoftKeyPath)}\\Final Fantasy VIII\\1.00";
                        ff8 = (string)Registry.GetValue(registry_path, "AppPath", null);
                        Sys.Settings.FF8InstalledVersion = !string.IsNullOrWhiteSpace(ff8) ? FF8Version.Original2K : FF8Version.Unknown;

                        if (!Directory.Exists(ff8))
                        {
                            Logger.Warn($"Deleting invalid 'AppPath' registry key since path does not exist: {ff8}");
                            RegistryHelper.DeleteValueFromKey(registry_path, "AppPath"); // delete old paths set 
                            Sys.Settings.FF8InstalledVersion = FF8Version.Unknown; // set back to Unknown to check other registry keys
                        }
                    }

                    string versionStr = Sys.Settings.FF8InstalledVersion == FF8Version.Original2K ? $"{Sys.Settings.FF8InstalledVersion.ToString()} (or Game Converted)" : Sys.Settings.FF8InstalledVersion.ToString();

                    Logger.Info($"FF8Version Detected: {versionStr} with installation path: {ff8}");

                    if (!Directory.Exists(ff8))
                    {
                        Logger.Warn("Found installation path does not exist. Ignoring...");
                        return;
                    }

                }
                catch
                {
                    // could fail if game not installed
                }

                if (Sys.Settings.FF8InstalledVersion != FF8Version.Unknown)
                    settings.SetPathsFromInstallationPath(ff8);
                else
                    Logger.Warn("Could not detect the path to any FF8 installed copy.");
            }
            // User has given a ff8 exe path, try to guess which version it is
            else
            {
                if (settings.FF8Exe.ToLower().EndsWith("ff8_en.exe"))
                {
                    string ff8Launcher = Path.Combine(Path.GetDirectoryName(settings.FF8Exe), "FF8_Launcher.exe");

                    if (File.Exists(ff8Launcher)) Sys.Settings.FF8InstalledVersion = FF8Version.Steam;
                }
                else if(settings.FF8Exe.ToLower().EndsWith("ff8.exe"))
                {
                    // No previously converted edition detected, looks like a genuine 2000 edition
                    Sys.Settings.FF8InstalledVersion = FF8Version.Original2K;
                }
            }
        }

        internal bool SaveSettings(bool installFFNxIfMissing = false)
        {
            if (!ValidateSettings())
            {
                return false;
            }

            Sys.Settings.Subscriptions = GetUpdatedSubscriptions();
            Sys.Settings.ExtraFolders = ExtraFolderList.Distinct().ToList();

            // ensure required folders are always in ExtraFolders list
            if (!Sys.Settings.ExtraFolders.Contains("ambient", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("ambient");
            }

            if (!Sys.Settings.ExtraFolders.Contains("direct", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("direct");
            }

            if (!Sys.Settings.ExtraFolders.Contains("override", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("override");
            }

            if (!Sys.Settings.ExtraFolders.Contains("save", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("save");
            }

            if (!Sys.Settings.ExtraFolders.Contains("sfx", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("sfx");
            }

            if (!Sys.Settings.ExtraFolders.Contains("voice", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("voice");
            }

            if (!Sys.Settings.ExtraFolders.Contains("widescreen", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("widescreen");
            }

            if (!Sys.Settings.ExtraFolders.Contains("shaders", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("shaders");
            }

            Sys.Settings.FF8Exe = FF8ExePathInput;
            Sys.Settings.LibraryLocation = LibraryPathInput;
            Sys.Settings.FFNxUpdateChannel = FFNxUpdateChannel;
            Sys.Settings.AppUpdateChannel = AppUpdateChannel;

            Sys.Settings.Options = GetUpdatedOptions();

            ApplyOptions();

            Directory.CreateDirectory(Sys.Settings.LibraryLocation);

            Sys.Message(new WMessage(ResourceHelper.Get(StringKey.GeneralSettingsHaveBeenUpdated)));

            if (installFFNxIfMissing && !FFNxDriverUpdater.IsAlreadyInstalled())
            {
                try
                {
                    FFNxDriverUpdater updater = new FFNxDriverUpdater();

                    Sys.Message(new WMessage($"Downloading and extracting the latest FFNx {Sys.Settings.FFNxUpdateChannel} version to {Sys.InstallPath}..."));
                    updater.DownloadAndExtractLatestVersion(Sys.Settings.FFNxUpdateChannel);
                }
                catch (Exception ex)
                {
                    Sys.Message(new WMessage($"Something went wrong while attempting to install FFNx. See logs."));
                    Logger.Error(ex);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// applies various options based on what is enabled e.g. updating registry to associate files
        /// </summary>
        private void ApplyOptions()
        {
            RegistryHelper.BeginTransaction();

            // ensure direct and music folder exist since they are defaults and can not be removed
            foreach (string folder in Settings.UseDefaultSettings().ExtraFolders)
            {
                string pathToFf8 = Path.GetDirectoryName(Sys.Settings.FF8Exe);
                Directory.CreateDirectory(Path.Combine(pathToFf8, folder));
            }

            if (Sys.Settings.HasOption(GeneralOptions.OpenIrosLinksWithJ8))
            {
                AssociateIrosUrlWithJ8();
            }
            else
            {
                RemoveIrosUrlAssociationFromRegistry();
            }

            if (Sys.Settings.HasOption(GeneralOptions.OpenModFilesWithJ8))
            {
                AssociateIroFilesWithJ8();
            }
            else
            {
                RemoveIroFileAssociationFromRegistry();
            }

            if (Sys.Settings.HasOption(GeneralOptions.ShowJ8InFileExplorerContextMenu))
            {
                AssociateFileExplorerContextMenuWithJ8();
            }
            else
            {
                RemoveFileExplorerContextMenuAssociationWithJ8();
            }

            RegistryHelper.CommitTransaction();

            // ensure only applying option if it has changed
            if (Sys.Settings.HasOption(GeneralOptions.AutoUpdateMods) && Sys.Library.DefaultUpdate == UpdateType.Notify)
            {
                HasChangedInstalledModUpdateTypes = true;
                Sys.Library.DefaultUpdate = UpdateType.Install;

                foreach (InstalledItem item in Sys.Library.Items)
                {
                    item.UpdateType = UpdateType.Install;
                }

            }
            else if (!Sys.Settings.HasOption(GeneralOptions.AutoUpdateMods) && Sys.Library.DefaultUpdate == UpdateType.Install)
            {
                HasChangedInstalledModUpdateTypes = true;
                Sys.Library.DefaultUpdate = UpdateType.Notify;

                foreach (InstalledItem item in Sys.Library.Items)
                {
                    item.UpdateType = UpdateType.Notify;
                }
            }
        }

        /// <summary>
        /// returns list of options currently set to true.
        /// </summary>
        private List<GeneralOptions> GetUpdatedOptions()
        {
            List<GeneralOptions> newOptions = new List<GeneralOptions>();

            if (AutoSortModsByDefault)
                newOptions.Add(GeneralOptions.AutoSortMods);

            if (AutoUpdateModsByDefault)
                newOptions.Add(GeneralOptions.AutoUpdateMods);

            if (ActivateInstalledModsAuto)
                newOptions.Add(GeneralOptions.AutoActiveNewMods);

            if (ImportLibraryFolderAuto)
                newOptions.Add(GeneralOptions.AutoImportMods);

            if (CheckForUpdatesAuto)
                newOptions.Add(GeneralOptions.CheckForUpdates);

            if (BypassCompatibilityLocks)
                newOptions.Add(GeneralOptions.BypassCompatibility);

            if (OpenIrosLinks)
                newOptions.Add(GeneralOptions.OpenIrosLinksWithJ8);

            if (OpenModFilesWithJ8)
                newOptions.Add(GeneralOptions.OpenModFilesWithJ8);

            if (WarnAboutModCode)
                newOptions.Add(GeneralOptions.WarnAboutModCode);

            if (ShowContextMenuInExplorer)
                newOptions.Add(GeneralOptions.ShowJ8InFileExplorerContextMenu);


            return newOptions;
        }

        /// <summary>
        /// Returns list of Subscriptions based on the current input in <see cref="SubscriptionList"/>
        /// </summary>
        private List<Subscription> GetUpdatedSubscriptions()
        {
            List<Subscription> updatedSubscriptions = new List<Subscription>();

            foreach (SubscriptionSettingViewModel item in SubscriptionList.ToList())
            {
                var existingSub = Sys.Settings.Subscriptions.FirstOrDefault(s => s.Url == item.Url);

                if (existingSub == null)
                {
                    existingSub = new Subscription() { Name = item.Name, Url = item.Url };
                }
                else
                {
                    existingSub.Name = item.Name;
                }

                updatedSubscriptions.Add(existingSub);
            }

            return updatedSubscriptions;
        }

        private bool ValidateSettings(bool showMessage = true)
        {
            string validationMessage = "";
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(FF8ExePathInput))
            {
                validationMessage = ResourceHelper.Get(StringKey.MissingFf8ExePath);
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LibraryPathInput))
            {
                validationMessage = ResourceHelper.Get(StringKey.MissingLibraryPath);
                isValid = false;
            }

            if ((Path.GetDirectoryName(FF8ExePathInput) == LibraryPathInput) || Sys._J8Folder == LibraryPathInput)
            {
                validationMessage = ResourceHelper.Get(StringKey.LibraryPathCannotBeGameOrApp);
                isValid = false;
            }

            if (showMessage && !isValid)
            {
                MessageDialogWindow.Show(validationMessage, ResourceHelper.Get(StringKey.SettingsNotValid), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return isValid;
        }

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        /// <summary>
        /// Update Registry to associate .iroj mod files with J8
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateIroFilesWithJ8()
        {
            try
            {
                //Associate .iroj mod files with J8's Prog_ID- .iroj extension
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.iroj", "", $"JunctionVIIIIROJ");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.iroj\\DefaultIcon", "", $"\"{Sys._J8Exe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.iroj\\shell\\open\\command", "", $"\"{Sys._J8Exe}\" /OPENIRO:\"%1\"");

                // create registry keys to assocaite .irojp files
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.irojp", "", $"JunctionVIIIIROJP");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.irojp\\DefaultIcon", "", $"\"{Sys._J8Exe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.irojp\\shell\\open\\command", "", $"\"{Sys._J8Exe}\" /OPENIROP:\"%1\"");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRegisterIroModFilesWithJunctionVIII)));
                return false;
            }
        }

        /// <summary>
        /// Deletes Registry keys/values (if they exist) to unassociate .iroj mod files with J8
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        private static bool RemoveIroFileAssociationFromRegistry()
        {
            try
            {
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\.iroj");
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\.irojp");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToUnregisterIroModFilesWithJunctionVIII)));
                return false;
            }
        }

        /// <summary>
        /// Update Registry to asssociate iroj:// URL with J8
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateIrosUrlWithJ8()
        {
            try
            {
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iroj", "", $"J8 Catalog Subscription");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iroj", "URL Protocol", $"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iroj\\DefaultIcon", "", $"\"{Sys._J8Exe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iroj\\shell\\open\\command", "", $"\"{Sys._J8Exe}\" \"%1\"");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRegisterIrosLinksWithJunctionVIII)));
                return false;
            }
        }

        /// <summary>
        /// Deletes Registry key/values (if they exist) to unasssociate iroj:// URL with J8
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        /// <returns></returns>
        private static bool RemoveIrosUrlAssociationFromRegistry()
        {
            try
            {
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\iroj");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToUnregisterIrosLinksWithJunctionVIII)));
                return false;
            }
        }

        /// <summary>
        /// Update Registry to add Context menu options to Windows File Explorer
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateFileExplorerContextMenuWithJ8()
        {
            try
            {
                // create registry keys for 'Pack IRO' for folders
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\Directory\\shell\\Pack into IROJ", "Icon", $"\"{Sys._J8Exe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\Directory\\shell\\Pack into IROJ\\command", "", $"\"{Sys._J8Exe}\" /PACKIRO:\"%1\"");

                // create registry keys for 'Unpack IROJ' for files
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\JunctionVIIIIROJ\\shell\\Unpack IROJ", "Icon", $"\"{Sys._J8Exe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\JunctionVIIIIROJ\\shell\\Unpack IROJ\\command", "", $"\"{Sys._J8Exe}\" /UNPACKIRO:\"%1\"");

                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToCreateJunctionVIIIContextMenuEntries), WMessageLogLevel.Error, e));
                return false;
            }
        }

        /// <summary>
        /// Deletes Registry key/values (if they exist) to unasssociate context menu options from Windows File Explorer
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        /// <returns></returns>
        private static bool RemoveFileExplorerContextMenuAssociationWithJ8()
        {
            try
            {
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\Directory\\shell\\Pack into IROJ");
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\JunctionVIIIIROJ\\shell\\Unpack IROJ");

                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRemoveJunctionVIIIContextMenuEntries), WMessageLogLevel.Error, e));
                return false;
            }
        }

        internal void EditSelectedSubscription(SubscriptionSettingViewModel selected)
        {
            IsEditingSubscription = true;
            IsSubscriptionPopupOpen = true;
            NewUrlText = selected.Url;
            NewNameText = selected.Name ?? "";
        }

        internal void AddNewSubscription()
        {
            IsEditingSubscription = false;
            SubscriptionNameTextBoxIsEnabled = false;
            SubscriptionNameHintText = ResourceHelper.Get(StringKey.CatalogNameWillAutoResolveOnSave);
            IsSubscriptionPopupOpen = true;
            string clipboardContent = "";

            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                clipboardContent = Clipboard.GetText(TextDataFormat.Text);
            }

            if (!string.IsNullOrWhiteSpace(clipboardContent) && clipboardContent.StartsWith("iroj://"))
            {
                NewUrlText = clipboardContent;
            }
        }

        /// <summary>
        /// Adds or Edits subscription and closes subscription popup
        /// </summary>
        internal bool SaveSubscription()
        {
            if (!NewUrlText.StartsWith("iroj://"))
            {
                StatusMessage = ResourceHelper.Get(StringKey.UrlMustBeInIrosFormat);
                return false;
            }

            if (!SubscriptionList.Any(s => s.Url == NewUrlText))
            {
                IsResolvingName = true;
                SubscriptionNameHintText = ResourceHelper.Get(StringKey.ResolvingCatalogName);
                ResolveCatalogNameFromUrl(NewUrlText, resolvedName =>
                {
                    NewNameText = resolvedName;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SubscriptionList.Add(new SubscriptionSettingViewModel(NewUrlText, NewNameText));
                        CloseSubscriptionPopup();
                        IsResolvingName = false;
                        ListDataChanged?.Invoke();
                    });
                });
            }
            else if (IsEditingSubscription)
            {
                SubscriptionSettingViewModel toEdit = SubscriptionList.FirstOrDefault(s => s.Url == NewUrlText);
                toEdit.Name = NewNameText;
                CloseSubscriptionPopup();
                ListDataChanged?.Invoke();
            }
            else
            {
                // if user is trying to add a url that already exists in list then just close popup
                CloseSubscriptionPopup();
                return true;
            }

            SubscriptionsChanged = true;
            return true;
        }

        internal void CloseSubscriptionPopup()
        {
            IsEditingSubscription = false;
            IsSubscriptionPopupOpen = false;
            NewUrlText = "";
            NewNameText = "";
            SubscriptionNameTextBoxIsEnabled = true;
            SubscriptionNameHintText = ResourceHelper.Get(StringKey.EnterNameForCatalog);
        }

        internal void MoveSelectedSubscription(SubscriptionSettingViewModel selected, int toAdd)
        {
            int currentIndex = SubscriptionList.IndexOf(selected);

            if (currentIndex < 0)
            {
                // not found in  list
                return;
            }

            int newIndex = currentIndex + toAdd;

            if (newIndex == currentIndex || newIndex < 0 || newIndex >= SubscriptionList.Count)
            {
                return;
            }

            SubscriptionList.Move(currentIndex, newIndex);
            SubscriptionsChanged = true;
        }

        internal void RemoveSelectedSubscription(SubscriptionSettingViewModel selected)
        {
            SubscriptionsChanged = true;
            SubscriptionList.Remove(selected);
            ListDataChanged?.Invoke();
        }

        /// <summary>
        /// Downloads catalog.xml to temp file and gets Name of the catalog. 
        /// resolved name gets passed to delegate method that is called after download
        /// </summary>
        /// <param name="catalogUrl"></param>
        /// <param name="callback"></param>
        internal static void ResolveCatalogNameFromUrl(string catalogUrl, Action<string> callback)
        {
            string name = "";

            string uniqueFileName = $"cattemp{Path.GetRandomFileName()}.xml"; // save temp catalog update to unique filename so multiple catalog updates can download async
            string path = Path.Combine(Sys.PathToTempFolder, uniqueFileName);

            Action onCancel = () =>
            {
                // delete temp file on cancel if it exists
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                callback(name);
            };

            Action<Exception> onError = ex =>
            {
                callback("");
            };

            Install.InstallProcedureCallback downloadCallback = new Install.InstallProcedureCallback(e =>
            {
                bool success = (e.Error == null && e.Cancelled == false);

                if (success)
                {
                    try
                    {
                        Catalog c = Util.Deserialize<Catalog>(path);
                        name = c.Name ?? "";

                        // delete temp file if it exists
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to deserialize catalog - {ex.Message}");
                    }
                }

                callback(name);
            });
            downloadCallback.Error = onError;

            DownloadItem download = new DownloadItem()
            {
                Links = new List<string>() { catalogUrl },
                SaveFilePath = path,
                Category = DownloadCategory.Catalog,
                ItemName = $"{ResourceHelper.Get(StringKey.ResolvingCatalogNameFor)} {catalogUrl}",
                IProc = downloadCallback,
                OnCancel = onCancel
            };

            Sys.Downloads.AddToDownloadQueue(download);
        }

        internal void AddExtraFolder()
        {
            string initialDir = File.Exists(FF8ExePathInput) ? Path.GetDirectoryName(FF8ExePathInput) : "";
            string pathToFolder = FileDialogHelper.BrowseForFolder("", initialDir);

            if (!string.IsNullOrWhiteSpace(pathToFolder))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(pathToFolder);
                string folderName = dirInfo.Name.ToLower();

                if (!ExtraFolderList.Contains(folderName))
                {
                    ExtraFolderList.Add(folderName);
                }
            }
        }

        internal void MoveSelectedFolder(string selected, int toAdd)
        {
            int currentIndex = ExtraFolderList.IndexOf(selected);

            if (currentIndex < 0)
            {
                // not found in  list
                return;
            }

            int newIndex = currentIndex + toAdd;

            if (newIndex == currentIndex || newIndex < 0 || newIndex >= ExtraFolderList.Count)
            {
                return;
            }

            ExtraFolderList.Move(currentIndex, newIndex);
        }
    }
}
