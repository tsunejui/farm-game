// =============================================================================
// InitManager.cs — Manages all game initialization and holds results
// =============================================================================

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Bootstrap;
using FarmGame.Data;
using FarmGame.Persistence;
using FarmGame.Persistence.Models;
using FarmGame.Screens;

namespace FarmGame.Core;

public class InitManager
{
    public SpriteBatch SpriteBatch { get; private set; }
    public ScreenManager ScreenManager { get; private set; }
    public PlayingScreen PlayingScreen { get; private set; }
    public PlayerStateSaver StateSaver { get; private set; }

    private string _databaseError;
    private PlayerState _initialSavedState;

    public void InitializeCore(string contentDir)
    {
        // 1. Config
        ConfigInitializer.Run(contentDir);

        // 2. Database
        var dbResult = DatabaseInitializer.Run();
        if (dbResult.Success)
        {
            StateSaver = new PlayerStateSaver(dbResult.PlayerStateRepo, dbResult.PlayerUuid);
            _initialSavedState = dbResult.SavedState;

            // 3. Locale
            LocaleInitializer.Run(contentDir, dbResult.Settings);

            // 4. ScreenManager (needs contentDir and settings for ChangeLanguage)
            ScreenManager = new ScreenManager(contentDir, dbResult.Settings);
        }
        else
        {
            _databaseError = dbResult.Error;

            // 3. Locale (without settings)
            LocaleInitializer.Run(contentDir, null);

            // 4. ScreenManager (without settings)
            ScreenManager = new ScreenManager(contentDir, null);
        }
    }

    public void LoadContent(
        Game game,
        GraphicsDeviceManager graphics,
        string contentDir,
        Action startGameCallback,
        Func<string, Texture2D> loadTexture)
    {
        // 5. Graphics + Font + Myra
        SpriteBatch = GraphicsInitializer.Run(game, graphics, contentDir);

        // 6. Data Registry
        var registry = DataInitializer.Run(contentDir);

        // 7. Screens
        var titleScreen = new TitleScreen();
        titleScreen.OnStartGame = startGameCallback;
        titleScreen.HasSavedState = _initialSavedState != null;
        titleScreen.Initialize();
        if (!string.IsNullOrEmpty(_databaseError))
            titleScreen.SetError(_databaseError);

        var pauseScreen = new PauseScreen();
        pauseScreen.Initialize();

        var settingsScreen = new SettingsScreen();
        settingsScreen.OnLanguageChanged = ScreenManager.ChangeLanguage;
        settingsScreen.Initialize();

        PlayingScreen = new PlayingScreen(game.GraphicsDevice, registry, loadTexture);

        ScreenManager.Register(GameState.TitleScreen, titleScreen);
        ScreenManager.Register(GameState.Settings, settingsScreen);
        ScreenManager.Register(GameState.Paused, pauseScreen);
        ScreenManager.Register(GameState.Playing, PlayingScreen);

        Log.Information("[Init] All initialization complete");
    }

    public PlayerState LoadSavedState()
    {
        return StateSaver?.Load() ?? _initialSavedState;
    }
}
