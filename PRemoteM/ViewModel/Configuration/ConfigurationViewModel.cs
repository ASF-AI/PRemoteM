﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using com.github.xiangyuecn.rsacsharp;
using Microsoft.Win32;
using PRM.Core;
using PRM.Core.DB;
using PRM.Core.I;
using PRM.Core.Model;
using PRM.Core.Service;
using Shawn.Utils;
using HotkeyModifierKeys = PRM.Core.Service.HotkeyModifierKeys;

namespace PRM.ViewModel.Configuration
{
    public class ConfigurationViewModel : NotifyPropertyChangedBase
    {
        public VmMain Host = null;
        private readonly PrmContext _context;
        private readonly LanguageService _languageService;
        private readonly ConfigurationService _configurationService;
        private readonly ProtocolConfigurationService _protocolConfigurationService;
        private readonly LauncherService _launcherService;
        private readonly DataService _dataService;
        private readonly ThemeService _themeService;

        protected ConfigurationViewModel(
            PrmContext context,
            string languageCode = "")
        {
            _context = context;
            _configurationService = context.ConfigurationService;
            _protocolConfigurationService = context.ProtocolConfigurationService;
            _languageService = context.LanguageService;
            _launcherService = context.LauncherService;
            _dataService = context.DataService;
            _themeService = context.ThemeService;

            if (string.IsNullOrEmpty(languageCode) == false
                && _languageService.LanguageCode2Name.ContainsKey(languageCode)
                && _languageService.SetLanguage(languageCode))
            {
                _configurationService.General.CurrentLanguageCode = languageCode;
            }
        }

        private static ConfigurationViewModel _configuration;

        public static void Init(PrmContext context, string languageCode = "")
        {
            _configuration = new ConfigurationViewModel(context, languageCode);
        }

        public static ConfigurationViewModel GetInstance(VmMain host = null)
        {
            Debug.Assert(_configuration != null);
            if (host != null)
            {
                _configuration.Host = host;
            }
            return _configuration;
        }


        private Visibility _progressBarVisibility = Visibility.Collapsed;
        public Visibility ProgressBarVisibility
        {
            get => _progressBarVisibility;
            private set => SetAndNotifyIfChanged(ref _progressBarVisibility, value);
        }


        private RelayCommand _cmdSaveAndGoBack;

        public RelayCommand CmdSaveAndGoBack
        {
            get
            {
                if (_cmdSaveAndGoBack != null) return _cmdSaveAndGoBack;
                _cmdSaveAndGoBack = new RelayCommand((o) =>
                {
                    // check if Db is ok
                    var res = _context.DataService?.Database_SelfCheck() ?? EnumDbStatus.AccessDenied;
                    if (res != EnumDbStatus.OK)
                    {
                        MessageBox.Show(res.GetErrorInfo(_context.LanguageService), _context.LanguageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                        return;
                    }


                    _configurationService.Save();
                    _protocolConfigurationService.Save();
                    if (Host != null)
                        Host.DispPage = null;
                });
                return _cmdSaveAndGoBack;
            }
        }

        private RelayCommand _cmdOpenPath;
        public RelayCommand CmdOpenPath
        {
            get
            {
                if (_cmdOpenPath != null) return _cmdOpenPath;
                _cmdOpenPath = new RelayCommand((o) =>
                {
                    var path = o.ToString();
                    if (File.Exists(path))
                    {
                        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe");
                        psi.Arguments = "/e,/select," + path;
                        System.Diagnostics.Process.Start(psi);
                    }

                    if (Directory.Exists(path))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", path);
                    }
                });
                return _cmdOpenPath;
            }
        }







        public Dictionary<string, string> Languages => _languageService.LanguageCode2Name;
        public string Language
        {
            get => _configurationService.General.CurrentLanguageCode;
            set
            {
                Debug.Assert(Languages.ContainsKey(value));
                if (SetAndNotifyIfChanged(ref _configurationService.General.CurrentLanguageCode, value))
                {
                    // reset lang service
                    _languageService.SetLanguage(value);
                    _configurationService.Save();
                }
            }
        }

