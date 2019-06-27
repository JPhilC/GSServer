﻿/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
using GS.Server.Domain;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.SkyTelescope;
using GS.Shared;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace GS.Server.Settings
{
    public class SettingsVM : ObservableObject, IPageVM
    {
        #region fields

        public string TopName => "GS Server";
        public string BottomName => "Settings";
        public int Uid => 3;
        private readonly MainWindowVM _mainWindowVm;
        public static SettingsVM _settingsVM;
        private readonly CancellationToken _cts;

        #endregion

        public SettingsVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    _settingsVM = this;

                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Loading SettingsVM" };
                    MonitorLog.LogToMonitor(monitorItem);

                    //token to cancel UI updates
                    _cts = new CancellationToken();

                    //subscribe to Server property changes
                    Settings.StaticPropertyChanged += PropertyChangedServer;

                    //subscribe to Server property Monitor items
                    MonitorQueue.StaticPropertyChanged += PropertyChangedMonitorQueue;

                    MonitorEntries = new ObservableCollection<MonitorEntry>();
                    StartStopButtonText = Shared.Settings.StartMonitor ? @"Stop" : @"Start";

                    _mainWindowVm = MainWindowVM._mainWindowVm;
                    SleepMode = Settings.SleepMode;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        #region ViewModel

          /// <summary>
        /// Subscription to changes from Settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedServer(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "Charting":
                                Charting = Settings.Charting;
                                break;
                            case "Focuser":
                                Focuser = Settings.Focuser;
                                break;
                            case "Gamepad":
                                Gamepad = Settings.Gamepad;
                                break;
                            case "SkyWatcher":
                                SkyWatcher = Settings.SkyWatcher;
                                break;
                            case "SleepMode":
                                SleepMode = Settings.SleepMode;
                                break;
                            case "StartMinimized":
                                StartMinimized = Settings.StartMinimized;
                                break;
                            case "StartOnTop":
                                StartOnTop = Settings.StartOnTop;
                                break;
                            case "VoiceActive":
                                VoiceActive = Settings.VoiceActive;
                                break;
                            case "VoiceName":
                                VoiceName = Settings.VoiceName;
                                break;
                            case "VoiceVolume":
                                VoiceVolume = Settings.VoiceVolume;
                                break;
                        }
                    }, _cts);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }      

        #endregion

        #region Bindings

        public bool SkyWatcher
        {
            get => Settings.SkyWatcher;
            set
            {
                Settings.SkyWatcher = value;
                _mainWindowVm.UpdateTabViewModel("SkyWatcher");
                OnPropertyChanged();
            }
        }

        public bool Focuser
        {
            get => Settings.Focuser;
            set
            {
                Settings.Focuser = value;
                _mainWindowVm.UpdateTabViewModel("Focuser");
                OnPropertyChanged();
            }
        }

        public bool Gamepad
        {
            get => Settings.Gamepad;
            set
            {
                Settings.Gamepad = value;
                _mainWindowVm.UpdateTabViewModel("Gamepad");
                OnPropertyChanged();
            }
        }

        public bool Charting
        {
            get => Settings.Charting;
            set
            {
                Settings.Charting = value;
                _mainWindowVm.UpdateTabViewModel("Charts");
                OnPropertyChanged();
            }
        }

        public bool Notes
        {
            get => Settings.Notes;
            set
            {
                Settings.Notes = value;
                _mainWindowVm.UpdateTabViewModel("Notes");
                OnPropertyChanged();
            }
        }

        private SleepMode _sleepMode;
        public bool SleepMode
        {
            get => Settings.SleepMode;
            set
            {
                if (value)
                {
                    if (_sleepMode == null)
                    {
                        _sleepMode = new SleepMode();
                        _sleepMode.SleepOn();
                    }
                }
                else
                {
                    if (_sleepMode != null)
                    {
                        _sleepMode.SleepOff();
                        _sleepMode = null;
                    }
                }
                Settings.SleepMode = value;
                OnPropertyChanged();
            }
        }

        public bool StartMinimized
        {
            get => Settings.StartMinimized;
            set
            {
                Settings.StartMinimized = value;
                OnPropertyChanged();
            }
        }

        public bool StartOnTop
        {
            get => Settings.StartOnTop;
            set
            {
                Settings.StartOnTop = value;
                OnPropertyChanged();
            }
        }

        public IList<string> VoiceNames => Synthesizer.GetVoices();

        public string VoiceName
        {
            get => Settings.VoiceName;
            set
            {
                Settings.VoiceName = value;
                Synthesizer.Speak(value);
                OnPropertyChanged();
            }
        }

        public bool VoiceActive
        {
            get => Settings.VoiceActive;
            set
            {
                Settings.VoiceActive = value;
                Synthesizer.VoiceActive = value;
                Synthesizer.Speak("Voice active");
                OnPropertyChanged();
                RaisePropertyChanged("VoiceActive");
            }
        }

        public int VoiceVolume
        {
            get => Settings.VoiceVolume;
            set
            {
                Settings.VoiceVolume = value;
                Synthesizer.Speak($"{value}");
                OnPropertyChanged();
            }
        }

        #endregion

        #region Properties

        private int _skyInterval;
        public int SkyInterval
        {
            get => SkySettings.SkyInterval;
            set
            {
                if (value == _skyInterval) return;
                _skyInterval = value;
                SkySettings.SkyInterval = value;
                OnPropertyChanged();
            }
        }
        
        private int _simInterval;
        public int SimInterval
        {
            get => SkySettings.SimInterval;
            set
            {
                if (value == _simInterval) return;
                _simInterval = value;
                SkySettings.SimInterval = value;
                OnPropertyChanged();
            }
        }

        public bool HomeWarning
        {
            get => SkySettings.HomeWarning;
            set
            {
                if (HomeWarning == value) return;
                SkySettings.HomeWarning = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Monitor

        public ObservableCollection<MonitorEntry> MonitorEntries { get; }

        private void PropertyChangedMonitorQueue(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "MonitorEntry":
                                UpdateUi(MonitorQueue.MonitorEntry);
                                break;
                        }
                    });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private void UpdateUi(MonitorEntry monitorEntry)
        {
            try
            {
                if (MonitorEntries.Count + 1 > 100000)
                {
                    MonitorEntries.Clear();
                    MonitorLog.ResetIndex();
                }
                monitorEntry.Message = monitorEntry.Message.Replace("\r", string.Empty);
                MonitorEntries.Add(monitorEntry);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        public bool ServerDevice
        {
            get => MonitorLog.InDevices(MonitorDevice.Server);
            set => MonitorLog.DevicesToMonitor(MonitorDevice.Server, value);
        }

        public bool Telescope
        {
            get => MonitorLog.InDevices(MonitorDevice.Telescope);
            set => MonitorLog.DevicesToMonitor(MonitorDevice.Telescope, value);
        }

        public bool Driver
        {
            get => MonitorLog.InCategory(MonitorCategory.Driver);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Driver, value);
        }

        public bool Interface
        {
            get => MonitorLog.InCategory(MonitorCategory.Interface);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Interface, value);
        }

        public bool Server
        {
            get => MonitorLog.InCategory(MonitorCategory.Server);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Server, value);
        }

        public bool Mount
        {
            get => MonitorLog.InCategory(MonitorCategory.Mount);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Mount, value);
        }

        public bool Information
        {
            get => MonitorLog.InTypes(MonitorType.Information);
            set => MonitorLog.TypesToMonitor(MonitorType.Information, value);
        }

        public bool Data
        {
            get => MonitorLog.InTypes(MonitorType.Data);
            set => MonitorLog.TypesToMonitor(MonitorType.Data, value);
        }

        public bool Warning
        {
            get => MonitorLog.InTypes(MonitorType.Warning);
            set => MonitorLog.TypesToMonitor(MonitorType.Warning, value);
        }

        public bool Error
        {
            get => MonitorLog.InTypes(MonitorType.Error);
            set => MonitorLog.TypesToMonitor(MonitorType.Error, value);
        }

        public bool SessionLog
        {
            get => Shared.Settings.LogSession;
            set
            {
                if (MonitorToFile == value) return;
                Shared.Settings.LogSession = value;
                OnPropertyChanged();
            }
        }

        public bool MonitorToFile
        {
            get => Shared.Settings.LogMonitor;
            set
            {
                if (MonitorToFile == value) return;
                Shared.Settings.LogMonitor = value;
                OnPropertyChanged();
            }
        }

        private ICommand _volumeupCommand;
        public ICommand VolumeupCommand
        {
            get
            {
                return _volumeupCommand ?? (_volumeupCommand = new RelayCommand(
                           param => Volumeup()
                       ));
            }
        }
        private void Volumeup()
        {
            try
            {
                var currentvVolume = VoiceVolume;
                if (currentvVolume <= 90)
                {
                    VoiceVolume += 10;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _volumedownCommand;
        public ICommand VolumedownCommand
        {
            get
            {
                return _volumedownCommand ?? (_volumedownCommand = new RelayCommand(
                           param => Volumedown()
                       ));
            }
        }
        private void Volumedown()
        {
            try
            {
                var currentvVolume = VoiceVolume;
                if (currentvVolume >= 10)
                {
                    VoiceVolume -= 10;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private string _startStopButtonText;
        public string StartStopButtonText
        {
            get => _startStopButtonText;
            set
            {
                if (_startStopButtonText == value) return;
                _startStopButtonText = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickStartStopCommand;
        public ICommand ClickStartStopCommand
        {
            get
            {
                return _clickStartStopCommand ?? (_clickStartStopCommand = new RelayCommand(
                           param => ClickStartStop()
                       ));
            }
        }
        private void ClickStartStop()
        {
            try
            {
                using (new WaitCursor())
                {
                    Shared.Settings.StartMonitor = !Shared.Settings.StartMonitor;
                    StartStopButtonText = StartStopButtonText == "Start" ? "Stop" : "Start";
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickClearCommand;
        public ICommand ClickClearCommand
        {
            get
            {
                return _clickClearCommand ?? (_clickClearCommand = new RelayCommand(
                           param => ClickClear()
                       ));
            }
        }
        private void ClickClear()
        {
            try
            {
                using (new WaitCursor())
                {
                    MonitorEntries.Clear();
                    MonitorLog.ResetIndex();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickExportCommand;
        public ICommand ClickExportCommand
        {
            get
            {
                return _clickExportCommand ?? (_clickExportCommand = new RelayCommand(
                           param => ClickExport()
                       ));
            }
        }
        private void ClickExport()
        {
            StreamWriter stream = null;
            try
            {
                Shared.Settings.StartMonitor = false;
                var saveFileDialog = new SaveFileDialog
                {
                    InitialDirectory = @"C:\",
                    Title = @"Save Monitor Information",
                    CheckFileExists = false,
                    CheckPathExists = false,
                    DefaultExt = "txt",
                    Filter = @"Txt files (*.txt)|*.txt|All files (*.*)|*.*",
                    FilterIndex = 2,
                    RestoreDirectory = true,
                    FileName = $"GSServerMonitor_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.txt"
                };
                if (saveFileDialog.ShowDialog() != true) return;
                using (new WaitCursor())
                {
                    stream = new StreamWriter(saveFileDialog.FileName);
                    foreach (var item in MonitorEntries)
                    {
                        stream.WriteLine(
                            $"{item.Index},{item.Datetime.ToLocalTime():dd/MM/yyyy HH:mm:ss.fff},{item.Device},{item.Category},{item.Type},{item.Thread},{item.Method},{item.Message}");
                    }
                    stream.Close();
                    stream = null;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
            finally
            {
                stream?.Dispose();
            }
        }

        #endregion

        #region Error  

        private string _DialogMsg;
        public string DialogMsg
        {
            get => _DialogMsg;
            set
            {
                if (_DialogMsg == value) return;
                _DialogMsg = value;
                OnPropertyChanged();
            }
        }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                if (_isDialogOpen == value) return;
                _isDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _dialogContent;
        public object DialogContent
        {
            get => _dialogContent;
            set
            {
                if (_dialogContent == value) return;
                _dialogContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openDialogCommand;
        public ICommand OpenDialogCommand
        {
            get
            {
                return _openDialogCommand ?? (_openDialogCommand = new RelayCommand(
                           param => OpenDialog(null)
                       ));
            }
        }
        private void OpenDialog(string msg)
        {
            if (msg != null) DialogMsg = msg;
            DialogContent = new Dialog();
            IsDialogOpen = true;

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{msg}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        private ICommand _clickOkDialogCommand;
        public ICommand ClickOkDialogCommand
        {
            get
            {
                return _clickOkDialogCommand ?? (_clickOkDialogCommand = new RelayCommand(
                           param => ClickOkDialog()
                       ));
            }
        }
        private void ClickOkDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _cancelDialogCommand;
        public ICommand CancelDialogCommand
        {
            get
            {
                return _cancelDialogCommand ?? (_cancelDialogCommand = new RelayCommand(
                           param => CancelDialog()
                       ));
            }
        }
        private void CancelDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _runMessageDialog;
        public ICommand RunMessageDialogCommand
        {
            get
            {
                return _runMessageDialog ?? (_runMessageDialog = new RelayCommand(
                           param => ExecuteMessageDialog()
                       ));
            }
        }
        private async void ExecuteMessageDialog()
        {
            //let's set up a little MVVM, cos that's what the cool kids are doing:
            var view = new ErrorMessageDialog
            {
                DataContext = new ErrorMessageDialogVM()
            };

            //show the dialog
            await DialogHost.Show(view, "RootDialog", ClosingMessageEventHandler);
        }
        private void ClosingMessageEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            Console.WriteLine(@"You can intercept the closing event, and cancel here.");
        }

        #endregion
    }

}
