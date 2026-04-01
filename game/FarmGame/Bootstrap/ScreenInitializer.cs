using Serilog;
using FarmGame.Screens;
using FarmGame.Screens.Panels;

namespace FarmGame.Bootstrap;

public class ScreenInitResult
{
    public TitleScreen TitleScreen { get; init; }
    public SettingsScreen SettingsScreen { get; init; }
    public MapTransitionOverlay MapTransition { get; init; }
    public ToastAlert Toast { get; init; }
}

public static class ScreenInitializer
{
    public static ScreenInitResult Run(string databaseError)
    {
        var titleScreen = new TitleScreen();
        titleScreen.Initialize();
        if (!string.IsNullOrEmpty(databaseError))
            titleScreen.SetError(databaseError);

        var settingsScreen = new SettingsScreen();
        settingsScreen.Initialize();

        var mapTransition = new MapTransitionOverlay();
        var toast = new ToastAlert();

        Log.Information("[Init] Screens initialized");

        return new ScreenInitResult
        {
            TitleScreen = titleScreen,
            SettingsScreen = settingsScreen,
            MapTransition = mapTransition,
            Toast = toast,
        };
    }
}