        public bool AppStartAutomatically
        {
            get => _configurationService.General.AppStartAutomatically;
            set
            {
                if (SetAndNotifyIfChanged(ref _configurationService.General.AppStartAutomatically, value))
                {
                    _configurationService.Save();
                }
            }
        }

        public bool AppStartMinimized
        {
            get => _configurationService.General.AppStartMinimized;
            set
            {
                if (SetAndNotifyIfChanged(ref _configurationService.General.AppStartMinimized, value))
                {
                    _configurationService.Save();
                }
            }
        }

        public bool ListPageIsCardView
        {
            get => _configurationService.General.ListPageIsCardView;
            set
            {
                if (SetAndNotifyIfChanged(ref _configurationService.General.ListPageIsCardView, value))
                {
                    _configurationService.Save();
                }
            }
        }



        public bool LauncherEnabled
        {
            get => _configurationService.Launcher.LauncherEnabled;
            set
            {
                if (SetAndNotifyIfChanged(ref _configurationService.Launcher.LauncherEnabled, value))
                {
                    _configurationService.Save();
                    GlobalEventHelper.OnLauncherHotKeyChanged?.Invoke();
                }
            }
        }

        public HotkeyModifierKeys LauncherHotKeyModifiers
        {
            get => _configurationService.Launcher.HotKeyModifiers;
            set
            {
                if (value != LauncherHotKeyModifiers
                    && _launcherService.CheckIfHotkeyAvailable(value, LauncherHotKeyKey)
                    && SetAndNotifyIfChanged(ref _configurationService.Launcher.HotKeyModifiers, value))
                {
                    _configurationService.Save();
                    GlobalEventHelper.OnLauncherHotKeyChanged?.Invoke();
                }
            }
        }

        public Key LauncherHotKeyKey
        {
            get => _configurationService.Launcher.HotKeyKey;
            set
            {
                if (value != LauncherHotKeyKey
                    && _launcherService.CheckIfHotkeyAvailable(LauncherHotKeyModifiers, value)
                    && SetAndNotifyIfChanged(ref _configurationService.Launcher.HotKeyKey, value))
                {
                    _configurationService.Save();
                    GlobalEventHelper.OnLauncherHotKeyChanged?.Invoke();
                }
            }
        }

        public string LogFolderName => new FileInfo(SimpleLogHelper.LogFileName).DirectoryName;

        public List<MatchProviderInfo> AvailableMatcherProviders => _configurationService.AvailableMatcherProviders;

        #region Database

        public string DbPath => _configurationService.Database.SqliteDatabasePath;
        public string RsaPublicKey => _dataService.Database_GetPublicKey();
        public string RsaPrivateKeyPath => _dataService.Database_GetPrivateKeyPath();



