// =============================================================================
// InitManager.cs — Manages all game initialization and holds results
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Bootstrap;
using FarmGame.Data;
using FarmGame.Screens;
using FarmGame.Screens.Components;
using FarmGame.Screens.HUD;
namespace FarmGame.Core;

public class InitManager
{
    public SpriteBatch SpriteBatch { get; private set; }
    public ScreenManager ScreenManager { get; private set; }
    public GameSession Session { get; private set; }
    public PlayingScreen PlayingScreen { get; private set; }

    private string _contentDir;
    private string _databaseError;

    public void InitializeCore(string contentDir)
    {
        _contentDir = contentDir;

        // 1. Config
        ConfigInitializer.Run(contentDir);

        // 2. Database
        var dbResult = DatabaseInitializer.Run();
        if (dbResult.Success)
        {
            Session = new GameSession(dbResult);
            LocaleInitializer.Run(contentDir, dbResult.Settings);
        }
        else
        {
            _databaseError = dbResult.Error;
            LocaleInitializer.Run(contentDir, null);
        }

        // 3. ScreenManager
        ScreenManager = new ScreenManager();
    }

    public void LoadContent(
        Game game,
        GraphicsDeviceManager graphics,
        string contentDir,
        Action startGameCallback,
        Func<string, Texture2D> loadTexture)
    {
        // 4. Graphics + Font + Myra
        SpriteBatch = GraphicsInitializer.Run(game, graphics, contentDir);

        // 5. Data Registry + Effect definitions
        var registry = DataInitializer.Run(contentDir);
        FarmGame.World.Effects.EffectRegistry.LoadDefinitions(contentDir, loadTexture);

        // 6. Screens
        var titleScreen = new TitleScreen();
        titleScreen.OnStartGame = startGameCallback;
        titleScreen.HasSavedState = Session?.HasSavedState ?? false;
        titleScreen.Initialize();
        if (!string.IsNullOrEmpty(_databaseError))
            titleScreen.SetError(_databaseError);

        var settingsScreen = new SettingsScreen();
        settingsScreen.HasSavedState = () => Session?.HasSavedState ?? false;
        settingsScreen.OnLanguageChanged = (lang) =>
        {
            Session?.ChangeLanguage(lang, _contentDir);
            ScreenManager.RebuildAll();
        };
        settingsScreen.OnDeleteCharacter = () =>
        {
            var loadingScreen = (LoadingScreen)null;
            if (ScreenManager.TryGet(GameState.Loading, out var screen))
                loadingScreen = (LoadingScreen)screen;

            loadingScreen?.Configure(() =>
            {
                Session?.DeleteAndReset();
                titleScreen.HasSavedState = false;
            }, GameState.TitleScreen);
        };
        settingsScreen.Initialize();

        var playingScreen = new PlayingScreen(game.GraphicsDevice, registry, loadTexture, Session, contentDir);
        PlayingScreen = playingScreen;

        var loading = new LoadingScreen();
        loading.Initialize();

        ScreenManager.Register(GameState.TitleScreen, titleScreen);
        ScreenManager.Register(GameState.Settings, settingsScreen);
        ScreenManager.Register(GameState.Playing, playingScreen);
        ScreenManager.Register(GameState.Loading, loading);

        Log.Information("[Init] All initialization complete");
    }
}
