using AppCore;
using Iros.Workshop;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using AppUI.Classes;
using AppUI.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.Media.Audio.DirectMusic;

namespace AppUI.ViewModels
{
    internal enum AudioChannel
    {
        Left,
        Center,
        Right
    }

    internal enum VolumeSlider
    {
        Music,
        Sfx,
        Voice,
        Ambient,
        Movie
    }

    class GameLaunchSettingsViewModel : ViewModelBase
    {
        #region Data Members
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool _autoUpdatePathChecked;
        private bool _isReverseSpeakersChecked;
        private bool _isLogVolumeChecked;
        private string _selectedSoundDevice;
        private string _selectedMidiDevice;
        private int _musicVolumeValue;

        private WaveOut _audioTest;

        private ObservableCollection<ProgramToRunViewModel> _programList;
        private string _newProgramPathText;
        private string _newProgramArgsText;
        private bool _isProgramPopupOpen;
        private int _sfxVolumeValue;
        private bool _isShowLauncherChecked;
        private VolumeSlider _lastVolumeSliderChanged;
        private bool _isSoundDevicesLoaded;

        private int _voiceVolumeValue;
        private int _ambientVolumeValue;
        private int _movieVolumeValue;

        private IDirectMusic _directMusic;

        #endregion


        #region Properties

        public bool IsAudioPlaying
        {
            get
            {
                return _audioTest != null && _audioTest.PlaybackState == PlaybackState.Playing;
            }
        }

        public bool IsAudioNotPlaying
        {
            get
            {
                return _audioTest == null;
            }
        }

        public bool IsSoundDevicesLoaded
        {
            get
            {
                return _isSoundDevicesLoaded;
            }
            set
            {
                _isSoundDevicesLoaded = value;
                NotifyPropertyChanged();
            }
        }