        private bool ValidateDbStatusAndShowMessageBox()
        {
            // validate rsa key
            var res = _dataService.Database_SelfCheck();
            RaisePropertyChanged(nameof(RsaPublicKey));
            RaisePropertyChanged(nameof(RsaPrivateKeyPath));
            if (res == EnumDbStatus.OK) return true;
            MessageBox.Show(res.GetErrorInfo(_languageService), _languageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
            return false;
        }

        private RelayCommand _cmdGenRsaKey;

        public RelayCommand CmdGenRsaKey
        {
            get
            {
                return _cmdGenRsaKey ??= new RelayCommand((o) =>
                {
                    // validate rsa key
                    if (!ValidateDbStatusAndShowMessageBox())
                    {
                        return;
                    }
                    if (_dataService.Database_IsEncrypted() == true)
                    {
                        return;
                    }
                    GenRsa();
                    RaisePropertyChanged(nameof(RsaPublicKey));
                    RaisePropertyChanged(nameof(RsaPrivateKeyPath));
                });
            }
        }

        /// <summary>
        /// Invoke Progress bar percent = arg1 / arg2
        /// </summary>
        private void OnRsaProgress(int now, int total)
        {
            GlobalEventHelper.OnLongTimeProgress?.Invoke(now, total, _languageService.Translate("system_options_data_security_info_data_processing"));
        }

        private const string PrivateKeyFileExt = ".prpk";
        public Task GenRsa(string privateKeyPath = "")
        {
            // validate rsa key
            Debug.Assert(_dataService.Database_IsEncrypted() == false);
            var t = new Task(() =>
            {
                lock (this)
                {
                    if (_dataService.Database_IsEncrypted()) return;

                    if (string.IsNullOrEmpty(privateKeyPath))
                    {
                        var path = SelectFileHelper.OpenFile(
                            selectedFileName: ConfigurationService.AppName + "_" + DateTime.Now.ToString("yyyyMMddhhmmss") + PrivateKeyFileExt,
                            checkFileExists: false,
                            filter: $"PRM RSA private key|*{PrivateKeyFileExt}");
                        if (path == null) return;
                        privateKeyPath = path;
                    }

                    int max = this._context.AppData.VmItemList.Count() * 3 + 2;
                    int val = 0;
                    OnRsaProgress(val, max);
                    // database back up
                    Debug.Assert(File.Exists(DbPath));
                    File.Copy(DbPath, DbPath + ".back", true);
                    OnRsaProgress(++val, max);

                    if (!File.Exists(privateKeyPath))
                    {
                        // gen rsa
                        var rsa = new RSA(2048);
                        // save key file
                        File.WriteAllText(privateKeyPath, rsa.ToPEM_PKCS1());
                    }

                    OnRsaProgress(++val, max);

                    if (_dataService.Database_SetEncryptionKey(privateKeyPath) != RSA.EnumRsaStatus.NoError)
                    {
                        MessageBox.Show(EnumDbStatus.RsaPrivateKeyFormatError.GetErrorInfo(_languageService), _languageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                        OnRsaProgress(0, 0);
                        return;
                    }

                    // encrypt old data
                    foreach (var vmProtocolServer in this._context.AppData.VmItemList)
                    {
                        OnRsaProgress(++val, max);
                        _dataService.Database_UpdateServer(vmProtocolServer.Server);
                        OnRsaProgress(++val, max);
                    }

                    // del back up
                    File.Delete(DbPath + ".back");

                    // done
                    OnRsaProgress(0, 0);

                    RaisePropertyChanged(nameof(RsaPublicKey));
                    RaisePropertyChanged(nameof(RsaPrivateKeyPath));
                }
            });
            t.Start();
            return t;
        }




        private RelayCommand _cmdClearRsaKey;

        public RelayCommand CmdClearRsaKey
        {
            get
            {
                return _cmdClearRsaKey ??= new RelayCommand((o) =>
                {
                    // validate rsa key
                    if (!ValidateDbStatusAndShowMessageBox())
                    {
                        return;
                    }
                    if (_dataService.Database_IsEncrypted() != true)
                    {
                        return;
                    }
                    CleanRsa();
                });
            }
        }
        public Task CleanRsa()
        {
            Debug.Assert(_dataService.Database_IsEncrypted() == true);
            var t = new Task(() =>
            {
                lock (this)
                {
                    if (!_dataService.Database_IsEncrypted()) return;
                    OnRsaProgress(0, 1);
                    int max = this._context.AppData.VmItemList.Count() * 3 + 2 + 1;
                    int val = 1;
                    OnRsaProgress(val, max);

                    // database back up
                    Debug.Assert(File.Exists(DbPath));
                    File.Copy(DbPath, DbPath + ".back", true);
                    OnRsaProgress(++val, max);

                    // decrypt pwd
                    foreach (var vmProtocolServer in this._context.AppData.VmItemList)
                    {
                        this._dataService.DecryptToConnectLevel(vmProtocolServer.Server);
                        OnRsaProgress(++val, max);
                    }

                    // remove rsa keys from db
                    this._dataService.Database_SetEncryptionKey("");

                    // update db
                    foreach (var vmProtocolServer in this._context.AppData.VmItemList)
                    {
                        this._dataService.Database_UpdateServer(vmProtocolServer.Server);
                        OnRsaProgress(++val, max);
                    }

                    // del key
                    //File.Delete(ppkPath);

                    // del back up
                    File.Delete(DbPath + ".back");

                    // done
                    OnRsaProgress(0, 0);

                    RaisePropertyChanged(nameof(RsaPublicKey));
                    RaisePropertyChanged(nameof(RsaPrivateKeyPath));
                }
            });
            t.Start();
            return t;
        }



        private RelayCommand _cmdSelectRsaPrivateKey;

        public RelayCommand CmdSelectRsaPrivateKey
        {
            get
            {
                if (_cmdSelectRsaPrivateKey == null)
                {
                    _cmdSelectRsaPrivateKey = new RelayCommand((o) =>
                    {
                        lock (this)
                        {
                            if (!_dataService.Database_IsEncrypted())
                            {
                                return;
                            }
                            var path = SelectFileHelper.OpenFile(
                                initialDirectory: new FileInfo(RsaPrivateKeyPath).DirectoryName,
                                filter: $"PRM RSA private key|*{PrivateKeyFileExt}");
                            if (path == null) return;
                            var pks = RSA.CheckPrivatePublicKeyMatch(path, _context.DataService.Database_GetPublicKey());
                            if (pks == RSA.EnumRsaStatus.NoError)
                            {
                                _dataService.Database_SetEncryptionKey(path, false);
                                RaisePropertyChanged(nameof(RsaPublicKey));
                                RaisePropertyChanged(nameof(RsaPrivateKeyPath));
                            }
                            else
                            {
                                MessageBox.Show(EnumDbStatus.RsaNotMatched.GetErrorInfo(_languageService), _languageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                            }
                        }
                    }, o => _context.DataService != null && _context.DataService.Database_IsEncrypted());
                }
                return _cmdSelectRsaPrivateKey;
            }
        }


        private RelayCommand _cmdSelectDbPath;

        public RelayCommand CmdSelectDbPath
        {
            get
            {
                return _cmdSelectDbPath ??= new RelayCommand((o) =>
                {
                    var path = SelectFileHelper.OpenFile(
                        initialDirectory: new FileInfo(DbPath).DirectoryName,
                        filter: "Sqlite Database|*.db");
                    if (path == null) return;
                    var oldDbPath = DbPath;
                    if (string.Equals(path, oldDbPath, StringComparison.CurrentCultureIgnoreCase))
                        return;

                    if (!IOPermissionHelper.HasWritePermissionOnFile(path))
                    {
                        MessageBox.Show(_languageService.Translate("string_database_error_permission_denied"), _languageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                        return;
                    }

                    try
                    {
                        _dataService.Database_OpenConnection(DatabaseType.Sqlite, DbExtensions.GetSqliteConnectionString(path));
                        _configurationService.Database.SqliteDatabasePath = path;
                        RaisePropertyChanged(nameof(DbPath));
                        _context.AppData.ReloadServerList();
                        _configurationService.Save();
                        ValidateDbStatusAndShowMessageBox();
                    }
                    catch (Exception ee)
                    {
                        _configurationService.Database.SqliteDatabasePath = oldDbPath;
                        _dataService.Database_OpenConnection(DatabaseType.Sqlite, DbExtensions.GetSqliteConnectionString(oldDbPath));
                        SimpleLogHelper.Warning(ee);
                        MessageBox.Show(_languageService.Translate("system_options_data_security_error_can_not_open"), _languageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                    }
                });
            }
        }



        private RelayCommand _cmdDbMigrate;

        public RelayCommand CmdDbMigrate
        {
            get
            {
                return _cmdDbMigrate ??= new RelayCommand((o) =>
                {
                    var path = SelectFileHelper.SaveFile(filter: "Sqlite Database|*.db", initialDirectory: new FileInfo(DbPath).DirectoryName, selectedFileName: new FileInfo(DbPath).Name);
                    if (path == null) return;
                    var oldDbPath = DbPath;
                    if (oldDbPath == path)
                        return;
                    try
                    {
                        if (IOPermissionHelper.HasWritePermissionOnFile(path))
                        {
                            this._context.DataService.Database_CloseConnection();
                            File.Copy(oldDbPath, path);
                            Thread.Sleep(500);
                            this._context.DataService.Database_OpenConnection(DatabaseType.Sqlite, DbExtensions.GetSqliteConnectionString(path));
                            // Migrate do not need reload data
                            // this._appContext.AppData.ReloadServerList();
                            _configurationService.Database.SqliteDatabasePath = path;
                            File.Delete(oldDbPath);
                        }
                        else
                            MessageBox.Show(_languageService.Translate("system_options_data_security_error_can_not_open"), _languageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                    }
                    catch (Exception ee)
                    {
                        SimpleLogHelper.Error(ee);
                        File.Delete(path);
                        _configurationService.Database.SqliteDatabasePath = oldDbPath;
                        this._context.DataService.Database_OpenConnection(DatabaseType.Sqlite, DbExtensions.GetSqliteConnectionString(oldDbPath));
                        MessageBox.Show(_languageService.Translate("system_options_data_security_error_can_not_open"), _languageService.Translate("messagebox_title_error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                    }
                    RaisePropertyChanged(nameof(DbPath));
                    _configurationService.Save();
                });
            }
        }


        #endregion


        #region UI

        private void SetTheme(string name)
        {
            Debug.Assert(_themeService.Themes.ContainsKey(name));
            var theme = _themeService.Themes[name];
            _configurationService.Theme.PrimaryMidColor = theme.PrimaryMidColor;
            _configurationService.Theme.PrimaryLightColor = theme.PrimaryLightColor;
            _configurationService.Theme.PrimaryDarkColor = theme.PrimaryDarkColor;
            _configurationService.Theme.PrimaryTextColor = theme.PrimaryTextColor;
            _configurationService.Theme.AccentMidColor = theme.AccentMidColor;
            _configurationService.Theme.AccentLightColor = theme.AccentLightColor;
            _configurationService.Theme.AccentDarkColor = theme.AccentDarkColor;
            _configurationService.Theme.AccentTextColor = theme.AccentTextColor;
            _configurationService.Theme.BackgroundColor = theme.BackgroundColor;
            _configurationService.Theme.BackgroundTextColor = theme.BackgroundTextColor;

            RaisePropertyChanged(nameof(PrimaryMidColor));
            RaisePropertyChanged(nameof(PrimaryLightColor));
            RaisePropertyChanged(nameof(PrimaryDarkColor));
            RaisePropertyChanged(nameof(PrimaryTextColor));
            RaisePropertyChanged(nameof(AccentMidColor));
            RaisePropertyChanged(nameof(AccentLightColor));
            RaisePropertyChanged(nameof(AccentDarkColor));
            RaisePropertyChanged(nameof(AccentTextColor));
            RaisePropertyChanged(nameof(BackgroundColor));
            RaisePropertyChanged(nameof(BackgroundTextColor));

            _themeService.ApplyTheme(_configurationService.Theme);
        }

        public string ThemeName
        {
            get => _configurationService.Theme.ThemeName;
            set
            {
                Debug.Assert(_themeService.Themes.ContainsKey(value));
                if (SetAndNotifyIfChanged(ref _configurationService.Theme.ThemeName, value))
                {
                    SetTheme(value);
                    _configurationService.Save();
                }
            }
        }

        public List<string> ThemeList => _themeService.Themes.Select(x => x.Key).ToList();


        public string PrimaryMidColor
        {
            get => _configurationService.Theme.PrimaryMidColor;
            set
            {
                try
                {
                    if (SetAndNotifyIfChanged(ref _configurationService.Theme.PrimaryMidColor, value))
                    {
                        var color = ColorAndBrushHelper.HexColorToMediaColor(value);
                        _configurationService.Theme.PrimaryLightColor = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(Math.Min(color.R + 50, 255), Math.Min(color.G + 45, 255), Math.Min(color.B + 40, 255)));
                        _configurationService.Theme.PrimaryDarkColor = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb((int)(color.R * 0.8), (int)(color.G * 0.8), (int)(color.B * 0.8)));
                        RaisePropertyChanged(nameof(PrimaryLightColor));
                        RaisePropertyChanged(nameof(PrimaryDarkColor));
                        _themeService.ApplyTheme(_configurationService.Theme);
                    }
                }
                catch (Exception e)
                {
                    SimpleLogHelper.Debug(e);
                }
            }
        }
        public string PrimaryLightColor
        {
            get => _configurationService.Theme.PrimaryLightColor;
            set
            {
                SetAndNotifyIfChanged(ref _configurationService.Theme.PrimaryLightColor, value);
                _themeService.ApplyTheme(_configurationService.Theme);
            }
        }

        public string PrimaryDarkColor
        {
            get => _configurationService.Theme.PrimaryDarkColor;
            set
            {
                SetAndNotifyIfChanged(ref _configurationService.Theme.PrimaryDarkColor, value);
                _themeService.ApplyTheme(_configurationService.Theme);
            }
        }

        public string PrimaryTextColor
        {
            get => _configurationService.Theme.PrimaryTextColor;
            set
            {
                SetAndNotifyIfChanged(ref _configurationService.Theme.PrimaryTextColor, value);
                _themeService.ApplyTheme(_configurationService.Theme);
            }
        }

        public string AccentMidColor
        {
            get => _configurationService.Theme.AccentMidColor;
            set
            {
                try
                {
                    if (SetAndNotifyIfChanged(ref _configurationService.Theme.AccentMidColor, value))
                    {
                        var color = ColorAndBrushHelper.HexColorToMediaColor(value);
                        _configurationService.Theme.AccentLightColor = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(Math.Min(color.R + 50, 255), Math.Min(color.G + 45, 255), Math.Min(color.B + 40, 255)));
                        _configurationService.Theme.AccentDarkColor = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb((int)(color.R * 0.8), (int)(color.G * 0.8), (int)(color.B * 0.8)));
                        RaisePropertyChanged(nameof(AccentLightColor));
                        RaisePropertyChanged(nameof(AccentDarkColor));
                        _themeService.ApplyTheme(_configurationService.Theme);
                    }
                }
                catch (Exception e)
                {
                    SimpleLogHelper.Debug(e);
                }
            }
        }
        public string AccentLightColor
        {
            get => _configurationService.Theme.AccentLightColor;
            set
            {
                SetAndNotifyIfChanged(ref _configurationService.Theme.AccentLightColor, value);
                _themeService.ApplyTheme(_configurationService.Theme);
            }
        }

        public string AccentDarkColor
        {
            get => _configurationService.Theme.AccentDarkColor;
            set
            {
                SetAndNotifyIfChanged(ref _configurationService.Theme.AccentDarkColor, value);
                _themeService.ApplyTheme(_configurationService.Theme);
            }
        }

        public string AccentTextColor
        {
            get => _configurationService.Theme.AccentTextColor;
            set
            {
                SetAndNotifyIfChanged(ref _configurationService.Theme.AccentTextColor, value);
                _themeService.ApplyTheme(_configurationService.Theme);
            }
        }

        public string BackgroundColor
        {
            get => _configurationService.Theme.BackgroundColor;
            set => SetAndNotifyIfChanged(ref _configurationService.Theme.BackgroundColor, value);
        }
        public string BackgroundTextColor
        {
            get => _configurationService.Theme.BackgroundTextColor;
            set
            {
                SetAndNotifyIfChanged(ref _configurationService.Theme.BackgroundTextColor, value);
                _themeService.ApplyTheme(_configurationService.Theme);
            }
        }


        private RelayCommand _cmdPrmThemeReset;

        public RelayCommand CmdResetTheme
        {
            get
            {
                if (_cmdPrmThemeReset == null)
                {
                    _cmdPrmThemeReset = new RelayCommand((o) =>
                    {
                        SetTheme(ThemeName);
                        _configurationService.Save();
                        _themeService.ApplyTheme(_configurationService.Theme);
                    });
                }
                return _cmdPrmThemeReset;
            }
        }
        #endregion
    }
}
