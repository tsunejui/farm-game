// =============================================================================
// WorldController.cs — Core game world: map, player, camera, entities, AI
//
// Order: 100 (drawn above background, below particles/UI)
// Manages GameMap, Player, Camera2D. Handles physics, AI, collision.
// Subscribes to InputEvent for player control.
// Pauses on DatabaseDisconnectedEvent / GamePausedEvent.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using Serilog;
using FarmGame.Core;
using FarmGame.Queues;
using FarmGame.Queues.Events;
using FarmGame.Bootstrap;
using FarmGame.Camera;
using FarmGame.Data;
using FarmGame.Entities;
using FarmGame.Models;
using FarmGame.Services;
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
    public bool IsLoading { get; set; }
    public int LoadingFrameCount { get; set; }
    public Action DeferredLoadAction { get; set; }
    public string MapName { get; set; }
    public KeyboardStateExtended CurrentKeyboard { get; set; }
    public GameTime CurrentGameTime { get; set; }
}

public class WorldRenderState
{
    public GameMap CurrentMap { get; set; }
    public Player Player { get; set; }
    public Camera2D Camera { get; set; }
    public bool IsLoading { get; set; }
    public string MapName { get; set; }
}

// ─── Controller ─────────────────────────────────────────────

public class WorldController : BaseController<WorldLogicState, WorldRenderState>,
    INotificationHandler<InputEvent>,
    INotificationHandler<DatabaseDisconnectedEvent>,
    INotificationHandler<DatabaseReconnectedEvent>,
    INotificationHandler<GamePausedEvent>
{
    public override string Name => "World";
    public override int Order => 100;

    private readonly IAssetService _assets;
    private readonly DataRegistry _registry;
    private readonly GameSession _session;
    private readonly QueueManager _queue;

    public WorldController(
        IAssetService assets,
        DataRegistry registry,
        GameSession session,
        QueueManager queue)
    {
        // (assets injected via constructor)
        _assets = assets;
        _registry = registry;
        _session = session;
        _queue = queue;
    }

    public override void Subscribe(QueueManager queue) { }
    public override void LoadResource(GraphicsDevice graphicsDevice, string contentDir) { }

    // ─── Game Lifecycle ─────────────────────────────────────

    public void StartGame(PlayerState savedState)
    {
        LogicState.IsLoading = true;
        LogicState.LoadingFrameCount = 0;
        LogicState.DeferredLoadAction = () => LoadMap(savedState);
    }

    private void LoadMap(PlayerState savedState)
    {
        var result = GameplayInitializer.Run(savedState, _registry, _assets.LoadTexture, _assets.GraphicsDevice, _assets.ContentDir);
        LogicState.CurrentMap = result.Map;
        LogicState.Player = result.Player;
        LogicState.Camera = result.Camera;
        LogicState.AutoSaveTimer = 0f;
        LogicState.MapName = result.MapName;

        LogicState.Player.RestoreAttributes(savedState);
        _session.LoadOrCreateMapState(result.Map.MapId, result.Map, savedState);
        LogicState.Camera.SetWorldBounds(LogicState.CurrentMap);
        LogicState.CurrentMap.PlayerProxy = LogicState.Player.WorldProxy;

        LogicState.Player.OnInteract = obj =>
        {
            if (obj.InteractionBehavior is DialogueBehavior db)
            {
                string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
                // TODO: publish dialogue event
            }
            else
            {
                string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
                MessageQueue.Enqueue(LocaleManager.Format("ui", "interact", name));
            }
        };

        MessageQueue.Enqueue(LocaleManager.Format("ui", "entered_map", result.MapName));
    }

    public void SaveState()
    {
        if (LogicState.Player == null || LogicState.CurrentMap == null) return;
        _session.SavePlayer(LogicState.Player, LogicState.CurrentMap.MapId);
        _session.SaveMapObjects(LogicState.CurrentMap);
    }

    // ─── Update Logic ───────────────────────────────────────

    public override void UpdateLogic(GameTime gameTime)
    {
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

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

        // Player update
        if (LogicState.Player != null && LogicState.CurrentGameTime != null)
            LogicState.Player.Update(LogicState.CurrentGameTime);

        // Map update (entities, AI, effects)
        LogicState.CurrentMap.Update(dt);

        // Camera follow player
        if (LogicState.Camera != null && LogicState.Player != null)
            LogicState.Camera.Update(LogicState.Player);

        // Handle teleport
        if (LogicState.CurrentMap.PendingInteraction != null)
        {
            var req = LogicState.CurrentMap.PendingInteraction;
            LogicState.CurrentMap.PendingInteraction = null;
            HandleTeleport(req);
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

            var result = GameplayInitializer.Run(teleportState, _registry, _assets.LoadTexture, _assets.GraphicsDevice, _assets.ContentDir);
            LogicState.CurrentMap = result.Map;
            LogicState.Player = result.Player;
            LogicState.Camera = result.Camera;
            LogicState.AutoSaveTimer = 0f;
            LogicState.MapName = result.MapName;

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
        };
    }

    // ─── State Sync ─────────────────────────────────────────

    protected override void CopyState(WorldLogicState logic, WorldRenderState render)
    {
        // Share references (GameMap/Player/Camera are thread-confined to main thread anyway)
        render.CurrentMap = logic.CurrentMap;
        render.Player = logic.Player;
        render.Camera = logic.Camera;
        render.IsLoading = logic.IsLoading;
        render.MapName = logic.MapName;
    }

    // ─── Draw ───────────────────────────────────────────────

    public override void DrawRender(SpriteBatch spriteBatch)
    {
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
                font.DrawText(spriteBatch, text, new Microsoft.Xna.Framework.Vector2(x, y),
                    new Color(200, 220, 200));
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
        Log.Warning("WorldController paused: {Reason}", notification.Reason);
        return Task.CompletedTask;
    }

    public Task Handle(DatabaseReconnectedEvent notification, CancellationToken ct)
    {
        LogicState.IsPaused = false;
        Log.Information("WorldController resumed");
        return Task.CompletedTask;
    }

    public Task Handle(GamePausedEvent notification, CancellationToken ct)
    {
        LogicState.IsPaused = notification.IsPaused;
        return Task.CompletedTask;
    }
}
