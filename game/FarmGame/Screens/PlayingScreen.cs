// =============================================================================
// PlayingScreen.cs — Active gameplay screen (map, player, camera, HUD)
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FarmGame.Bootstrap;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Data;
using FarmGame.Models;
using FarmGame.Screens.HUD;
using FarmGame.World;
using FarmGame.World.Interactions;
using Serilog;

namespace FarmGame.Screens;

public class PlayingScreen : IScreen, IWorldRenderer
{
    private readonly Func<string, Texture2D> _loadTexture;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly DataRegistry _registry;
    private readonly GameSession _session;
    private readonly MapTransitionOverlay _mapTransition;
    private readonly ToastAlert _toast;
    private readonly ObjectInspector _inspector;
    private readonly DialogueOverlay _dialogue;

    private GameMap _currentMap;
    private Entities.Player _player;
    private Camera2D _camera;
    private float _autoSaveTimer;

    private bool _isLoading;
    private int _loadingFrameCount;
    private Action _deferredLoadAction;

    private readonly string _contentDir;

    public PlayingScreen(
        GraphicsDevice graphicsDevice,
        DataRegistry registry,
        Func<string, Texture2D> loadTexture,
        GameSession session,
        string contentDir)
    {
        _graphicsDevice = graphicsDevice;
        _registry = registry;
        _loadTexture = loadTexture;
        _session = session;
        _contentDir = contentDir;
        _mapTransition = new MapTransitionOverlay();
        _toast = new ToastAlert();
        _inspector = new ObjectInspector();
        _dialogue = new DialogueOverlay();
    }

    public void Initialize() { }
    public void Rebuild() { }
    public void OnEnter(GameState fromState) { }

    public void OnExit(GameState toState)
    {
        SaveState();
    }

    public void StartGame(PlayerState savedState)
    {
        _isLoading = true;
        _loadingFrameCount = 0;
        _deferredLoadAction = () =>
        {
            var result = GameplayInitializer.Run(savedState, _registry, _loadTexture, _graphicsDevice, _contentDir);
            _currentMap = result.Map;
            _player = result.Player;
            _camera = result.Camera;
            _autoSaveTimer = 0f;

            // Restore player attributes from saved state
            _player.RestoreAttributes(savedState);

            // Load or create persisted map entity state (HP, alive/dead)
            _session.LoadOrCreateMapState(result.Map.MapId, result.Map, savedState);

            // Set world bounds once at map load (not per-frame)
            _camera.SetWorldBounds(_currentMap);

            // Register player as inspectable object and effect target
            _inspector.SetPlayerObject(_player.WorldProxy);
            _currentMap.PlayerProxy = _player.WorldProxy;

            // Wire interaction callback for interactable objects
            _player.OnInteract = obj =>
            {
                if (obj.InteractionBehavior is DialogueBehavior db)
                {
                    string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
                    _dialogue.Open(name, db.GetLines());
                }
                else
                {
                    string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
                    MessageQueue.Enqueue(LocaleManager.Format("ui", "interact", name));
                }
            };

            _mapTransition.Start(result.MapName);
            MessageQueue.Enqueue(LocaleManager.Format("ui", "entered_map", result.MapName));
        };
    }

    public void SaveState()
    {
        _session.SavePlayer(_player, _currentMap?.MapId ?? GameConstants.StartMap);
        if (_currentMap != null)
            _session.SaveMapObjects(_currentMap);
    }

