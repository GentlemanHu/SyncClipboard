﻿using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SyncClipboard.Core.AbstractClasses;
using SyncClipboard.Core.ViewModels;
using System;
using System.Linq;

namespace SyncClipboard.Desktop.Views;

internal class TrayIconImpl : TrayIconBase<WindowIcon>
{
    private static readonly WindowIcon _DefaultIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://SyncClipboard.Desktop/Assets/default.ico")));
    private static readonly WindowIcon _ErrorIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://SyncClipboard.Desktop/Assets/erro.ico")));
    private static readonly WindowIcon _DefaultInactiveIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://SyncClipboard.Desktop/Assets/default-inactive.ico")));
    private static readonly WindowIcon _ErrorInactiveIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://SyncClipboard.Desktop/Assets/erro-inactive.ico")));

    protected override WindowIcon DefaultIcon => _DefaultIcon;
    protected override WindowIcon ErrorIcon => _ErrorIcon;
    protected override WindowIcon DefaultInactiveIcon => _DefaultInactiveIcon;
    protected override WindowIcon ErrorInactiveIcon => _ErrorInactiveIcon;
    protected override int MaxToolTipLenth => 255;

    private readonly ServiceStatusViewModel _serviceStatusViewModel;
    protected override ServiceStatusViewModel? ServiceStatusViewModel => _serviceStatusViewModel;

    public override event Action? MainWindowWakedUp;

    private readonly TrayIcon _trayIcon;

    public TrayIconImpl(ServiceStatusViewModel serviceStatusViewModel)
    {
        var icons = TrayIcon.GetIcons(App.Current);
        var trayIcon = icons?[0];
        ArgumentNullException.ThrowIfNull(trayIcon, nameof(trayIcon));
        _trayIcon = trayIcon;
        _trayIcon.Command = new RelayCommand(() => MainWindowWakedUp?.Invoke());
        _trayIcon.ToolTipText = string.Empty;
        _serviceStatusViewModel = serviceStatusViewModel;
    }

    public override void Create()
    {
    }

    protected override void SetIcon(WindowIcon icon)
    {
        ActionInUIThread(() => _trayIcon.Icon = icon);
    }

    protected override void SetToolTip(string text)
    {
        ActionInUIThread(() => _trayIcon.ToolTipText = text);
    }

    private static void ActionInUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action.Invoke();
        }
        else
        {
            Dispatcher.UIThread.Post(action, DispatcherPriority.Send);
        }
    }

    protected override WindowIcon[] UploadIcons()
    {
        return Enumerable.Range(1, 17)
            .Select(x => $"avares://SyncClipboard.Desktop/Assets/upload{x:d3}.ico")
            .Select(x => new WindowIcon(AssetLoader.Open(new Uri(x))))
            .ToArray();
    }

    protected override WindowIcon[] DownloadIcons()
    {
        return Enumerable.Range(1, 17)
            .Select(x => $"avares://SyncClipboard.Desktop/Assets/download{x:d3}.ico")
            .Select(x => new WindowIcon(AssetLoader.Open(new Uri(x))))
            .ToArray();
    }
}
