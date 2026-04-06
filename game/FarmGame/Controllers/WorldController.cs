// =============================================================================
// WorldController.cs — Core game world: map, player, camera, entities, AI, HUD
//
// Order: 500 (drawn above background)
// Owns MapManager (map loading/transitions) and ObjectManager (player/camera/entities).
// Absorbs ParticleController (damage numbers) and UIController (HUD/toast/menu).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using FontStashSharp;
using Serilog;
using FarmGame.Core;
using FarmGame.Camera;
using FarmGame.Data;
using FarmGame.Entities;
using FarmGame.Models;
using FarmGame.Screens.Panels;
using FarmGame.World;
using FarmGame.World.Interactions;

namespace FarmGame.Controllers;

// ─── State Definitions ──────────────────────────────────────

public class WorldLogicState
{
    public GameMap CurrentMap { get; set; }
    public Player Player { get; set; }
    public Camera2D Camera { get; set; }
    public float AutoSaveTimer { get; set; }
    public bool IsPaused { get; set; }
    public bool InputBlocked { get; set; }
    public bool IsLoading { get; set; }
    public int LoadingFrameCount { get; set; }
    public Action DeferredLoadAction { get; set; }
    public string MapName { get; set; }
    public GameTime CurrentGameTime { get; set; }
    // UI state
    public bool IsMenuOpen { get; set; }
    // Particle state
    public List<DamageNumber> DamageNumbers { get; set; } = new();
}

public class WorldRenderState
{
    public GameMap CurrentMap { get; set; }
    public Player Player { get; set; }
    public Camera2D Camera { get; set; }
    public bool IsLoading { get; set; }
    public string MapName { get; set; }
    // UI state
    public bool IsMenuOpen { get; set; }
    // Particle state
    public List<DamageNumber> DamageNumbers { get; set; } = new();
}

/// <summary>Floating damage number data.</summary>
public class DamageNumber
{
    public Vector2 WorldPosition { get; set; }
    public int Amount { get; set; }
    public bool IsCritical { get; set; }
    public float Timer { get; set; }
    public float Duration { get; set; } = 0.8f;
}

// ─── Controller ─────────────────────────────────────────────