    public ScreenTransition Update(GameTime gameTime)
    {
        if (_isLoading)
        {
            _loadingFrameCount++;
            if (_loadingFrameCount == 2)
            {
                _deferredLoadAction?.Invoke();
                _deferredLoadAction = null;
            }
            else if (_loadingFrameCount >= 3)
            {
                _isLoading = false;
            }
            return ScreenTransition.None;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mapTransition.Update(dt);
        _toast.Update(dt);

        // Auto-save on configurable interval
        float interval = GameConstants.AutoSaveInterval;
        if (interval > 0f)
        {
            _autoSaveTimer += dt;
            if (_autoSaveTimer >= interval)
            {
                _autoSaveTimer = 0f;
                SaveState();
                MessageQueue.Enqueue(LocaleManager.Get("ui", "auto_saved", "Auto-saved"));
            }
        }

        // Dialogue overlay blocks all game input while open
        if (_dialogue.Update())
            return ScreenTransition.None;

        var keyboard = KeyboardExtended.GetState();
        if (keyboard.WasKeyPressed(Keys.Escape))
            return ScreenTransition.To(GameState.Paused);

        _player.Update(gameTime);
        _currentMap.Update(dt);
        _camera.Update(_player);
        _inspector.Update(_currentMap, _camera);

        // Handle pending interaction (e.g. teleport)
        if (_currentMap.PendingInteraction != null)
        {
            var req = _currentMap.PendingInteraction;
            _currentMap.PendingInteraction = null;
            HandleTeleport(req);
        }

        return ScreenTransition.None;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_isLoading)
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

        if (_currentMap != null)
        {
            DrawWorld(spriteBatch);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _inspector.DrawHUD(spriteBatch);
            _toast.Draw(spriteBatch);
            if (_mapTransition.IsActive)
                _mapTransition.Draw(spriteBatch);
            _dialogue.Draw(spriteBatch);
            spriteBatch.End();
        }
    }

    public void DrawWorld(SpriteBatch spriteBatch)
    {
        if (_currentMap == null || _camera == null || _player == null) return;

        spriteBatch.Begin(
            transformMatrix: _camera.TransformMatrix,
            samplerState: SamplerState.PointClamp);
        _currentMap.Draw(spriteBatch, _camera);
        _inspector.DrawWorldMarker(spriteBatch);
        _player.Draw(spriteBatch);
        _currentMap.DrawObjectInfo(spriteBatch, _player.GridPosition);
        spriteBatch.End();
    }

    private void HandleTeleport(InteractionRequest req)
    {
        // Save current map state before leaving and set TTL for expiry
        SaveState();
        _session.SetMapStateTtlOnLeave();

        _isLoading = true;
        _loadingFrameCount = 0;
        _deferredLoadAction = () =>
        {
            // Build a temporary PlayerState for the target map
            var teleportState = new PlayerState
            {
                Uuid = "",
                PositionX = req.TargetX,
                PositionY = req.TargetY,
                FacingDirection = _player.FacingDirection.ToString(),
                CurrentMap = req.TargetMap,
                CurrentMapStateId = null, // will be resolved by LoadOrCreateMapState
                MaxHp = _player.MaxHp,
                CurrentHp = _player.CurrentHp,
                Strength = _player.Strength,
                Dexterity = _player.Dexterity,
                WeaponAtk = _player.WeaponAtk,
                BuffPercent = _player.BuffPercent,
                CritRate = _player.CritRate,
                CritDamage = _player.CritDamage,
            };

            // Load the target map
            var result = GameplayInitializer.Run(teleportState, _registry, _loadTexture, _graphicsDevice, _contentDir);
            _currentMap = result.Map;
            _player = result.Player;
            _camera = result.Camera;
            _autoSaveTimer = 0f;

            _player.RestoreAttributes(teleportState);

            // Load or create map state for the target map
            _session.CurrentMapStateId = null;
            _session.LoadOrCreateMapState(result.Map.MapId, result.Map, null);

            _camera.SetWorldBounds(_currentMap);
            _inspector.SetPlayerObject(_player.WorldProxy);
            _currentMap.PlayerProxy = _player.WorldProxy;

            _player.OnInteract = obj =>
            {
                string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
                MessageQueue.Enqueue(LocaleManager.Format("ui", "interact", name));
            };

            _mapTransition.Start(result.MapName);
        };
    }
}