        public bool AutoUpdatePathChecked
        {
            get
            {
                return _autoUpdatePathChecked;
            }
            set
            {
                _autoUpdatePathChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsShowLauncherChecked
        {
            get
            {
                return _isShowLauncherChecked;
            }
            set
            {
                _isShowLauncherChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsReverseSpeakersChecked
        {
            get
            {
                return _isReverseSpeakersChecked;
            }
            set
            {
                _isReverseSpeakersChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsLogVolumeChecked
        {
            get
            {
                return _isLogVolumeChecked;
            }
            set
            {
                _isLogVolumeChecked = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedSoundDevice
        {
            get
            {
                return _selectedSoundDevice;
            }
            set
            {
                _selectedSoundDevice = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> SoundDevices
        {
            get
            {
                return SoundDeviceGuids?.Keys.ToList();
            }
        }

        public Dictionary<string, Guid> SoundDeviceGuids { get; set; }
        
        public string SelectedMidiDevice
        {
            get
            {
                return _selectedMidiDevice;
            }
            set
            {
                _selectedMidiDevice = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> MidiDevices
        {
            get
            {
                return MidiDeviceIDs?.Keys.ToList();
            }
        }

        public Dictionary<string, Guid> MidiDeviceIDs { get; set; }

        public int MusicVolumeValue
        {
            get
            {
                return _musicVolumeValue;
            }
            set
            {
                _musicVolumeValue = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(MusicVolumeDisplayText));
                LastVolumeSliderChanged = VolumeSlider.Music;

                if (IsAudioPlaying && LastVolumeSliderChanged == VolumeSlider.Music)
                {
                    _audioTest.Volume = (float)_musicVolumeValue / (float)100.0;
                }
            }
        }
        public int VoiceVolumeValue
        {
            get
            {
                return _voiceVolumeValue;
            }
            set
            {
                _voiceVolumeValue = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(VoiceVolumeDisplayText));
                LastVolumeSliderChanged = VolumeSlider.Voice;

                if (IsAudioPlaying && LastVolumeSliderChanged == VolumeSlider.Voice)
                {
                    _audioTest.Volume = (float)_voiceVolumeValue / (float)100.0;
                }
            }
        }

        public int AmbientVolumeValue
        {
            get
            {
                return _ambientVolumeValue;
            }
            set
            {
                _ambientVolumeValue = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(AmbientVolumeDisplayText));
                LastVolumeSliderChanged = VolumeSlider.Ambient;

                if (IsAudioPlaying && LastVolumeSliderChanged == VolumeSlider.Ambient)
                {
                    _audioTest.Volume = (float)_ambientVolumeValue / (float)100.0;
                }
            }
        }

        public int MovieVolumeValue
        {
            get
            {
                return _movieVolumeValue;
            }
            set
            {
                _movieVolumeValue = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(MovieVolumeDisplayText));
                LastVolumeSliderChanged = VolumeSlider.Movie;

                if (IsAudioPlaying && LastVolumeSliderChanged == VolumeSlider.Movie)
                {
                    _audioTest.Volume = (float)_movieVolumeValue / (float)100.0;
                }
            }
        }

        public int SfxVolumeValue
        {
            get
            {
                return _sfxVolumeValue;
            }
            set
            {
                _sfxVolumeValue = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(SfxVolumeDisplayText));
                LastVolumeSliderChanged = VolumeSlider.Sfx;

                if (IsAudioPlaying && LastVolumeSliderChanged == VolumeSlider.Sfx)
                {
                    _audioTest.Volume = (float)_sfxVolumeValue / (float)100.0;
                }
            }
        }

        public string SfxVolumeDisplayText
        {
            get
            {
                return $"{ResourceHelper.Get(StringKey.Volume)}: {SfxVolumeValue}";
            }
        }

        public string MusicVolumeDisplayText
        {
            get
            {
                return $"{ResourceHelper.Get(StringKey.Volume)}: {MusicVolumeValue}";
            }
        }

        public string VoiceVolumeDisplayText
        {
            get
            {
                return $"{ResourceHelper.Get(StringKey.Volume)}: {VoiceVolumeValue}";
            }
        }
        public string AmbientVolumeDisplayText
        {
            get
            {
                return $"{ResourceHelper.Get(StringKey.Volume)}: {AmbientVolumeValue}";
            }
        }
        public string MovieVolumeDisplayText
        {
            get
            {
                return $"{ResourceHelper.Get(StringKey.Volume)}: {MovieVolumeValue}";
            }
        }

        public bool IsProgramPopupOpen
        {
            get
            {
                return _isProgramPopupOpen;
            }
            set
            {
                _isProgramPopupOpen = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<ProgramToRunViewModel> ProgramList
        {
            get
            {
                if (_programList == null)
                    _programList = new ObservableCollection<ProgramToRunViewModel>();

                return _programList;
            }
            set
            {
                _programList = value;
                NotifyPropertyChanged();
            }
        }

        public string NewProgramPathText
        {
            get
            {
                return _newProgramPathText;
            }
            set
            {
                _newProgramPathText = value;
                NotifyPropertyChanged();
            }
        }

        public string NewProgramArgsText
        {
            get
            {
                return _newProgramArgsText;
            }
            set
            {
                _newProgramArgsText = value;
                NotifyPropertyChanged();
            }
        }

        private bool HasLoaded { get; set; }

        public VolumeSlider LastVolumeSliderChanged
        {
            get
            {
                return _lastVolumeSliderChanged;
            }
            set
            {
                _lastVolumeSliderChanged = value;
                NotifyPropertyChanged();
            }
        }

        #endregion


        public GameLaunchSettingsViewModel()
        {
            NewProgramPathText = "";
            NewProgramArgsText = "";
            IsProgramPopupOpen = false;
            IsSoundDevicesLoaded = false;
            _audioTest = null;
            HasLoaded = false;
            LastVolumeSliderChanged = VolumeSlider.Music;

            // Initialize direct music
            object dmusic = null;
            PInvoke.CoInitialize();
            Guid CLSID_DirectMusic = new Guid("636b9f10-0c7d-11d1-95b2-0020afdc7421"); // CLSID for DirectMusic
            Guid IID_IDirectMusic = typeof(IDirectMusic).GUID;
            HRESULT hr = PInvoke.CoCreateInstance(CLSID_DirectMusic, null, CLSCTX.CLSCTX_INPROC_SERVER, IID_IDirectMusic, out dmusic);
            _directMusic = (IDirectMusic)dmusic;

            // initialize sound devices on background task because it can take up to 1-3 seconds to loop over audio devices and get their names
            SelectedSoundDevice = ResourceHelper.Get(StringKey.LoadingDevices);
            InitSoundDevicesAsync();

            if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam) Sys.FFNxConfig.Reload();

            InitMidiDevices();
            LoadSettings(Sys.Settings.GameLaunchSettings);
        }

        internal Task InitSoundDevicesAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                InitSoundDevices();
                SetSelectedSoundDeviceFromSettings(Sys.Settings.GameLaunchSettings);
                IsSoundDevicesLoaded = true;
            });
        }

        private void LoadSettings(LaunchSettings launchSettings)
        {
            HasLoaded = false;

            if (Sys.Settings.GameLaunchSettings == null)
            {
                Logger.Warn("No game launcher settings found, initializing to defaults.");
                Sys.Settings.GameLaunchSettings = LaunchSettings.DefaultSettings();
                launchSettings = Sys.Settings.GameLaunchSettings;
            }

            ProgramList = new ObservableCollection<ProgramToRunViewModel>(Sys.Settings.ProgramsToLaunchPrior.Select(s => new ProgramToRunViewModel(s.PathToProgram, s.ProgramArgs)));

            AutoUpdatePathChecked = launchSettings.AutoUpdateDiscPath;
            IsShowLauncherChecked = launchSettings.ShowLauncherWindow;

            SetSelectedSoundDeviceFromSettings(launchSettings);
            SetSelectedMidiDeviceFromSettings();

            GetVolumesFromRegistry();

            IsReverseSpeakersChecked = launchSettings.ReverseSpeakers;
            IsLogVolumeChecked = launchSettings.LogarithmicVolumeControl;

            HasLoaded = true;
        }

        private void SetMidiDeviceInRegistry()
        {
            string ff8KeyPath = $"{RegistryHelper.GetKeyPath(FF8RegKey.SquareSoftKeyPath)}\\Final Fantasy VIII";
            string midiKeyPath = $"{ff8KeyPath}\\1.00";

            string virtualStorePath = $"{RegistryHelper.GetKeyPath(FF8RegKey.VirtualStoreKeyPath)}\\Final Fantasy VIII";
            string midiVirtualKeyPath = $"{virtualStorePath}\\1.00";

            RegistryHelper.SetValueIfChanged(midiKeyPath, "MIDIGUID", MidiDeviceIDs[SelectedMidiDevice], RegistryValueKind.Binary);
            RegistryHelper.SetValueIfChanged(midiVirtualKeyPath, "MIDIGUID", MidiDeviceIDs[SelectedMidiDevice], RegistryValueKind.Binary);
        }

        private int GetMidiDeviceInRegistry()
        {
            int ret = 0;
            string ff8KeyPath = $"{RegistryHelper.GetKeyPath(FF8RegKey.SquareSoftKeyPath)}\\Final Fantasy VIII";
            string midiKeyPath = $"{ff8KeyPath}\\1.00";
            Guid deviceID = new Guid(RegistryHelper.GetValue(midiKeyPath, "MIDIGUID", Guid.Empty.ToByteArray()) as byte[]);

            DMUS_PORTCAPS caps = new DMUS_PORTCAPS();
            unsafe
            {
                caps.dwSize = (uint)sizeof(DMUS_PORTCAPS);
            }

            if (deviceID != Guid.Empty)
            {
                while (true)
                {
                    try
                    {
                        _directMusic.EnumPort((uint)ret, ref caps);
                        if (caps.guidPort == deviceID) break;
                        ret++;
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            return ret;
        }

        private void GetVolumesFromRegistry()
        {
            SfxVolumeValue = int.Parse(Sys.FFNxConfig.Get("external_sfx_volume"));
            if (SfxVolumeValue < 0) SfxVolumeValue = 100;

            MusicVolumeValue = int.Parse(Sys.FFNxConfig.Get("external_music_volume"));
            if (MusicVolumeValue < 0) MusicVolumeValue = 100;

            VoiceVolumeValue = int.Parse(Sys.FFNxConfig.Get("external_voice_volume"));
            if (VoiceVolumeValue < 0) VoiceVolumeValue = 100;

            AmbientVolumeValue = int.Parse(Sys.FFNxConfig.Get("external_ambient_volume"));
            if (AmbientVolumeValue < 0) AmbientVolumeValue = 100;

            MovieVolumeValue = int.Parse(Sys.FFNxConfig.Get("ffmpeg_video_volume"));
            if (MovieVolumeValue < 0) MovieVolumeValue = 100;
           
        }

        private void SetVolumesInRegistry()
        {
            Sys.FFNxConfig.Set("external_sfx_volume", SfxVolumeValue.ToString());
            Sys.FFNxConfig.Set("external_music_volume", MusicVolumeValue.ToString());
            Sys.FFNxConfig.Set("external_voice_volume", VoiceVolumeValue.ToString());
            Sys.FFNxConfig.Set("external_ambient_volume", AmbientVolumeValue.ToString());
            Sys.FFNxConfig.Set("ffmpeg_video_volume", MovieVolumeValue.ToString());
        }

        private void SetSelectedSoundDeviceFromSettings(LaunchSettings launchSettings)
        {
            if (SoundDeviceGuids == null || SoundDeviceGuids.Count == 0)
            {
                return;
            }

            SelectedSoundDevice = SoundDeviceGuids.Where(s => s.Value == launchSettings.SelectedSoundDevice)
                                                  .Select(s => s.Key)
                                                  .FirstOrDefault();

            // switch back to 'Auto' if device not found
            if (SelectedSoundDevice == null)
            {
                SelectedSoundDevice = SoundDeviceGuids.Where(s => s.Value == Guid.Empty)
                                                      .Select(s => s.Key)
                                                      .FirstOrDefault();
            }
        }

        private void SetSelectedMidiDeviceFromSettings()
        {
            int MidiDevIDFromReg = GetMidiDeviceInRegistry();

            if (MidiDevIDFromReg < MidiDevices.Count - 1)
            {
                SelectedMidiDevice = MidiDevices.ElementAt(0);
            }
            else
            {
                SelectedMidiDevice = MidiDevices.ElementAt(MidiDevIDFromReg);
            }

        }

        internal bool SaveSettings()
        {
            try
            {
                Sys.Settings.ProgramsToLaunchPrior = GetUpdatedProgramsToRun();

                Sys.Settings.GameLaunchSettings.AutoUpdateDiscPath = AutoUpdatePathChecked;
                Sys.Settings.GameLaunchSettings.ShowLauncherWindow = IsShowLauncherChecked;

                Sys.Settings.GameLaunchSettings.SelectedSoundDevice = SoundDeviceGuids[SelectedSoundDevice];
                Sys.Settings.GameLaunchSettings.SelectedMidiDevice = MidiDeviceIDs[SelectedMidiDevice];
                Sys.Settings.GameLaunchSettings.ReverseSpeakers = IsReverseSpeakersChecked;
                Sys.Settings.GameLaunchSettings.LogarithmicVolumeControl = IsLogVolumeChecked;

                RegistryHelper.BeginTransaction();
                SetVolumesInRegistry();
                SetMidiDeviceInRegistry();
                RegistryHelper.CommitTransaction();

                if (Sys.Settings.FF8InstalledVersion == FF8Version.Steam) Sys.FFNxConfig.Save();

                Sys.SaveSettings();

                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.GameLauncherSettingsUpdated)));
                return true;
            }
            catch (Exception e)
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FailedToSaveLaunchSettings), e.Message, ResourceHelper.Get(StringKey.Error), MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error(e);
                return false;
            }
        }

        private void InitSoundDevices()
        {
            var deviceGuids = new Dictionary<string, Guid>();

            deviceGuids.Add("Auto-Switch (Windows Default)", Guid.Empty);

            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                WaveOutCapabilities caps;

                try
                {
                    caps = WaveOut.GetCapabilities(n);
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                    continue;
                }

                if (caps.ProductGuid == Guid.Empty)
                    continue;

                // reference: https://stackoverflow.com/questions/1449162/get-the-full-name-of-a-wavein-device
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (device.FriendlyName.StartsWith(caps.ProductName) && !deviceGuids.ContainsKey(device.FriendlyName))
                    {
                        Guid audioGuid = new Guid(device.Properties[PropertyKeys.PKEY_AudioEndpoint_GUID].Value as string);
                        deviceGuids.Add(device.FriendlyName, audioGuid);
                        break;
                    }
                }
            }

            SoundDeviceGuids = deviceGuids;
            NotifyPropertyChanged(nameof(SoundDevices));
        }

        private DMUS_PORTCAPS GetMidiDeviceCaps(int deviceIndex)
        {
            DMUS_PORTCAPS caps = new DMUS_PORTCAPS();

            try
            {
                unsafe {
                    caps.dwSize = (uint)sizeof(DMUS_PORTCAPS);
                }
                _directMusic.EnumPort((uint)deviceIndex, ref caps);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            
            return caps;
        }

        private void InitMidiDevices()
        {
            var deviceIDs = new Dictionary<string, Guid>();

            int deviceIndex = 0;
            while(true)
            {
                DMUS_PORTCAPS caps = GetMidiDeviceCaps(deviceIndex);

                if (caps.guidPort == Guid.Empty) break;
                deviceIDs.Add($"{deviceIndex}: " + caps.wszDescription.ToString(), caps.guidPort);
                deviceIndex++;
            }

            MidiDeviceIDs = deviceIDs;
            NotifyPropertyChanged(nameof(MidiDevices));    
        }

        /// <summary>
        /// Plays audio_test_file.ogg based on launch settings for testing audio
        /// </summary>
        /// <param name="channel"> Where audio will play from: Left,Center,or Right channel</param>
        internal void TestAudio(AudioChannel channel)
        {
            string pathToTestFile = Path.Combine(Sys._J8Folder, "Resources", "audio_test_file.ogg");


            if (_audioTest == null)
            {
                // reference: https://markheath.net/post/handling-multi-channel-audio-in-naudio
                // reference: https://stackoverflow.com/questions/22248138/play-sound-on-specific-channel-with-naudio?rq=1

                // input 0 - audio test .ogg
                // input 1 - silenced wave provider to play silent audio
                NAudio.Vorbis.VorbisWaveReader waveReader = new NAudio.Vorbis.VorbisWaveReader(pathToTestFile);
                MultiplexingWaveProvider waveProvider = new MultiplexingWaveProvider(new List<IWaveProvider>() { waveReader, new SilenceWaveProvider(waveReader.WaveFormat) }, 2);

                int leftChannel = 0;
                int rightChannel = 1;

                if (IsReverseSpeakersChecked)
                {
                    leftChannel = 1;
                    rightChannel = 0;
                }

                if (channel == AudioChannel.Left)
                {
                    // note that the wave reader has 2 input channels so we must route both input channels of the reader to the output channel
                    waveProvider.ConnectInputToOutput(0, leftChannel);
                    waveProvider.ConnectInputToOutput(1, leftChannel);

                    waveProvider.ConnectInputToOutput(2, rightChannel);
                    waveProvider.ConnectInputToOutput(3, rightChannel);
                }
                else if (channel == AudioChannel.Right)
                {
                    waveProvider.ConnectInputToOutput(0, rightChannel);
                    waveProvider.ConnectInputToOutput(1, rightChannel);

                    waveProvider.ConnectInputToOutput(2, leftChannel);
                    waveProvider.ConnectInputToOutput(3, leftChannel);
                }
                else
                {
                    waveProvider.ConnectInputToOutput(0, leftChannel);
                    waveProvider.ConnectInputToOutput(1, leftChannel);

                    waveProvider.ConnectInputToOutput(0, rightChannel);
                    waveProvider.ConnectInputToOutput(1, rightChannel);
                }

                float outVolume = (float)MusicVolumeValue / (float)100.0;
                if (LastVolumeSliderChanged == VolumeSlider.Sfx)
                {
                    outVolume = (float)SfxVolumeValue / (float)100.0;
                }

                _audioTest = new WaveOut
                {
                    DeviceNumber = GetSelectedSoundDeviceNumber(),
                    Volume = outVolume
                };

                _audioTest.Init(waveProvider);

                _audioTest.PlaybackStopped += AudioTest_PlaybackStopped;
                _audioTest.Play();

                NotifyPropertyChanged(nameof(IsAudioPlaying));
                NotifyPropertyChanged(nameof(IsAudioNotPlaying));
            }
        }

        private int GetSelectedSoundDeviceNumber()
        {
            Guid selectedGuid = SoundDeviceGuids[SelectedSoundDevice];

            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);

                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (device.FriendlyName.StartsWith(caps.ProductName))
                    {
                        Guid soundGuid = new Guid(device.Properties[PropertyKeys.PKEY_AudioEndpoint_GUID].Value as string);

                        if (soundGuid == selectedGuid)
                        {
                            return n;
                        }
                    }
                }
            }