public class WorldController : BaseController<WorldLogicState, WorldRenderState>,
    INotificationHandler<InputEvent>,
    INotificationHandler<DatabaseDisconnectedEvent>,
    INotificationHandler<DatabaseReconnectedEvent>,
    INotificationHandler<MapLoadedEvent>,
    INotificationHandler<DamageDealtEvent>,
    INotificationHandler<TogglePauseEvent>
{
    public override string Name => "World";
    public override int Order => 500;

    // ─── Managers ───────────────────────────────────────────

    public MapManager Maps { get; private set; }
    public ObjectManager Objects { get; private set; }

    // ─── External References (set during Load) ──────────────

    private GraphicsDevice _graphicsDevice;
    private Func<string, Texture2D> _loadTexture;
    private DataRegistry _registry;
    private GameSession _session;
    private QueueManager _queue;
    private ScreenManager _screenManager;

    // ─── UI Components (absorbed from UIController) ─────────

    private readonly MapTransitionOverlay _mapTransition = new();
    private readonly ToastAlert _toast = new();
    private readonly GameMenuPanel _gameMenu = new();

    /// <summary>Whether the in-game menu is currently open.</summary>
    public bool IsMenuOpen => _gameMenu.IsOpen;

    /// <summary>Set to true to block player keyboard input.</summary>
    public bool InputBlocked { set => LogicState.InputBlocked = value; }

    /// <summary>Fired when player selects "Leave Game" from the menu.</summary>
    public Action OnLeaveGame { get; set; }

    /// <summary>Fired when player selects "Settings" from the menu.</summary>
    public Action OnSettings { get; set; }

    // ─── Lifecycle ──────────────────────────────────────────

    public override void Initialize()
    {
        Maps = new MapManager();
        Objects = new ObjectManager();
        Log.Information("[WorldController] Initialized");
    }

    public override void Load(ControllerManager controllers)
    {
        var system = controllers.System;
        _session = system.Session;
        _queue = system.Queue;
        _screenManager = controllers.Background.Screen;

        // Build DataRegistry from ConfigManager
        _registry = system.Config.ToDataRegistry();

        // Create asset service (needs GraphicsDevice — set via SetGraphicsDevice)
        if (_graphicsDevice != null)
        {
            Maps.Configure(_registry, _loadTexture, _graphicsDevice, system.ContentDir);
        }

        // Wire game menu callbacks
        OnLeaveGame = () => controllers.Background.TransitionTo(GameState.TitleScreen);
        OnSettings = () => controllers.Background.TransitionTo(GameState.Settings);
        _gameMenu.OnLeaveGame = () => OnLeaveGame?.Invoke();
        _gameMenu.OnSettings = () => OnSettings?.Invoke();

        // Load effect definitions
        World.Effects.EffectRegistry.LoadDefinitions(system.ContentDir, _loadTexture);
    }

    /// <summary>Set graphics context for asset loading. Called from Game1 before Load.</summary>
    public void SetGraphicsContext(GraphicsDevice graphicsDevice, Func<string, Texture2D> loadTexture)
    {
        _graphicsDevice = graphicsDevice;
        _loadTexture = loadTexture;
    }

    public override void Shutdown()
    {
        SaveState();
        Log.Information("[WorldController] Shutdown");
    }

    // ─── Game Lifecycle ─────────────────────────────────────

    public void StartGame(PlayerState savedState)
    {
        LogicState.IsLoading = true;
        LogicState.LoadingFrameCount = 0;
        LogicState.DeferredLoadAction = () => LoadMap(savedState);
    }

    private void LoadMap(PlayerState savedState)
    {
        var result = Maps.LoadMap(savedState);
        LogicState.CurrentMap = result.Map;
        LogicState.Player = result.Player;
        LogicState.Camera = result.Camera;
        LogicState.AutoSaveTimer = 0f;
        LogicState.MapName = result.MapName;

        Objects.Player = result.Player;
        Objects.Camera = result.Camera;

        LogicState.Player.RestoreAttributes(savedState);
        _session.LoadOrCreateMapState(result.Map.MapId, result.Map, savedState);
        LogicState.Camera.SetWorldBounds(LogicState.CurrentMap);
        LogicState.CurrentMap.PlayerProxy = LogicState.Player.WorldProxy;

        LogicState.Player.OnInteract = obj =>
        {
            if (obj.InteractionBehavior is DialogueBehavior)
            {
                string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
            }
            else
            {
                string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
                MessageQueue.Enqueue(LocaleManager.Format("ui", "interact", name));
            }
        };

        MessageQueue.Enqueue(LocaleManager.Format("ui", "entered_map", result.MapName));
        _mapTransition.Start(result.MapName);
    }

    public void SaveState()
    {
        if (LogicState.Player == null || LogicState.CurrentMap == null) return;
        _session?.SavePlayer(LogicState.Player, LogicState.CurrentMap.MapId);
        _session?.SaveMapObjects(LogicState.CurrentMap);
    }

    // ─── Update Logic ───────────────────────────────────────

    public override void UpdateLogic(GameTime gameTime)
    {
        // Only run when in Playing state
        if (_screenManager != null && _screenManager.CurrentState != GameState.Playing)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // UI components update
        _mapTransition.Update(dt);
        _toast.Update(dt);
        _gameMenu.Update();
        LogicState.IsMenuOpen = _gameMenu.IsOpen;

        // Deferred loading
        if (LogicState.IsLoading)
        {
            LogicState.LoadingFrameCount++;
            if (LogicState.LoadingFrameCount == 2)
            {
                LogicState.DeferredLoadAction?.Invoke();
                LogicState.DeferredLoadAction = null;
            }
            else if (LogicState.LoadingFrameCount >= 3)
            {
                LogicState.IsLoading = false;
            }
            return;
        }

        if (LogicState.IsPaused || LogicState.CurrentMap == null) return;

        // Auto-save
        float interval = GameConstants.AutoSaveInterval;
        if (interval > 0f)
        {
            LogicState.AutoSaveTimer += dt;
            if (LogicState.AutoSaveTimer >= interval)
            {
                LogicState.AutoSaveTimer = 0f;
                SaveState();
                MessageQueue.Enqueue(LocaleManager.Get("ui", "auto_saved", "Auto-saved"));
            }
        }

        // Object manager update (player, map entities, camera)
        Objects.Update(LogicState.CurrentMap, dt, LogicState.CurrentGameTime, LogicState.InputBlocked);

        // Handle teleport
        if (LogicState.CurrentMap.PendingInteraction != null)
        {
            var req = LogicState.CurrentMap.PendingInteraction;
            LogicState.CurrentMap.PendingInteraction = null;
            HandleTeleport(req);
        }

        // Damage number particles
        UpdateDamageNumbers(dt);
    }

    private void UpdateDamageNumbers(float dt)
    {
        var numbers = LogicState.DamageNumbers;
        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            var dn = numbers[i];
            dn.Timer += dt;
            dn.WorldPosition = new Vector2(dn.WorldPosition.X, dn.WorldPosition.Y - 40f * dt);
            if (dn.Timer >= dn.Duration)
                numbers.RemoveAt(i);
        }
    }

    private void HandleTeleport(InteractionRequest req)
    {
        SaveState();
        _session.SetMapStateTtlOnLeave();

        LogicState.IsLoading = true;
        LogicState.LoadingFrameCount = 0;
        LogicState.DeferredLoadAction = () =>
        {
            var teleportState = new PlayerState
            {
                Uuid = "",
                PositionX = req.TargetX,
                PositionY = req.TargetY,
                FacingDirection = LogicState.Player.FacingDirection.ToString(),
                CurrentMap = req.TargetMap,
                CurrentMapStateId = null,
                MaxHp = LogicState.Player.MaxHp,
                CurrentHp = LogicState.Player.CurrentHp,
                Strength = LogicState.Player.Strength,
                Dexterity = LogicState.Player.Dexterity,
                WeaponAtk = LogicState.Player.WeaponAtk,
                BuffPercent = LogicState.Player.BuffPercent,
                CritRate = LogicState.Player.CritRate,
                CritDamage = LogicState.Player.CritDamage,
            };

            var result = Maps.LoadMap(teleportState);
            LogicState.CurrentMap = result.Map;
            LogicState.Player = result.Player;
            LogicState.Camera = result.Camera;
            LogicState.AutoSaveTimer = 0f;
            LogicState.MapName = result.MapName;

            Objects.Player = result.Player;
            Objects.Camera = result.Camera;

            LogicState.Player.RestoreAttributes(teleportState);
            _session.CurrentMapStateId = null;
            _session.LoadOrCreateMapState(result.Map.MapId, result.Map, null);
            LogicState.Camera.SetWorldBounds(LogicState.CurrentMap);
            LogicState.CurrentMap.PlayerProxy = LogicState.Player.WorldProxy;

            LogicState.Player.OnInteract = obj =>
            {
                string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
                MessageQueue.Enqueue(LocaleManager.Format("ui", "interact", name));
            };

            _mapTransition.Start(result.MapName);
        };
    }

    // ─── State Sync ─────────────────────────────────────────

    protected override void CopyState(WorldLogicState logic, WorldRenderState render)
    {
        render.CurrentMap = logic.CurrentMap;
        render.Player = logic.Player;
        render.Camera = logic.Camera;
        render.IsLoading = logic.IsLoading;
        render.MapName = logic.MapName;
        render.IsMenuOpen = logic.IsMenuOpen;

        // Deep-copy damage numbers
        render.DamageNumbers = new List<DamageNumber>(logic.DamageNumbers.Count);
        foreach (var dn in logic.DamageNumbers)
        {
            render.DamageNumbers.Add(new DamageNumber
            {
                WorldPosition = dn.WorldPosition,
                Amount = dn.Amount,
                IsCritical = dn.IsCritical,
                Timer = dn.Timer,
                Duration = dn.Duration
            });
        }
    }

    // ─── Draw ───────────────────────────────────────────────

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        if (_screenManager != null && _screenManager.CurrentState != GameState.Playing)
            return;

        if (RenderState.IsLoading)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            var font = FontManager.GetFont(28);
            if (font != null)
            {
                string text = LocaleManager.Get("ui", "loading", "Loading...");
                var size = font.MeasureString(text);
                float x = (GameConstants.ScreenWidth - size.X) / 2f;
                float y = (GameConstants.ScreenHeight - size.Y) / 2f;
                font.DrawText(spriteBatch, text, new Vector2(x, y), new Color(200, 220, 200));
            }
            spriteBatch.End();
            return;
        }

        if (RenderState.CurrentMap == null || RenderState.Camera == null) return;

        // World-space rendering (with camera transform)
        spriteBatch.Begin(
            transformMatrix: RenderState.Camera.TransformMatrix,
            samplerState: SamplerState.PointClamp);

        RenderState.CurrentMap.Draw(spriteBatch, RenderState.Camera);
        RenderState.Player?.Draw(spriteBatch);
        if (RenderState.Player != null)
            RenderState.CurrentMap.DrawObjectInfo(spriteBatch, RenderState.Player.GridPosition);

        // Damage numbers (world-space)
        foreach (var dn in RenderState.DamageNumbers)
        {
            float progress = dn.Timer / dn.Duration;
            float alpha = 1f - progress;
            int fontSize = dn.IsCritical ? 18 : 14;
            var dmgFont = FontManager.GetFont(fontSize);
            if (dmgFont == null) continue;
            string text = dn.IsCritical ? $"{dn.Amount}!" : dn.Amount.ToString();
            var color = dn.IsCritical ? Color.Yellow : Color.White;
            dmgFont.DrawText(spriteBatch, text, dn.WorldPosition, color * alpha);
        }

        spriteBatch.End();

        // Screen-space HUD (toast, map transition, game menu)
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _toast.Draw(spriteBatch);
        if (_mapTransition.IsActive)
            _mapTransition.Draw(spriteBatch);
        _gameMenu.Draw(spriteBatch);
        spriteBatch.End();
    }

    // ─── Event Handlers ─────────────────────────────────────

    public Task Handle(InputEvent notification, CancellationToken ct)
    {
        LogicState.CurrentGameTime = notification.GameTime;
        return Task.CompletedTask;
    }

    public Task Handle(DatabaseDisconnectedEvent notification, CancellationToken ct)
    {
        LogicState.IsPaused = true;
        return Task.CompletedTask;
    }

    public Task Handle(DatabaseReconnectedEvent notification, CancellationToken ct)
    {
        LogicState.IsPaused = false;
        return Task.CompletedTask;
    }

    public Task Handle(MapLoadedEvent notification, CancellationToken ct)
    {
        _mapTransition.Start(notification.MapName);
        return Task.CompletedTask;
    }

    public Task Handle(DamageDealtEvent notification, CancellationToken ct)
    {
        LogicState.DamageNumbers.Add(new DamageNumber
        {
            WorldPosition = notification.WorldPosition,
            Amount = notification.Damage,
            IsCritical = notification.IsCritical,
        });
        return Task.CompletedTask;
    }

    public Task Handle(TogglePauseEvent notification, CancellationToken ct)
    {
        _gameMenu.Toggle();
        return Task.CompletedTask;
    }
}
