using CloudStatus.Core;
using CloudStatus.Device;
using CloudStatus.Native;
using CloudStatus.Persistence;
using CloudStatus.Ui;
using CloudStatus.Ui.Settings;

namespace CloudStatus;

internal enum ApplySettingsResult
{
    Applied,
    /// <summary>Nothing was applied: the previous settings stay in use.</summary>
    NotSaved,
    /// <summary>Settings saved and applied, but the Run entry could not be changed.</summary>
    StartupNotChanged,
}

/// <summary>Application lifetime and the use cases that span several components.</summary>
internal static class App
{
    static bool s_isShutDown;
    static LowBatteryAlert s_lowBattery;

    public static int Run(bool showSettings)
    {
        // The only registry reads until the user saves (spec 007).
        Config.Current = ConfigStore.Load();
        StartupRegistration.Initialize();
        Strings.Apply(Config.Current.Language);

        if (!MainWindow.Create())
            return 1;

        TrayIcon.Create(MainWindow.Handle);
        HeadsetLink.Initialize(MainWindow.Handle);
        HeadsetLink.Changed += OnHeadsetChanged;
        DongleWatcher.Changed += OnDongleChanged;
        DongleWatcher.Start(MainWindow.Handle); // after the icon, so a slow search never delays it
        if (showSettings)
            SettingsWindow.Show();
        return MessageLoop.Run() ? 0 : 1;
    }

    public static void Exit() => MainWindow.Close();

    /// <summary>Saves and applies new settings. If they cannot be saved, nothing changes.</summary>
    public static ApplySettingsResult ApplySettings(in Config next, bool startWithWindows)
    {
        if (!ConfigStore.Save(next))
            return ApplySettingsResult.NotSaved;
        bool startupChanged = StartupRegistration.TrySet(startWithWindows);

        Config previous = Config.Current;
        Config.Current = next;
        if (next.Language != previous.Language)
            Strings.Apply(next.Language);
        if (next.PollMinutes != previous.PollMinutes)
            HeadsetLink.OnPollIntervalChanged();
        TrayIcon.OnSettingsChanged();
        EvaluateLowBattery();

        return startupChanged ? ApplySettingsResult.Applied : ApplySettingsResult.StartupNotChanged;
    }

    static void OnHeadsetChanged()
    {
        TrayIcon.Refresh();
        EvaluateLowBattery();
    }

    static void EvaluateLowBattery()
    {
        Config config = Config.Current;
        BatteryNotice notice = s_lowBattery.Evaluate(HeadsetLink.Power, HeadsetLink.BatteryPercent, HeadsetLink.Charge,
            HeadsetLink.IsChargeKnown, config.LowBatteryPercent, config.LowBatteryNotice);
        TrayIcon.ShowNotice(notice, HeadsetLink.BatteryPercent);
    }

    static void OnDongleChanged()
    {
        if (DongleWatcher.IsConnected)
            HeadsetLink.Connect(DongleWatcher.Selected);
        else
            HeadsetLink.Disconnect();
        TrayIcon.Refresh();
    }

    public static void OnTrayEvent(TrayEvent trayEvent, POINT anchor)
    {
        switch (trayEvent)
        {
            case TrayEvent.Activated:
                SettingsWindow.Show();
                break;

            case TrayEvent.ContextMenuRequested:
                switch (TrayMenu.Show(MainWindow.Handle, anchor))
                {
                    case TrayCommand.Settings:
                        SettingsWindow.Show();
                        break;
                    case TrayCommand.Exit:
                        Exit();
                        break;
                }
                break;
        }
    }

    /// <summary>Releases every resource. Idempotent.</summary>
    public static void Shutdown()
    {
        if (s_isShutDown)
            return;
        s_isShutDown = true;

        SettingsWindow.Close();
        HeadsetLink.Disconnect();
        DongleWatcher.Stop();
        TrayIcon.Remove();
    }
}