            return -1; // if device not found return default device which is -1
        }

        private void AudioTest_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (_audioTest != null)
            {
                _audioTest.Stop();
                _audioTest.PlaybackStopped -= AudioTest_PlaybackStopped;
                _audioTest = null;

                NotifyPropertyChanged(nameof(IsAudioPlaying));
                NotifyPropertyChanged(nameof(IsAudioNotPlaying));
            }
        }

        internal void EditSelectedProgram(ProgramToRunViewModel selected)
        {
            IsProgramPopupOpen = true;
            NewProgramPathText = selected.ProgramPath;
            NewProgramArgsText = selected.ProgramArguments ?? "";
        }

        internal void AddNewProgram()
        {
            IsProgramPopupOpen = true;
        }

        /// <summary>
        /// Adds or Edits program to run and closes programs popup
        /// </summary>
        internal bool SaveProgramToRun()
        {
            if (!File.Exists(NewProgramPathText))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ProgramToRunNotFound), ResourceHelper.Get(StringKey.Error), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!ProgramList.Any(s => s.ProgramPath == NewProgramPathText))
            {
                ProgramList.Add(new ProgramToRunViewModel(NewProgramPathText, NewProgramArgsText));
            }
            else
            {
                ProgramToRunViewModel toEdit = ProgramList.FirstOrDefault(s => s.ProgramPath == NewProgramPathText);
                toEdit.ProgramArguments = NewProgramArgsText;
            }

            CloseProgramPopup();
            return true;
        }

        internal void CloseProgramPopup()
        {
            IsProgramPopupOpen = false;
            NewProgramPathText = "";
            NewProgramArgsText = "";
        }

        internal void RemoveSelectedProgram(ProgramToRunViewModel selected)
        {
            ProgramList.Remove(selected);
        }

        /// <summary>
        /// Returns list of <see cref="ProgramLaunchInfo"/> objects based on the current input in <see cref="ProgramList"/>
        /// </summary>
        private List<ProgramLaunchInfo> GetUpdatedProgramsToRun()
        {
            List<ProgramLaunchInfo> updatedPrograms = new List<ProgramLaunchInfo>();

            foreach (ProgramToRunViewModel item in ProgramList.ToList())
            {
                ProgramLaunchInfo existingProg = Sys.Settings.ProgramsToLaunchPrior.FirstOrDefault(s => s.PathToProgram == item.ProgramPath);

                if (existingProg == null)
                {
                    existingProg = new ProgramLaunchInfo() { PathToProgram = item.ProgramPath, ProgramArgs = item.ProgramArguments };
                }
                else
                {
                    existingProg.ProgramArgs = item.ProgramArguments;
                }

                updatedPrograms.Add(existingProg);
            }

            return updatedPrograms;
        }

        internal void ChangeProgramOrder(ProgramToRunViewModel program, int delta)
        {
            int currentIndex = ProgramList.IndexOf(program);
            int targetIndex = currentIndex + delta;

            if (targetIndex == currentIndex || targetIndex < 0 || targetIndex >= ProgramList.Count)
            {
                return;
            }

            ProgramList.Move(currentIndex, targetIndex);
        }

        internal void ResetToDefaults()
        {
            Logger.Info("Resetting game launcher settings to defaults.");
            LoadSettings(LaunchSettings.DefaultSettings());
        }

    }
}
