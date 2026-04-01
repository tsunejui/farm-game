// =============================================================================
// InitManager.cs — Fluent game initialization pipeline
//
// Usage (Method Chaining / Fluent Interface):
//   _init
//     .WithConfig(contentDir)
//     .WithDatabase()
//     .WithLocale()
//     .WithControllers(controllerManager)
//     .Bootstrap();
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
using FarmGame.Services;

namespace FarmGame.Core;

public class InitManager
{
    public SpriteBatch SpriteBatch { get; private set; }
    public ScreenManager ScreenManager { get; private set; }
    public GameSession Session { get; private set; }
    public PlayingScreen PlayingScreen { get; private set; }
    public DataRegistry Registry { get; private set; }

    private string _contentDir;
    private string _databaseError;
    private ControllerManager _controllerManager;

    // ─── Fluent Chain Methods ───────────────────────────────

    /// <summary>Step 1: Load config.yaml and apply game constants.</summary>
    public InitManager WithConfig(string contentDir)
    {
        _contentDir = contentDir;
        ConfigInitializer.Run(contentDir);
        return this;
    }

    /// <summary>Step 2: Initialize SQLite database, migrations, player UUID.</summary>
    public InitManager WithDatabase()
    {
        var dbResult = DatabaseInitializer.Run();
        if (dbResult.Success)
            Session = new GameSession(dbResult);
        else
            _databaseError = dbResult.Error;
        return this;
    }

    /// <summary>Step 3: Load locale files based on saved language preference.</summary>
    public InitManager WithLocale()
    {
        if (Session != null)
        {
            var dbResult = DatabaseInitializer.GetCachedResult();
            LocaleInitializer.Run(_contentDir, dbResult?.Settings);
        }
        else
        {
            LocaleInitializer.Run(_contentDir, null);
        }
        return this;
    }

    /// <summary>Step 4: Register the ControllerManager for later configuration.</summary>
    public InitManager WithControllers(ControllerManager controllerManager)
    {
        _controllerManager = controllerManager;
        return this;
    }

    /// <summary>
    /// Final step: execute all deferred initialization.
    /// Must be called after the fluent chain is complete.
    /// </summary>
    public InitManager Bootstrap()
    {
        ScreenManager = new ScreenManager();
        Log.Information("[Init] Bootstrap complete");
        return this;
    }

    // ─── Content Loading (called from LoadContent) ──────────

    public void LoadContent(
        Game game,
        GraphicsDeviceManager graphics,
        string contentDir,
        Action startGameCallback,
        IAssetService assets,
        QueueManager queue)
    {
        // Graphics + Font + Myra
        SpriteBatch = GraphicsInitializer.Run(game, graphics, contentDir);

        // Data Registry + Effects
        Registry = DataInitializer.Run(contentDir);
        FarmGame.World.Effects.EffectRegistry.LoadDefinitions(contentDir, assets.LoadTexture);

        // Screens
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
            if (ScreenManager.TryGet(GameState.Loading, out var screen))
            {
                var loadingScreen = (LoadingScreen)screen;
                loadingScreen.Configure(() =>
                {
                    Session?.DeleteAndReset();
                    titleScreen.HasSavedState = false;
                }, GameState.TitleScreen);
            }
        };
        settingsScreen.Initialize();

        var playingScreen = new PlayingScreen(game.GraphicsDevice, Registry, assets.LoadTexture, Session, contentDir);
        PlayingScreen = playingScreen;

        var loading = new LoadingScreen();
        loading.Initialize();

        ScreenManager.Register(GameState.TitleScreen, titleScreen);
        ScreenManager.Register(GameState.Settings, settingsScreen);
        ScreenManager.Register(GameState.Playing, playingScreen);
        ScreenManager.Register(GameState.Loading, loading);

        // Configure controllers (QueueManager passed in, controllers subscribe to queues)
        _controllerManager?.ConfigureAll(assets, Registry, Session, queue);

        Log.Information("[Init] All content loaded");
    }
}
