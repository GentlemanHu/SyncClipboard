﻿using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SyncClipboard.Abstract;
using SyncClipboard.Core.Commons;
using SyncClipboard.Core.I18n;
using SyncClipboard.Core.Interfaces;
using SyncClipboard.Core.Models.UserConfigs;
using SyncClipboard.Core.UserServices;
using SyncClipboard.Core.Utilities;
using SyncClipboard.Core.Utilities.Web;
using SyncClipboard.Core.ViewModels;

namespace SyncClipboard.Core
{
    public class ProgramWorkflow
    {
        public IServiceProvider Services { get; }
        private ServiceManager? ServiceManager { get; set; }

        public ProgramWorkflow(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }

        private static void InitLanguage(ConfigManager configManager)
        {
            var langTag = configManager.GetConfig<ProgramConfig>().Language;
            if (string.IsNullOrEmpty(langTag))
            {
                return;
            }
            I18nHelper.SetProgramLanguage(langTag);
        }

        public void Run()
        {
            var configManager = Services.GetRequiredService<ConfigManager>();
            InitLanguage(configManager);

            var contextMenu = Services.GetRequiredService<IContextMenu>();
            var mainWindow = Services.GetRequiredService<IMainWindow>();

            contextMenu.AddMenuItem(new MenuItem(Strings.Settings, mainWindow.Show), "Top Group");
            contextMenu.AddMenuItemGroup(configManager.Menu);

            PrepareRemoteWorkingFolder();
            PrepareWorkingFolder(configManager);
            ServiceManager = Services.GetRequiredService<ServiceManager>();
            ServiceManager.StartUpAllService();

            InitTrayIcon();
            Services.GetRequiredService<AppInstance>().WaitForOtherInstanceToActiveAsync();
            contextMenu.AddMenuItemGroup(new MenuItem[] { new(Strings.Exit, mainWindow.ExitApp) });
            ShowMainWindow(configManager);
            CheckUpdate();
        }

        private void InitTrayIcon()
        {
            var trayIcon = Services.GetRequiredService<ITrayIcon>();
            trayIcon.MainWindowWakedUp += Services.GetRequiredService<IMainWindow>().Show;
            trayIcon.Create();
        }

        private void ShowMainWindow(ConfigManager configManager)
        {
            var config = configManager.GetConfig<ProgramConfig>();

            var mainWindow = Services.GetRequiredService<IMainWindow>();
            mainWindow.SetFont(config.Font);
            mainWindow.ChangeTheme(config.Theme);
            mainWindow.Init(config.HideWindowOnStartup);
        }

        private async void CheckUpdate()
        {
            var configManager = Services.GetRequiredService<ConfigManager>();
            var updateChecker = Services.GetRequiredService<UpdateChecker>();
            var notificationManager = Services.GetService<INotification>();
            if (notificationManager is null)
            {
                return;
            }

            bool checkOnStartup = configManager.GetConfig<ProgramConfig>().CheckUpdateOnStartUp;
            if (checkOnStartup)
            {
                try
                {
                    var (needUpdate, newVersion) = await updateChecker.Check();
                    if (needUpdate)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        notificationManager.SendText(
                            Strings.FoundNewVersion,
                            $"v{updateChecker.Version} -> {newVersion}",
                            new Button(Strings.OpenDownloadPage, () => Sys.OpenWithDefaultApp(updateChecker.ReleaseUrl))
                        );
                    }
                }
                catch
                {
                }
            }
        }

        public void Stop()
        {
            ServiceManager?.StopAllService();
            var disposable = Services as IDisposable;
            disposable?.Dispose();
        }

        public static void ConfigCommonService(IServiceCollection services)
        {
            services.AddSingleton((serviceProvider) => serviceProvider);
            services.AddSingleton<ConfigManager>();

            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IMessenger, WeakReferenceMessenger>();

            services.AddSingleton<IWebDav, WebDavClient>();
            services.AddSingleton<IHttp, Http>();
            services.AddSingleton<ServiceManager>();
            services.AddTransient<UpdateChecker>();

            services.AddTransient<AppInstance>();
        }

        public static void ConfigurateViewModels(IServiceCollection services)
        {
            services.AddTransient<SyncSettingViewModel>();
            services.AddTransient<SystemSettingViewModel>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<CliboardAssistantViewModel>();
            services.AddTransient<NextCloudLogInViewModel>();
            services.AddSingleton<ServiceStatusViewModel>();
            services.AddSingleton<MainViewModel>();
        }

        public static void ConfigurateUserService(IServiceCollection services)
        {
            services.AddSingleton<IService, EasyCopyImageSerivce>();
            services.AddSingleton<IService, ConvertService>();
            services.AddSingleton<IService, ServerService>();
            services.AddSingleton<IService, UploadService>();
            services.AddSingleton<IService, DownloadService>();
        }

        private async void PrepareRemoteWorkingFolder()
        {
            try
            {
                var webdav = Services.GetRequiredService<IWebDav>();
                var res = await webdav.TestAlive();
                if (res)
                {
                    await webdav.CreateDirectory(Env.RemoteFileFolder);
                }
            }
            catch { }
        }

        private static void PrepareWorkingFolder(ConfigManager configManager)
        {
            var config = configManager.GetConfig<ProgramConfig>();
            if (Directory.Exists(Env.TemplateFileFolder))
            {
                if (config.DeleteTempFilesOnStartUp)
                {
                    Directory.Delete(Env.TemplateFileFolder, true);
                    Directory.CreateDirectory(Env.TemplateFileFolder);
                }
            }
            else
            {
                Directory.CreateDirectory(Env.TemplateFileFolder);
            }

            var logFolder = new DirectoryInfo(Env.LogFolder);
            if (logFolder.Exists && config.LogRemainDays != 0)
            {
                var today = DateTime.Today;
                foreach (var logFile in logFolder.EnumerateFileSystemInfos("????????.txt"))
                {
                    var createTime = logFile.CreationTime.Date;
                    if ((today - createTime) > TimeSpan.FromDays(config.LogRemainDays))
                    {
                        logFile.Delete();
                    }
                }
            }
        }
    }
}