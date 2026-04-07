// =============================================================================
// BackgroundController.cs — Background rendering, audio, and screen management
//
// Order: 100 (drawn behind world and UI)
// Owns AudioManager (BGM/SE) and ScreenManager (scene switching).
// Draws gradient background. Manages screen lifecycle for title, settings,
// loading, and gameplay screens.
// =============================================================================

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Serilog;
using FarmGame.Core;
using FarmGame.Core.Managers;
using FarmGame.Views;
using FarmGame.Views.HUD;

namespace FarmGame.Controllers;

public class BackgroundLogicState
{
    public float ScrollOffset { get; set; }
    public Color TopColor { get; set; } = new Color(20, 40, 20);
    public Color BottomColor { get; set; } = new Color(10, 25, 10);
    public ViewTransition PendingTransition { get; set; }
}

public class BackgroundRenderState
{
    public float ScrollOffset { get; set; }
    public Color TopColor { get; set; } = new Color(20, 40, 20);
    public Color BottomColor { get; set; } = new Color(10, 25, 10);
    public GameState CurrentScreen { get; set; }
}

public class BackgroundController : BaseController<BackgroundLogicState, BackgroundRenderState>
{
    private const float ScrollSpeed = 8f;

    public override string Name => "Background";
    public override int Order => 100;

    // ─── Managers ───────────────────────────────────────────

    public AudioManager Audio { get; private set; }
    public ScreenManager Screen { get; private set; }

    // ─── Screen Instances (managed by ScreenManager) ────────

    private IView _activeScreen;
    private readonly System.Collections.Generic.Dictionary<GameState, IView> _screens = new();

    /// <summary>Callback when screen transition requests game exit.</summary>
    public Action OnExitGame { get; set; }

    /// <summary>Callback when "Start Game" is triggered from title screen.</summary>
    public Action OnStartGame { get; set; }

    // ─── Lifecycle ──────────────────────────────────────────

    public override void Initialize()
    {
        Audio = new AudioManager();
        Audio.Initialize();

        Screen = new ScreenManager();
        Log.Information("[BackgroundController] Initialized");
    }

    public override void Load(ControllerManager controllers)
    {
        var session = controllers.System.Session;
        var localesDir = controllers.System.LocalesDir;

        // Create and register screens
        var mainView = new MainView();
        mainView.OnStartGame = () => OnStartGame?.Invoke();
        mainView.HasSavedState = session?.HasSavedState ?? false;
        mainView.Initialize();

        var settingsView = new SettingsView();
        settingsView.HasSavedState = () => session?.HasSavedState ?? false;
        settingsView.OnLanguageChanged = (lang) =>
        {
            session?.ChangeLanguage(lang, localesDir);
            // Rebuild all views to reflect the new language
            foreach (var view in _screens.Values)
                view.Rebuild();
        };
        settingsView.OnDeleteCharacter = () =>
        {
            session?.DeleteAndReset();
            mainView.HasSavedState = false;
            TransitionTo(GameState.TitleScreen);
        };
        settingsView.Initialize();

        var loadingView = new LoadingView();
        loadingView.Initialize();

        RegisterScreen(GameState.TitleScreen, mainView);
        RegisterScreen(GameState.Settings, settingsView);
        RegisterScreen(GameState.Loading, loadingView);

        // Wire callbacks
        OnStartGame = () =>
        {
            var savedState = session?.LoadPlayer();
            controllers.World.StartGame(savedState);
            TransitionTo(GameState.Playing);
        };

        // Start on title screen
        InitializeScreen(GameState.TitleScreen);
    }

    /// <summary>Register a screen for a game state.</summary>
    public void RegisterScreen(GameState state, IView screen)
    {
        _screens[state] = screen;
    }

    /// <summary>Transition to a game state, activating its screen.</summary>
    public void TransitionTo(GameState target)
    {
        var from = Screen.CurrentState;

        // Exit old screen
        _activeScreen?.OnExit(target);

        Screen.TransitionTo(target);

        // Enter new screen
        if (_screens.TryGetValue(target, out var screen))
        {
            _activeScreen = screen;
            _activeScreen.OnEnter(from);
        }
        else
        {
            _activeScreen = null;
        }

        Log.Information("[BackgroundController] Screen: {From} → {To}", from, target);
    }

    /// <summary>Initialize the first screen.</summary>
    public void InitializeScreen(GameState initialState)
    {
        Screen.TransitionTo(initialState);
        if (_screens.TryGetValue(initialState, out var screen))
        {
            _activeScreen = screen;
            _activeScreen.OnEnter(initialState);
        }
    }

    public override void Shutdown()
    {
        Audio?.Shutdown();
        Log.Information("[BackgroundController] Shutdown");
    }

    // ─── Update ─────────────────────────────────────────────

    public override void UpdateLogic(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Gradient scroll
        LogicState.ScrollOffset += ScrollSpeed * dt;
        if (LogicState.ScrollOffset > 10000f)
            LogicState.ScrollOffset -= 10000f;

        // Audio
        Audio?.Update(dt);

        // Active screen update
        if (_activeScreen != null)
        {
            var transition = _activeScreen.Update(gameTime);
            if (transition != null && transition != ViewTransition.None)
                LogicState.PendingTransition = transition;
        }
    }

    protected override void CopyState(BackgroundLogicState logic, BackgroundRenderState render)
    {
        render.ScrollOffset = logic.ScrollOffset;
        render.TopColor = logic.TopColor;
        render.BottomColor = logic.BottomColor;
        render.CurrentScreen = Screen.CurrentState;

        // Process pending transitions on main thread
        if (logic.PendingTransition != null)
        {
            var t = logic.PendingTransition;
            logic.PendingTransition = null;

            Log.Information("[BackgroundController] CopyState transition: Exit={Exit}, Target={Target}",
                t.Exit, t.Target);

            if (t.Exit)
                OnExitGame?.Invoke();
            else if (t.Target.HasValue)
                TransitionTo(t.Target.Value);
        }
    }

    // ─── Draw ───────────────────────────────────────────────

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        // Gradient background
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        const int bandCount = 16;
        int bandHeight = screenH / bandCount + 1;
        for (int i = 0; i < bandCount; i++)
        {
            float t = (float)i / (bandCount - 1);
            var color = Color.Lerp(RenderState.TopColor, RenderState.BottomColor, t);
            int y = i * (screenH / bandCount);
            spriteBatch.FillRectangle(new Rectangle(0, y, screenW, bandHeight), color);
        }
        spriteBatch.End();

        // Active screen draw (title, settings, loading — but NOT gameplay world)
        if (_activeScreen != null && RenderState.CurrentScreen != GameState.Playing)
        {
            _activeScreen.Draw(spriteBatch);
        }
    }
}
