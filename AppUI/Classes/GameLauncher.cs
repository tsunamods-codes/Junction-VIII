using AppCore;
using AppWrapper;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using Iros;
using Iros.Workshop;
using Microsoft.Win32;
using AppUI.Windows;
using AppUI.ViewModels;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Profile = Iros.Workshop.Profile;

namespace AppUI.Classes
{
    internal enum GraphicsRenderer
    {
        SoftwareRenderer = 0,
        D3DHardwareAccelerated = 1,
        CustomDriver = 3
    }

    /// <summary>
    /// Responsibile for the entire process that happens for launching the game
    /// </summary>
    public class GameLauncher
    {
        #region Data Members and Properties

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static GameLauncher _instance;

        public static GameLauncher Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameLauncher();

                return _instance;
            }
        }

        private Dictionary<string, Process> _alsoLaunchProcesses = new Dictionary<string, Process>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<AppWrapper.ProgramInfo, Process> _sideLoadProcesses = new Dictionary<AppWrapper.ProgramInfo, Process>();

        private ControllerInterceptor _controllerInterceptor;

        public delegate void OnProgressChanged(string message);
        public event OnProgressChanged ProgressChanged;

        public delegate void OnLaunchCompleted(bool wasSuccessful);
        public event OnLaunchCompleted LaunchCompleted;


        public string DriveLetter { get; set; }
        public bool DidMountVirtualDisc { get; set; }

        #endregion

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_FORCEMINIMIZE = 11;
        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        private static Process ff8Proc;

        public static async Task<bool> LaunchGame(bool varDump, bool debug, bool launchWithNoMods = false, bool launchChocobo = false)
        {
            bool runAsVanilla = false;
            string vanillaMsg = "";
            RuntimeProfile runtimeProfile = null;

            MainWindowViewModel.SaveActiveProfile();
            Sys.Save();

            GameConverter converter = new GameConverter(Path.GetDirectoryName(Sys.Settings.FF8Exe));
            converter.MessageSent += GameConverter_MessageSent;

            // Check for DEP
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckForSystemDEPStatus));
            switch (AppWrapper.Win32.GetSystemDEPPolicy())
            {
                case AppWrapper.Win32.DEP_SYSTEM_POLICY_TYPE.DEPPolicyAlwaysOn:
                case AppWrapper.Win32.DEP_SYSTEM_POLICY_TYPE.DEPPolicyOptOut:
                    if (MessageDialogWindow.Show(ResourceHelper.Get(StringKey.DoYouWantToDisableDEP), ResourceHelper.Get(StringKey.DEPDetected), MessageBoxButton.YesNo, MessageBoxImage.Warning).Result == MessageBoxResult.Yes)
                    {
                        string fileName = Path.Combine(Sys.PathToTempFolder, "fixdep.bat");

                        System.IO.File.WriteAllText(
                            fileName,
                            $@"@echo off
@bcdedit /set nx OptIn
"
                        );

                        try
                        {
                            // Execute temp batch script with admin privileges
                            ProcessStartInfo startInfo = new ProcessStartInfo(fileName)
                            {
                                CreateNoWindow = false,
                                UseShellExecute = true,
                                Verb = "runas"
                            };

                            // Launch process, wait and then save exit code
                            using (Process temp = Process.Start(startInfo))
                            {
                                temp.WaitForExit();
                            }

                            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SystemDEPDisabledPleaseReboot), NLog.LogLevel.Info);
                        }
                        catch (Exception)
                        {
                            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SomethingWentWrongWhileDisablingDEP), NLog.LogLevel.Error);
                        }
                    }
                    else
                    {
                        Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CannotContinueWithDEPEnabled), NLog.LogLevel.Warn);
                    }
                    return false;
            }

            //
            // Ensure FFNx is installed correctly
            //
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingLatestGameDriverIsInstalled));
            if (!FFNxDriverUpdater.IsAlreadyInstalled())
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SomethingWentWrongTryingToDetectGameDriver), NLog.LogLevel.Error);
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingFf8IsNotRunning));
            if (IsFF8Running())
            {
                string title = ResourceHelper.Get(StringKey.Ff8IsAlreadyRunning);
                string message = ResourceHelper.Get(StringKey.Ff8IsAlreadyRunningDoYouWantToForceClose);

                Instance.RaiseProgressChanged($"\t{title}", NLog.LogLevel.Warn);

                var result = MessageDialogWindow.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result.Result == MessageBoxResult.Yes)
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ForceClosingAllInstancesFf8)}", NLog.LogLevel.Info);

                    if (!ForceKillFF8())
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCloseProcess)} {Path.GetFileName(Sys.Settings.FF8Exe)}. {ResourceHelper.Get(StringKey.Aborting)}", NLog.LogLevel.Error);
                        return false;
                    }
                }
            }

            Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.CheckingFf8ExeExistsAt)} {Sys.Settings.FF8Exe} ...");
            if (!File.Exists(Sys.Settings.FF8Exe))
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FileNotFoundAborting)}", NLog.LogLevel.Error);
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.Ff8ExeNotFoundYouMayNeedToConfigure));
                return false;
            }

            //
            // GAME SANITY CHECK - Make sure the user has at least run once the Steam game
            //
            if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
            {
                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.CheckingFF8SteamInstalledCorrectly)}...");
                if (!Path.Exists(GameConverter.GetSteamFF8UserPath()) || Directory.EnumerateDirectories(GameConverter.GetSteamFF8UserPath(), "user_*").Count() <= 0)
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FF8SteamNotInstalledCorrectly)}", NLog.LogLevel.Warn);
                    var result = MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FF8SteamNotInstalledCorrectly), "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (result.Result == MessageBoxResult.OK)
                    {
                        runAsVanilla = true;
                        goto LaunchGame;
                    }
                }
            }

            //
            // GAME CONVERTER - Make sure game is ready for mods
            //
            FFNxDriverUpdater.CleanupUnnecessaryFiles();
            ReShadeUpdater.Cleanup();

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingInstalledGameIsCompatible));
            if (converter.IsGamePirated())
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ErrorCodeYarr), NLog.LogLevel.Error);
                Logger.Info(FileUtils.ListAllFiles(converter.InstallPath));
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CreatingMissingRequiredDirectories));
            converter.CreateMissingDirectories();

            if (Sys.Settings.FF8InstalledVersion == FF8Version.Original2K)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingGameIsMaxInstall));
                if (!converter.VerifyFullInstallation())
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.YourFf8InstallationFolderIsMissingCriticalFiles), NLog.LogLevel.Error);
                    return false;
                }
            }

            string backupFolderPath = Path.Combine(converter.InstallPath, GameConverter.BackupFolderName, DateTime.Now.ToString("yyyyMMddHHmmss"));

            if (Sys.Settings.FF8InstalledVersion == FF8Version.Original2K)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingFf8Exe));
                if (new FileInfo(Sys.Settings.FF8Exe).Name.Equals("ff8.exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    // only compare exes are different if ff8.exe set in Settings (and not something like ff8_bc.exe)
                    if (converter.IsExeDifferent())
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.Ff8ExeDetectedToBeDifferent)}");
                        if (converter.BackupExe(backupFolderPath))
                        {
                            bool didCopy = converter.CopyFF8ExeToGame();
                            if (!didCopy)
                            {
                                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCopyFf8Exe)}", NLog.LogLevel.Error);
                                return false;
                            }
                        }
                        else
                        {
                            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCreateBackupOfFf8Exe)}", NLog.LogLevel.Error);
                            return false;
                        }
                    }
                }
                converter.Ensure102PatchApplied();
            }

            // Auto-patch for 4GB support
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.App4GBPatchRequired));
            PEFile file = PEFile.FromFile(Sys.Settings.FF8Exe);
            if (!file.FileHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware))
            {
                converter.BackupExe(backupFolderPath);
                file.FileHeader.Characteristics |= Characteristics.LargeAddressAware;
                file.Write(Sys.Settings.FF8Exe);
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.App4GBPatchApplied));
            }

            //
            // Copy the new launcher to the game path
            //
            if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingFf8LauncherExe));

                string src = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string dest = Path.GetDirectoryName(Sys.Settings.FF8Exe);
                File.Copy(Path.Combine(src, "AppLauncher.exe"), Path.Combine(dest, "FF8_Launcher.exe"), true);
            }

            //
            // Get Drive Letter and update registry
            //
            if (Sys.Settings.FF8InstalledVersion == FF8Version.Original2K)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.LookingForGameDisc));
                Instance.DriveLetter = GetDriveLetter();

                if (!string.IsNullOrEmpty(Instance.DriveLetter))
                {
                    Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.FoundGameDiscAt)} {Instance.DriveLetter} ...");
                }
                else
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.FailedToFindGameDisc), NLog.LogLevel.Error);
                    return false;
                }

                //
                // Update Registry with new launch settings
                //
                Instance.SetRegistryValues();
            }

            //
            // GAME SHOULD BE FULLY 'CONVERTED' AND READY TO LAUNCH FOR MODS AT THIS POINT
            //
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingAProfileIsActive));
            if (Sys.ActiveProfile == null)
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ActiveProfileNotFound)}", NLog.LogLevel.Error);
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CreateAProfileFirstAndTryAgain));
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingModCompatibilityRequirements));
            if (!SanityCheckCompatibility())
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedModCompatibilityCheck)}", NLog.LogLevel.Error);
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingModConstraintsForCompatibility));
            if (!SanityCheckSettings())
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedModConstraintCheck)}", NLog.LogLevel.Error);
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingModLoadOrderRequirements));
            if (!VerifyOrdering())
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedModLoadOrderCheck)}", NLog.LogLevel.Error);
                return false;
            }

            //
            // Copy input.cfg to FF8
            //
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CopyingFf8InputCfgToFf8Path));
            bool didCopyCfg = CopyKeyboardInputCfg();

            // Copy Reshade
            ReShadeUpdater.Install();

            //
            // Determine if game will be ran as 'vanilla' with mods so don't have to inject with AppLoader
            //
            if (launchWithNoMods)
            {
                vanillaMsg = ResourceHelper.Get(StringKey.UserRequestedToPlayWithNoModsLaunchingGameAsVanilla);
                runAsVanilla = true;
            }
            else if (Sys.ActiveProfile.ActiveItems.Count == 0)
            {
                vanillaMsg = ResourceHelper.Get(StringKey.NoModsActivatedLaunchingGameAsVanilla);
                runAsVanilla = true;
            }

            if (!runAsVanilla)
            {
                //
                // Create Runtime Profile for Active Mods
                //
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CreatingRuntimeProfile));
                runtimeProfile = CreateRuntimeProfile();

                if (runtimeProfile == null)
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCreateRuntimeProfileForActiveMods)}", NLog.LogLevel.Error);
                    return false;
                }

                if (!launchChocobo)
                {
                    //
                    // Copy J8Wrapper* dlls to FF8
                    //
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CopyingEasyHookToFf8PathIfNotFoundOrOlder));
                    CopyJ8WrapperDlls();
                }

                //
                // Inherit FFNx Config keys from each mod
                //
                Sys.FFNxConfig.Reload();
                Sys.FFNxConfig.Backup(true);
                Sys.FFNxConfig.OverrideInternalKeys(debug && runtimeProfile != null);
                foreach (RuntimeMod mod in runtimeProfile.Mods)
                {
                    foreach(FFNxFlag flag in mod.FFNxConfig)
                    {
                        bool addConfig = true;

                        if (flag.Attributes.Count > 0)
                        {
                            foreach (var attr in flag.Attributes)
                            {
                                foreach(Iros.Workshop.ProfileItem item in Sys.ActiveProfile.ActiveItems)
                                {
                                    foreach(ProfileSetting setting in item.Settings)
                                    {
                                        if (setting.ID == attr.Key && setting.Value != attr.Value) addConfig = false;
                                    }
                                }
                            }
                        }

                        if (addConfig)
                        {
                            if (Sys.FFNxConfig.HasKey(flag.Key))
                            {
                                if (flag.Values.Count > 0)
                                {
                                    Sys.FFNxConfig.Set(flag.Key, flag.Values);
                                }
                                else
                                {
                                    Sys.FFNxConfig.Set(flag.Key, flag.Value);
                                }
                            }
                        }
                    }
                }
                Sys.FFNxConfig.Save();
            }
            else
            {
                Sys.FFNxConfig.Reload();
                Sys.FFNxConfig.Backup(true);
                Sys.FFNxConfig.OverrideInternalKeys(debug);
                Sys.FFNxConfig.Save();
            }

            //
            // Setup log file if debugging
            //
            if (debug && runtimeProfile != null)
            {
                runtimeProfile.Options |= RuntimeOptions.DetailedLog;
                runtimeProfile.LogFile = Path.Combine(Path.GetDirectoryName(Sys.Settings.FF8Exe), "log.txt");

                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.DebugLoggingSetToTrueDetailedLoggingWillBeWrittenTo)} {runtimeProfile.LogFile} ...");
            }

            //
            // Initialize the controller input interceptor
            //
            try
            {
                if (Sys.Settings.GameLaunchSettings.EnableGamepadPolling)
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.BeginningToPollForGamePadInput));

                    if (Instance._controllerInterceptor == null)
                    {
                        Instance._controllerInterceptor = new ControllerInterceptor();
                    }

                    await Instance._controllerInterceptor.PollForGamepadInput().ContinueWith((result) =>
                    {
                        if (result.IsFaulted)
                        {
                            Logger.Error(result.Exception);
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ReceivedUnknownError)}: {e.Message} ...", NLog.LogLevel.Warn);
            }

        LaunchGame:
            // start FF8 proc as normal and return true when running the game as vanilla
            if (runAsVanilla)
            {
                Instance.RaiseProgressChanged(vanillaMsg);
                if (launchChocobo)
                    await LaunchChocoboExe();
                else
                    await LaunchFF8Exe();

                return true;
            }

            //
            // Attempt to Create FF8 Proc and Inject with AppLoader
            //
            try
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.LaunchingAdditionalProgramsToRunIfAny));
                Instance.LaunchAdditionalProgramsToRunPrior();

                RuntimeParams parms = new RuntimeParams
                {
                    ProfileFile = Path.Combine(Path.GetDirectoryName(Sys.Settings.FF8Exe), ".J8WrapperProfile")
                };

                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.WritingTemporaryRuntimeProfileFileTo)} {parms.ProfileFile} ...");

                using (FileStream fs = new FileStream(parms.ProfileFile, FileMode.Create))
                    Util.SerializeBinary(runtimeProfile, fs);

                // attempt to launch the game a few times in the case of an ApplicationException that can be thrown by AppLoader it seems randomly at times
                // ... The error tends to go away the second time trying but we will try multiple times before failing
                bool didInject = false;

                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.AttemptingToInjectWithEasyHook));

                while (!didInject)
                {
                    try
                    {
                        if (launchChocobo)
                            await LaunchChocoboExe();
                        else
                            await LaunchFF8Exe();
                        didInject = true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            ff8Proc.Kill();
                        }catch { }
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ReceivedUnknownError)}: {e.Message} ...", NLog.LogLevel.Warn);
                    }
                }

                if (!didInject)
                {
                    Sys.FFNxConfig.RestoreBackup(true);
                    return false;
                }


                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.GettingFf8Proc));

                if (ff8Proc == null)
                {
                    Sys.FFNxConfig.RestoreBackup(true);

                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToGetFf8Proc)}", NLog.LogLevel.Error);
                    return false;
                }

                ff8Proc.EnableRaisingEvents = true;

                if (debug)
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.DebugLoggingSetToTrueWiringUpLogFile));
                    ff8Proc.Exited += (o, e) =>
                    {
                        try
                        {
                            DebugLogger.CloseLogFile();

                            ProcessStartInfo debugTxtProc = new ProcessStartInfo()
                            {
                                WorkingDirectory = Path.GetDirectoryName(runtimeProfile.LogFile),
                                FileName = runtimeProfile.LogFile,
                                UseShellExecute = true
                            };
                            Process.Start(debugTxtProc);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.FailedToStartProcessForDebugLog)}: {ex.Message}", NLog.LogLevel.Error);
                        }
                    };
                }

                //
                // Start Turbolog for Variable Dump
                //
                if (varDump)
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VariableDumpSetToTrueStartingTurboLog));
                    StartTurboLogForVariableDump(runtimeProfile);
                }

                /// sideload programs for mods before starting FF8 because FF8 losing focus while initializing can cause the intro movies to stop playing
                /// ... Thus we load programs first so they don't steal window focus
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.StartingProgramsForMods));
                foreach (RuntimeMod mod in runtimeProfile.Mods)
                {
                    Instance.LaunchProgramsForMod(mod);
                }

                // wire up process to stop plugins and side processes when proc has exited
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SettingUpFf8ExeToStopPluginsAndModPrograms));
                ff8Proc.Exited += (o, e) =>
                {
                    if (!IsFF8Running() && Instance._controllerInterceptor != null)
                    {
                        // stop polling for input once all ff8 procs are closed (could be multiple instances open)
                        Instance._controllerInterceptor.PollingInput = false;
                    }

                    // wrapped in try/catch so an unhandled exception when exiting the game does not crash J8
                    try
                    {
                        Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.StoppingOtherProgramsForMods));
                        Instance.StopAllSideProcessesForMods();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    // Did the game crash? If yes, generate a report
                    if (ff8Proc.ExitCode < 0)
                    {
                        Logger.Error($"FF8.exe Crashed while exiting with code 0x{ff8Proc.ExitCode.ToString("X8")}. Generating report...");
                        Instance.CollectCrashReport();
                    }

                    // Restore FFNx config after the game is closed
                    Sys.FFNxConfig.RestoreBackup(true);
                };
            

                // ensure ff8 window is active at end of launching
                if (ff8Proc.MainWindowHandle != IntPtr.Zero)
                {
                    int secondsToWait = 120;
                    // setting the ff8 proc as the foreground window makes it the active window and thus can start processing mods (this will usually cause a 'Not Responding...' window when loading a lot of mods)
                    SetForegroundWindow(ff8Proc.MainWindowHandle);
                    Instance.RaiseProgressChanged(string.Format(ResourceHelper.Get(StringKey.WaitingForFf8ExeToRespond), secondsToWait));

                    // after setting as active window, wait to ensure the window loads all mods and becomes responsive
                    DateTime start = DateTime.Now;
                    while (ff8Proc.Responding == false)
                    {
                        TimeSpan elapsed = DateTime.Now.Subtract(start);
                        if (elapsed.TotalSeconds > secondsToWait)
                            break;

                        ff8Proc.Refresh();
                    }

                    SetForegroundWindow(ff8Proc.MainWindowHandle); // activate window again
                }

                return true;
            }
            catch (Exception e)
            {
                Sys.FFNxConfig.RestoreBackup(true);

                Logger.Error(e);

                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ExceptionOccurredWhileTryingToStart), NLog.LogLevel.Error);
                MessageDialogWindow.Show(e.ToString(), ResourceHelper.Get(StringKey.ErrorFailedToStartGame), MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
            finally
            {
                converter.MessageSent -= GameConverter_MessageSent;
            }
        }

        private static void GameConverter_MessageSent(string message, NLog.LogLevel logLevel)
        {
            Instance.RaiseProgressChanged(message, logLevel);
        }

        internal static RuntimeProfile CreateRuntimeProfile()
        {
            List<RuntimeMod> runtimeMods = null;

            try
            {
                runtimeMods = Sys.ActiveProfile.ActiveItems.Select(i => i.GetRuntime(Sys._context))
                                                           .Where(i => i != null)
                                                           .ToList();
            }
            catch (Exception)
            {
                throw;
            }


            RuntimeProfile runtimeProfiles = new RuntimeProfile()
            {
                MonitorPaths = new List<string>() {
                    Sys.PathToFF8Data,
                    Sys.PathToFF8Textures,
                },
                ModPath = Sys.Settings.LibraryLocation,
                FF8Path = Sys.InstallPath,
                gameFiles = Directory.GetFiles(Sys.InstallPath, "*.*", SearchOption.AllDirectories),
                Mods = runtimeMods
            };

            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.AddingPathsToMonitor)}");
            runtimeProfiles.MonitorPaths.AddRange(Sys.Settings.ExtraFolders.Where(s => s.Length > 0).Select(s => Path.Combine(Sys.InstallPath, s)));
            return runtimeProfiles;
        }

        private static void CopyJ8WrapperDlls()
        {
            string src = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string dest = Path.GetDirectoryName(Sys.Settings.FF8Exe);

            File.Copy(Path.Combine(src, "AppProxy.runtimeconfig.json"), Path.Combine(dest, "AppProxy.runtimeconfig.json"), true);
            File.Copy(Path.Combine(src, "AppProxy.dll"), Path.Combine(dest, "AppProxy.dll"), true);
            File.Copy(Path.Combine(src, "SharpCompress.dll"), Path.Combine(dest, "SharpCompress.dll"), true);
            File.Copy(Path.Combine(src, "AppWrapper.dll"), Path.Combine(dest, "AppWrapper.dll"), true);
            File.Copy(Path.Combine(src, "AppLoader.dll"), Path.Combine(dest, "dinput.dll"), true);
            File.Copy(Path.Combine(src, "AppLoader.pdb"), Path.Combine(dest, "AppLoader.pdb"), true);
            File.Copy(Path.Combine(src, "nethost.dll"), Path.Combine(dest, "nethost.dll"), true);
            File.Copy(Path.Combine(src, "System.Runtime.Serialization.Formatters.dll"), Path.Combine(dest, "System.Runtime.Serialization.Formatters.dll"), true);
        }

        private static void DeleteJ8WrapperDlls()
        {
            string dest = Path.GetDirectoryName(Sys.Settings.FF8Exe);

            string workshopDir = Path.Combine(dest, "J8Workshop");
            if (Directory.Exists(workshopDir)) Directory.Delete(workshopDir, true);

            File.Delete(Path.Combine(dest, "AppProxy.runtimeconfig.json"));
            File.Delete(Path.Combine(dest, "AppProxy.dll"));
            File.Delete(Path.Combine(dest, "SharpCompress.dll"));
            File.Delete(Path.Combine(dest, "AppWrapper.dll"));
            File.Delete(Path.Combine(dest, "dinput.dll"));
            File.Delete(Path.Combine(dest, "AppLoader.pdb"));
            File.Delete(Path.Combine(dest, "nethost.dll"));
            File.Delete(Path.Combine(dest, "System.Runtime.Serialization.Formatters.dll"));
        }

        private static void StartTurboLogForVariableDump(RuntimeProfile runtimeProfiles)
        {
            string turboLogProcName = Path.Combine(Sys._J8Folder, "TurBoLog.exe");

            // remove from dictionary (and stop other turbolog exe) if exists
            if (Instance._alsoLaunchProcesses.ContainsKey(turboLogProcName))
            {
                if (!Instance._alsoLaunchProcesses[turboLogProcName].HasExited)
                {
                    Instance._alsoLaunchProcesses[turboLogProcName].Kill();
                }
                Instance._alsoLaunchProcesses.Remove(turboLogProcName);
            }

            runtimeProfiles.MonitorVars = Sys._context.VarAliases.Select(kv => new Tuple<string, string>(kv.Key, kv.Value)).ToList();

            ProcessStartInfo psi = new ProcessStartInfo(turboLogProcName)
            {
                WorkingDirectory = Path.GetDirectoryName(Sys.Settings.FF8Exe),
                UseShellExecute = true,
            };
            Process aproc = Process.Start(psi);

            Instance._alsoLaunchProcesses.Add(turboLogProcName, aproc);
            aproc.EnableRaisingEvents = true;
            aproc.Exited += (o, e) => Instance._alsoLaunchProcesses.Remove(turboLogProcName);
        }

        internal static async Task<Process> waitForProcess(string name)
        {
            Process ret = null;

            do
            {
                if (Process.GetProcessesByName(name) is [{ HasExited: false } process, ..])
                    ret = process;
                else
                    await Task.Delay(5000);
            } while (ret == null);

            return ret;
        }

        internal static async Task<bool> waitForProcessExit(string name)
        {
            Process ret;
            do
            {
                if (Process.GetProcessesByName(name) is [{ HasExited: true } process, ..])
                {
                    ret = process;
                    await Task.Delay(5000);
                }
                else
                    ret = null;
            } while (ret != null);

            return true;
        }

        /// <summary>
        /// Launches FF8.exe without loading any mods.
        /// </summary>
        internal static async Task<bool> LaunchFF8Exe()
        {
            try
            {
                if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
                {
                    string chocoTicket = Path.Combine(Sys.InstallPath, ".J8LaunchChoco");
                    if (File.Exists(chocoTicket)) File.Delete(chocoTicket);

                    // Start game via Steam
                    ProcessStartInfo startInfo = new ProcessStartInfo(GameConverter.GetSteamExePath())
                    {
                        WorkingDirectory = GameConverter.GetSteamPath(),
                        UseShellExecute = true,
                        Arguments = "-applaunch 39150"
                    };
                    Process.Start(startInfo);

                    // Wait for game process
                    Process game = await waitForProcess(Path.GetFileNameWithoutExtension(Sys.Settings.FF8Exe));
                    if (game != null)
                    {
                        ff8Proc = game;
                    }
                }
                else
                {
                    // Start game directly
                    ProcessStartInfo startInfo = new ProcessStartInfo(Sys.Settings.FF8Exe)
                    {
                        WorkingDirectory = Path.GetDirectoryName(Sys.Settings.FF8Exe),
                        UseShellExecute = true,
                    };
                    ff8Proc = Process.Start(startInfo);
                }

                ff8Proc.EnableRaisingEvents = true;
                ff8Proc.Exited += (o, e) =>
                {
                    try
                    {
                        if (!IsFF8Running() && Instance._controllerInterceptor != null)
                        {
                            // stop polling for input once all ff8 procs are closed (could be multiple instances open)
                            Instance._controllerInterceptor.PollingInput = false;
                        }

                        // cleanup
                        DeleteJ8WrapperDlls();
                        ReShadeUpdater.Cleanup();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                };

                return true;
            }
            catch (Exception ex)
            {
                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.AnExceptionOccurredTryingToStartFf8At)} {Sys.Settings.FF8Exe} ...", NLog.LogLevel.Error);
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Launches FF8.exe without loading any mods.
        /// </summary>
        internal static async Task<bool> LaunchChocoboExe()
        {
            string chocoboExe = string.Empty;

            try
            {
                if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
                {
                    chocoboExe = Path.Combine(Sys.InstallPath, "chocobo_en.exe");
                    string ticket = Path.Combine(Sys.InstallPath, ".J8LaunchChoco");

                    // Create signal file for the custom launcher
                    System.IO.File.WriteAllText(ticket, "chocobo_en.exe");

                    // Start game via Steam
                    ProcessStartInfo startInfo = new ProcessStartInfo(GameConverter.GetSteamExePath())
                    {
                        WorkingDirectory = GameConverter.GetSteamPath(),
                        UseShellExecute = true,
                        Arguments = "-applaunch 39150"
                    };
                    Process.Start(startInfo);

                    // Wait for game process
                    Process game = await waitForProcess(Path.GetFileNameWithoutExtension(chocoboExe));
                    if (game != null)
                    {
                        ff8Proc = game;
                    }

                    // Delete signal file
                    File.Delete(ticket);
                }
                else
                {
                    chocoboExe = Path.Combine(Sys.InstallPath, "chocobo.exe");

                    // Start game directly
                    ProcessStartInfo startInfo = new ProcessStartInfo(chocoboExe)
                    {
                        WorkingDirectory = Path.GetDirectoryName(chocoboExe),
                        UseShellExecute = true,
                    };
                    ff8Proc = Process.Start(startInfo);
                }

                ff8Proc.EnableRaisingEvents = true;
                ff8Proc.Exited += (o, e) =>
                {
                    try
                    {
                        if (!IsChocoboRunning() && Instance._controllerInterceptor != null)
                        {
                            // stop polling for input once all ff8 procs are closed (could be multiple instances open)
                            Instance._controllerInterceptor.PollingInput = false;
                        }

                        // cleanup
                        DeleteJ8WrapperDlls();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                };

                return true;
            }
            catch (Exception ex)
            {
                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.AnExceptionOccurredTryingToStartFf8At)} {chocoboExe} ...", NLog.LogLevel.Error);
                Logger.Error(ex);
                return false;
            }
        }

        internal static bool SanityCheckSettings()
        {
            List<string> changes = new List<string>();
            foreach (var constraint in GetConstraints())
            {
                if (!constraint.Verify(out string msg))
                {
                    Logger.Warn(msg);
                    MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ThereWasAnErrorVerifyingModConstraintSeeTheFollowingDetails), msg, ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (msg != null)
                {
                    changes.Add(msg);
                }
            }

            if (changes.Any())
            {
                Logger.Warn($"{ResourceHelper.Get(StringKey.TheFollowingSettingsHaveBeenChangedToMakeTheseModsCompatible)}\n{String.Join("\n", changes)}");
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.TheFollowingSettingsHaveBeenChangedToMakeTheseModsCompatible), String.Join("\n", changes), ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return true;
        }

        internal static bool SanityCheckCompatibility()
        {
            if (Sys.Settings.HasOption(GeneralOptions.BypassCompatibility))
            {
                return true; // user has 'ignore compatibility restrictions' set in general settings so just return true
            }

            List<InstalledItem> activeInstalledMods = Sys.ActiveProfile.ActiveItems.Select(pi => Sys.Library.GetItem(pi.ModID)).ToList();
            var vars = new List<AppWrapper.Variable>();

            foreach (InstalledItem item in activeInstalledMods)
            {
                ModInfo info = item.GetModInfo();
                var mod = Sys.Library.GetItem(item.ModID);
                string location = System.IO.Path.Combine(Sys.Settings.LibraryLocation, mod.LatestInstalled.InstalledLocation);

                if (mod.LatestInstalled.InstalledLocation.EndsWith(".iroj"))
                {
                    using (var arc = new AppWrapper.IrosArc(location))
                    {
                        if (arc.HasFile("mod.xml"))
                        {
                            var doc = new System.Xml.XmlDocument();
                            doc.Load(arc.GetData("mod.xml"));
                            foreach (XmlNode xmlNode in doc.SelectNodes("/ModInfo/Variable"))
                            {
                                var currentVar = new AppWrapper.Variable() { Name = xmlNode.Attributes.GetNamedItem("Name").Value, Value = item.ModID.ToString() };
                                var test = vars.Find(var => var.Name == currentVar.Name);
                                if (test == null){
                                    vars.Add(currentVar);
                                }
                                else
                                {
                                    //@todo create StringKey resource for unknown incompatible mod
                                    MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleAnotherModVariableUsed), item.CachedDetails.Name, Sys.Library.GetItem(new Guid(test.Value)).CachedDetails.Name, currentVar.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                                    return false;
                                }
                            }
                        }
                    }
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
                            var currentVar = new AppWrapper.Variable() { Name = xmlNode.Attributes.GetNamedItem("Name").Value, Value = item.ModID.ToString() };
                            var test = vars.Find(var => var.Name == currentVar.Name);
                            if (test == null)
                            {
                                vars.Add(currentVar);
                            }
                            else
                            {
                                MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleAnotherModVariableUsed), item.CachedDetails.Name, Sys.Library.GetItem(new Guid(test.Value)).CachedDetails.Name, currentVar.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                                return false;
                            }
                        }
                    }

                }

                if (info == null)
                {
                    continue;
                }

                foreach (var req in info.Compatibility.Requires)
                {
                    var rInst = activeInstalledMods.Find(i => i.ModID.Equals(req.ModID));
                    if (rInst == null)
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModRequiresYouToActivateAsWell), item.CachedDetails.Name, req.Description), ResourceHelper.Get(StringKey.MissingRequiredActiveMods));
                        return false;
                    }
                    else if (req.Versions.Any() && !req.Versions.Contains(rInst.LatestInstalled.VersionDetails.Version))
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModRequiresYouToActivateButYouDoNotHaveCompatibleVersionInstalled), item.CachedDetails.Name, rInst.CachedDetails.Name), ResourceHelper.Get(StringKey.UnsupportedModVersion));
                        return false;
                    }
                }

                foreach (var forbid in info.Compatibility.Forbids)
                {
                    var rInst = activeInstalledMods.Find(i => i.ModID.Equals(forbid.ModID));
                    if (rInst == null)
                    {
                        continue; //good!
                    }

                    if (forbid.Versions.Any() && forbid.Versions.Contains(rInst.LatestInstalled.VersionDetails.Version))
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleWithTheVersionYouHaveInstalled), item.CachedDetails.Name, rInst.CachedDetails.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                        return false;
                    }
                    else
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleWithYouWillNeedToDisableIt), item.CachedDetails.Name, rInst.CachedDetails.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool VerifyOrdering()
        {
            var details = Sys.ActiveProfile
                             .ActiveItems
                             .Select(i => Sys.Library.GetItem(i.ModID))
                             .Select(ii => new { Mod = ii, Info = ii.GetModInfo() })
                             .ToDictionary(a => a.Mod.ModID, a => a);

            List<string> problems = new List<string>();

            foreach (int i in Enumerable.Range(0, Sys.ActiveProfile.ActiveItems.Count))
            {
                Iros.Workshop.ProfileItem mod = Sys.ActiveProfile.ActiveItems[i];
                var info = details[mod.ModID].Info;

                if (info == null)
                {
                    continue;
                }

                foreach (Guid after in info.OrderAfter)
                {
                    if (Sys.ActiveProfile.ActiveItems.Skip(i).Any(pi => pi.ModID.Equals(after)))
                    {
                        problems.Add(string.Format(ResourceHelper.Get(StringKey.ModIsMeantToComeBelowModInTheLoadOrder), details[mod.ModID].Mod.CachedDetails.Name, details[after].Mod.CachedDetails.Name));
                    }
                }

                foreach (Guid before in info.OrderBefore)
                {
                    if (Sys.ActiveProfile.ActiveItems.Take(i).Any(pi => pi.ModID.Equals(before)))
                    {
                        problems.Add(string.Format(ResourceHelper.Get(StringKey.ModIsMeantToComeAboveModInTheLoadOrder), details[mod.ModID].Mod.CachedDetails.Name, details[before].Mod.CachedDetails.Name));
                    }
                }
            }

            if (problems.Any())
            {
                if (MessageDialogWindow.Show(ResourceHelper.Get(StringKey.TheFollowingModsWillNotWorkProperlyInTheCurrentOrder), String.Join("\n", problems), ResourceHelper.Get(StringKey.LoadOrderIncompatible), MessageBoxButton.YesNo).Result != MessageBoxResult.Yes)
                    return false;
            }

            return true;
        }

        internal static List<Constraint> GetConstraints()
        {
            List<Constraint> constraints = new List<Constraint>();
            foreach (Iros.Workshop.ProfileItem pItem in Sys.ActiveProfile.ActiveItems)
            {
                InstalledItem inst = Sys.Library.GetItem(pItem.ModID);
                ModInfo info = inst.GetModInfo();

                if (info == null)
                {
                    continue;
                }

                foreach (var cSetting in info.Compatibility.Settings)
                {
                    if (!String.IsNullOrWhiteSpace(cSetting.MyID))
                    {
                        var setting = pItem.Settings.Find(s => s.ID.Equals(cSetting.MyID, StringComparison.InvariantCultureIgnoreCase));
                        if ((setting == null) || (setting.Value != cSetting.MyValue)) continue;
                    }

                    Iros.Workshop.ProfileItem oItem = Sys.ActiveProfile.ActiveItems.Find(i => i.ModID.Equals(cSetting.ModID));
                    if (oItem == null) continue;

                    InstalledItem oInst = Sys.Library.GetItem(cSetting.ModID);
                    Constraint ct = constraints.Find(c => c.ModID.Equals(cSetting.ModID) && c.Setting.Equals(cSetting.TheirID, StringComparison.InvariantCultureIgnoreCase));
                    if (ct == null)
                    {
                        ct = new Constraint() { ModID = cSetting.ModID, Setting = cSetting.TheirID };
                        constraints.Add(ct);
                    }

                    if (!ct.ParticipatingMods.ContainsKey(inst.CachedDetails.ID))
                    {
                        ct.ParticipatingMods.Add(inst.CachedDetails.ID, inst.CachedDetails.Name);
                    }

                    if (cSetting.Require.HasValue)
                    {
                        ct.Require.Add(cSetting.Require.Value);
                    }

                    foreach (var f in cSetting.Forbid)
                    {
                        ct.Forbid.Add(f);
                    }
                }

                foreach (var setting in info.Options)
                {
                    Constraint ct = constraints.Find(c => c.ModID.Equals(pItem.ModID) && c.Setting.Equals(setting.ID, StringComparison.InvariantCultureIgnoreCase));
                    if (ct == null)
                    {
                        ct = new Constraint() { ModID = pItem.ModID, Setting = setting.ID };
                        constraints.Add(ct);
                    }
                    ct.Option = setting;
                }

            }

            return constraints;
        }

        /// <summary>
        /// Scans all drives looking for the drive labeled "FF8DISC1", "FF8DISC2", or "FF8DISC3" and returns the corresponding drive letter.
        /// If not found returns empty string. Returns the drive letter for <paramref name="labelToFind"/> if not null
        /// </summary>
        public static string GetDriveLetter(string labelToFind = null)
        {
            List<string> labels = null;
            if (string.IsNullOrWhiteSpace(labelToFind))
            {
                labels = new List<string>() { "FF8_DISC1", "FF8_DISC2", "FF8_DISC3", "FF8_DISC4" };
            }
            else
            {
                labels = new List<string>() { labelToFind };
            }

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && labels.Any(s => s.Equals(drive.VolumeLabel, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return drive.Name;
                }
            }

            return "";
        }

        /// <summary>
        /// Scans all drives looking for the drive labeled "FF8DISC1", "FF8DISC2", or "FF8DISC3" and returns the corresponding drive letters that have the matching label.
        /// If not found returns empty string. Returns the drive letter for <paramref name="labelToFind"/> if not null
        /// </summary>
        public static List<string> GetDriveLetters(string labelToFind = null)
        {
            List<string> labels = null;
            if (string.IsNullOrWhiteSpace(labelToFind))
            {
                labels = new List<string>() { "FF8DISC1", "FF8DISC2", "FF8DISC3" };
            }
            else
            {
                labels = new List<string>() { labelToFind };
            }

            List<string> drivesFound = new List<string>();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && labels.Any(s => s.Equals(drive.VolumeLabel, StringComparison.InvariantCultureIgnoreCase)))
                {
                    drivesFound.Add(drive.Name);
                }
            }

            return drivesFound;
        }

        /// <summary>
        /// Updates Registry with new values from <see cref="Sys.Settings.GameLaunchSettings"/>
        /// </summary>
        public void SetRegistryValues()
        {
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ApplyingChangedValuesToRegistry));

            RegistryHelper.BeginTransaction();

            string ff8KeyPath = $"{RegistryHelper.GetKeyPath(FF8RegKey.SquareSoftKeyPath)}\\Final Fantasy VIII\\1.00";
            string virtualStorePath = $"{RegistryHelper.GetKeyPath(FF8RegKey.VirtualStoreKeyPath)}\\Final Fantasy VIII\\1.00";

            string installPath = Path.GetDirectoryName(Sys.Settings.FF8Exe);
            string pathToData = Path.Combine(installPath, @"data\");

            if (installPath != null && !installPath.EndsWith(@"\"))
            {
                installPath += @"\";
            }


            // Add registry key values for paths and drive letter
            SetValueIfChanged(ff8KeyPath, "AppPath", installPath);
            SetValueIfChanged(virtualStorePath, "AppPath", installPath);

            // setting the drive letter may not happen if auto update disc path is not set
            if (Sys.Settings.GameLaunchSettings.AutoUpdateDiscPath && !string.IsNullOrWhiteSpace(DriveLetter))
            {
                SetValueIfChanged(ff8KeyPath, "DataDrive", DriveLetter);
                SetValueIfChanged(virtualStorePath, "DataDrive", DriveLetter);
            }

            SetValueIfChanged(ff8KeyPath, "Graphics", 0x00100021, RegistryValueKind.DWord);
            SetValueIfChanged(virtualStorePath, "Graphics", 0x00100021, RegistryValueKind.DWord);

            Guid graphicsGuidBytes = Guid.Empty;
            SetValueIfChanged(ff8KeyPath, "GraphicsGUID", graphicsGuidBytes, RegistryValueKind.Binary);
            SetValueIfChanged(virtualStorePath, "GraphicsGUID", graphicsGuidBytes, RegistryValueKind.Binary);

            SetValueIfChanged(ff8KeyPath, "InstallOptions", 0x000000ff, RegistryValueKind.DWord);
            SetValueIfChanged(virtualStorePath, "InstallOptions", 0x000000ff, RegistryValueKind.DWord);

            // Add registry key values for default MIDI Device if missing from registry
            if (RegistryHelper.GetValue(ff8KeyPath, "MIDIGUID", null) == null || RegistryHelper.GetValue(virtualStorePath, "MIDIGUID", null) == null)
            {
                Guid midiGuidBytes = Sys.Settings.GameLaunchSettings.SelectedMidiDevice;
                SetValueIfChanged(ff8KeyPath, "MIDIGUID", midiGuidBytes, RegistryValueKind.Binary);
                SetValueIfChanged(virtualStorePath, "MIDIGUID", midiGuidBytes, RegistryValueKind.Binary);
            }

            SetValueIfChanged(ff8KeyPath, "MidiOptions", 0x00000001, RegistryValueKind.DWord);
            SetValueIfChanged(virtualStorePath, "MidiOptions", 0x00000001, RegistryValueKind.DWord);

            Guid soundGuidBytes = Sys.Settings.GameLaunchSettings.SelectedSoundDevice;
            SetValueIfChanged(ff8KeyPath, "SoundGUID", soundGuidBytes, RegistryValueKind.Binary);
            SetValueIfChanged(virtualStorePath, "SoundGUID", soundGuidBytes, RegistryValueKind.Binary);

            SetValueIfChanged(ff8KeyPath, "SoundOptions", 0x00000000, RegistryValueKind.DWord);
            SetValueIfChanged(virtualStorePath, "SoundOptions", 0x00000000, RegistryValueKind.DWord);

            // Disable compatibility flags
            string appCompatPath = $"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags";
            string appCompatPath64 = $"HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags";

            SetValueIfChanged(appCompatPath, "{e1af58c2-e896-4640-bb34-d9fa0b8260a3}", 0x00000077, RegistryValueKind.DWord);
            SetValueIfChanged(appCompatPath64, "{e1af58c2-e896-4640-bb34-d9fa0b8260a3}", 0x00000077, RegistryValueKind.DWord);

            RegistryHelper.CommitTransaction();
        }

        /// <summary>
        /// Update Registry with new value if it has changed from the current value in the Registry.
        /// Logs the changed value.
        /// </summary>
        private void SetValueIfChanged(string regKeyPath, string regValueName, object newValue, RegistryValueKind valueKind = RegistryValueKind.String)
        {
            if (RegistryHelper.SetValueIfChanged(regKeyPath, regValueName, newValue, valueKind))
            {
                string valueFormatted = newValue?.ToString(); // used to display the object value correctly in the log i.e. for a byte[] convert it to readable string

                if (newValue is byte[])
                {
                    valueFormatted = newValue == null ? "" : BitConverter.ToString(newValue as byte[]);
                }

                Instance.RaiseProgressChanged($"\t {regKeyPath}::{regValueName} = {valueFormatted}");
            }
        }

        public static bool CopyKeyboardInputCfg()
        {
            Directory.CreateDirectory(Sys.PathToControlsFolder);
            string pathToCfg = Path.Combine(Sys.PathToControlsFolder, Sys.Settings.GameLaunchSettings.InGameConfigOption);

            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.UsingControlConfigurationFile)} {Sys.Settings.GameLaunchSettings.InGameConfigOption} ...");
            if (!File.Exists(pathToCfg))
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.InputCfgFileNotFoundAt)} {pathToCfg}", NLog.LogLevel.Warn);
                return false;
            }

            try
            {
                string targetPath = Path.Combine(
                    Sys.Settings.FF8InstalledVersion == FF8Version.Steam ? GameConverter.GetSteamFF8UserPath() : Path.GetDirectoryName(Sys.Settings.FF8Exe),
                    "ff8input.cfg"
                );

                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.Copying)} {pathToCfg} -> {targetPath} ...");
                File.Copy(pathToCfg, targetPath, true);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCopyCfgFile)}: {e.Message}", NLog.LogLevel.Error);
                return false;
            }

            return true;
        }

        public static bool IsFF8Running()
        {
            bool ret = false;

            string fileName = Path.GetFileNameWithoutExtension(Sys.Settings.FF8Exe);
            ret = Process.GetProcessesByName(fileName).Length > 0;

            if (!ret && Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
            {
                ret = Process.GetProcessesByName("FF8_Launcher").Length > 0;
            }

            return ret;
        }

        public static bool IsChocoboRunning()
        {
            bool ret = false;

            string fileName = Sys.Settings.FF8InstalledVersion == FF8Version.Steam ? "chocobo_en" : "chocobo";
            ret = Process.GetProcessesByName(fileName).Length > 0;

            if (!ret && Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
            {
                ret = Process.GetProcessesByName("FF8_Launcher").Length > 0;
            }

            return ret;
        }

        private static bool ForceKillFF8()
        {
            string fileName = Path.GetFileNameWithoutExtension(Sys.Settings.FF8Exe);

            try
            {
                foreach (Process item in Process.GetProcessesByName(fileName))
                {
                    item.Kill();
                }

                if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
                {
                    foreach (Process item in Process.GetProcessesByName("FF8_Launcher"))
                    {
                        item.Kill();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }


        /// <summary>
        /// Kills any currently running process found in <see cref="_sideLoadProcesses"/>
        /// </summary>
        private void StopAllSideProcessesForMods()
        {
            foreach (var valuePair in _sideLoadProcesses.ToList())
            {
                AppWrapper.ProgramInfo info = valuePair.Key;
                Process sideProc = valuePair.Value;
                string procName = sideProc.ProcessName;

                if (!sideProc.HasExited)
                {
                    sideProc.Kill();
                }

                // Kill all instances with same process name if necessary
                if (info.CloseAllInstances)
                {
                    foreach (Process otherProc in Process.GetProcessesByName(procName))
                    {
                        if (!otherProc.HasExited)
                            otherProc.Kill();
                    }
                }
            }
        }

        private void CollectCrashReport()
        {
            // Cleanup any old report older than 1 day

            DirectoryInfo dir = new DirectoryInfo(Sys.PathToCrashReports);
            FileInfo[] files = dir.GetFiles().Where(file => file.CreationTime < DateTime.Now.AddDays(-1)).ToArray();
            foreach (FileInfo file in files) file.Delete();

            // Create the new report

            var zipOutPath = Path.Combine(Sys.PathToCrashReports, $"J8CrashReport-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.zip");

            // Flush logs before saving in order to obtain any possible leftover in memory
            Logger.Factory.Flush(0);

            using (var archive = ZipArchive.CreateArchive())
            {
                // === FF8 files ===
                if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam)
                {
                    var di = new DirectoryInfo(GameConverter.GetSteamFF8UserPath());
                    foreach (FileInfo fi in di.GetFiles("*.ff8", SearchOption.AllDirectories))
                    {
                        archive.AddEntry(Path.Combine("save", Path.GetFileName(fi.FullName)), fi.FullName);
                    }
                }
                else
                {
                    var savePath = Path.Combine(Sys.InstallPath, "save");
                    if (Directory.Exists(savePath))
                    {
                        var saveFiles = Directory.GetFiles(savePath);
                        foreach (var file in saveFiles)
                        {
                            archive.AddEntry(Path.Combine("save", Path.GetFileName(file)), file);
                        }
                    }
                }

                // === FFNx files ===
                if (File.Exists(Sys.PathToFFNxLog)) archive.AddEntry("FFNx.log", Sys.PathToFFNxLog);
                archive.AddEntry("FFNx.toml", Sys.PathToFFNxToml);

                // === App files ===
                archive.AddEntry("AppLoader.log", Path.Combine(Sys.InstallPath, "AppLoader.log"));
                archive.AddEntry("applog.txt", File.Open(Sys.PathToApplog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                archive.AddEntry("settings.xml", Sys.PathToSettings);

                string rt = Path.Combine(Sys.PathToTempFolder, "registry_transaction.bat");
                if (File.Exists(rt)) archive.AddEntry(Path.GetFileName(rt), rt);

                // Convert profile.xml to profile.txt
                Profile currentProfile = Util.Deserialize<Profile>(Sys.PathToCurrentProfileFile);
                IEnumerable<string> profileDetails = currentProfile.GetDetails();
                File.WriteAllLines(Path.Combine(Sys.PathToTempFolder, "profile.txt"), profileDetails);
                archive.AddEntry("profile.txt", Path.Combine(Sys.PathToTempFolder, "profile.txt"));

                // =================================================================================================

                archive.SaveTo(zipOutPath, CompressionType.Deflate);
            }
        }

        internal void LaunchProgramsForMod(RuntimeMod mod)
        {
            if (!mod.LoadPrograms.Any())
            {
                return;
            }

            mod.Startup();

            foreach (var program in mod.GetLoadPrograms())
            {
                if (!_sideLoadProcesses.ContainsKey(program))
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.StartingProgram)}: {program.PathToProgram}");
                    ProcessStartInfo psi = new ProcessStartInfo()
                    {
                        WorkingDirectory = Path.GetDirectoryName(program.PathToProgram),
                        FileName = program.PathToProgram,
                        Arguments = program.ProgramArgs,
                        UseShellExecute = true,
                    };
                    Process aproc = Process.Start(psi);

                    aproc.EnableRaisingEvents = true;
                    aproc.Exited += (_o, _e) => _sideLoadProcesses.Remove(program);

                    _sideLoadProcesses.Add(program, aproc);
                    Instance.RaiseProgressChanged($"\t\t{ResourceHelper.Get(StringKey.Started)}");

                    IntPtr mainWindowHandle = IntPtr.Zero;

                    if (program.WaitForWindowToShow)
                    {
                        Instance.RaiseProgressChanged($"\t\t{ResourceHelper.Get(StringKey.WaitingForProgramToInitializeAndShow)}");

                        DateTime startTime = DateTime.Now;

                        while (aproc.MainWindowHandle == IntPtr.Zero && mainWindowHandle == IntPtr.Zero)
                        {
                            if (program.WaitTimeOutInSeconds != 0 && DateTime.Now.Subtract(startTime).TotalSeconds > program.WaitTimeOutInSeconds)
                            {
                                Instance.RaiseProgressChanged($"\t\t{string.Format(ResourceHelper.Get(StringKey.TimeoutReachedWaitingForProgramToShow), program.WaitTimeOutInSeconds)}", NLog.LogLevel.Warn);
                                break;
                            }

                            aproc.Refresh();

                            if (!string.IsNullOrWhiteSpace(program.WindowTitle))
                            {
                                // attempt to find main window handle based on the Window Title of the process
                                // ... necessary because some programs (Speed Hack) will open multiple processes and the Main Window Handle needs to be found since it is different from the original process started
                                mainWindowHandle = FindWindow(null, program.WindowTitle);
                            }

                        };
                    }

                    // force the process to become minimized
                    if (aproc.MainWindowHandle != IntPtr.Zero || mainWindowHandle != IntPtr.Zero)
                    {
                        Logger.Info($"\t\t{ResourceHelper.Get(StringKey.ForceMinimizingProgram)}");
                        IntPtr handleToMinimize = aproc.MainWindowHandle != IntPtr.Zero ? aproc.MainWindowHandle : mainWindowHandle;

                        System.Threading.Thread.Sleep(1500); // add a small delay to ensure the program is showing in taskbar before force minimizing;
                        ShowWindow(handleToMinimize.ToInt32(), SW_FORCEMINIMIZE);
                    }
                }
                else
                {
                    if (!_sideLoadProcesses[program].HasExited)
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ProgramAlreadyRunning)}: {program.PathToProgram}", NLog.LogLevel.Warn);
                    }
                }
            }
        }


        /// <summary>
        /// Starts the processes with the specified arguments that are set in <see cref="Sys.Settings.ProgramsToLaunchPrior"/>.
        /// </summary>
        internal void LaunchAdditionalProgramsToRunPrior()
        {
            // launch other processes set in settings
            foreach (ProgramLaunchInfo al in Sys.Settings.ProgramsToLaunchPrior.Where(s => !String.IsNullOrWhiteSpace(s.PathToProgram)))
            {
                try
                {
                    if (!_alsoLaunchProcesses.ContainsKey(al.PathToProgram))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo()
                        {
                            WorkingDirectory = Path.GetDirectoryName(al.PathToProgram),
                            FileName = al.PathToProgram,
                            Arguments = al.ProgramArgs,
                            UseShellExecute = true,
                        };
                        Process aproc = Process.Start(psi);

                        _alsoLaunchProcesses.Add(al.PathToProgram, aproc);
                        aproc.EnableRaisingEvents = true;
                        aproc.Exited += (_o, _e) => _alsoLaunchProcesses.Remove(al.PathToProgram);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                    Instance.RaiseProgressChanged($"\t{string.Format(ResourceHelper.Get(StringKey.FailedToStartAdditionalProgram), al.PathToProgram)}", NLog.LogLevel.Warn);
                }
            }
        }

        internal void RaiseProgressChanged(string messageToLog, NLog.LogLevel logLevel = null)
        {
            if (logLevel == null)
            {
                logLevel = NLog.LogLevel.Info;
            }

            Logger.Log(logLevel, messageToLog);
            ProgressChanged?.Invoke(messageToLog);
        }

        internal void RaiseLaunchCompleted(bool didLaunch)
        {
            LaunchCompleted?.Invoke(didLaunch);
        }



    }
}
