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
    public LoadingScreen LoadingScreen { get; private set; }
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
        settingsScreen.OnDeleteCharacter = () =>
        {
            var dbPath = DatabasePathResolver.GetDatabasePath(GameConstants.GameTitle);
            LoadingScreen.Configure(() =>
            {
                DatabaseBackup.Backup(dbPath);
                if (File.Exists(dbPath))
                    File.Delete(dbPath);

                // Re-initialize database with fresh state
                var freshDb = DatabaseInitializer.Run();
                if (freshDb.Success)
                    StateSaver = new PlayerStateSaver(freshDb.PlayerStateRepo, freshDb.PlayerUuid);
                else
                    StateSaver = null;

                _initialSavedState = null;
                titleScreen.HasSavedState = false;
                Log.Information("Character deleted, database re-initialized");
            }, GameState.TitleScreen);
        };
        settingsScreen.Initialize();

        PlayingScreen = new PlayingScreen(game.GraphicsDevice, registry, loadTexture);

        LoadingScreen = new LoadingScreen();
        LoadingScreen.Initialize();

        ScreenManager.Register(GameState.TitleScreen, titleScreen);
        ScreenManager.Register(GameState.Settings, settingsScreen);
        ScreenManager.Register(GameState.Paused, pauseScreen);
        ScreenManager.Register(GameState.Playing, PlayingScreen);
        ScreenManager.Register(GameState.Loading, LoadingScreen);

        Log.Information("[Init] All initialization complete");
    }

    public PlayerState LoadSavedState()
    {
        return StateSaver?.Load() ?? _initialSavedState;
    }
}
