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
using FarmGame.Persistence.Models;
using FarmGame.Screens.HUD;
using FarmGame.World;

namespace FarmGame.Screens;

public class PlayingScreen : IScreen, IWorldRenderer
{
    private readonly Func<string, Texture2D> _loadTexture;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly DataRegistry _registry;
    private readonly GameSession _session;
    private readonly MapTransitionOverlay _mapTransition;
    private readonly ToastAlert _toast;

    private GameMap _currentMap;
    private Entities.Player _player;
    private Camera2D _camera;
    private float _autoSaveTimer;

    public PlayingScreen(
        GraphicsDevice graphicsDevice,
        DataRegistry registry,
        Func<string, Texture2D> loadTexture,
        GameSession session)
    {
        _graphicsDevice = graphicsDevice;
        _registry = registry;
        _loadTexture = loadTexture;
        _session = session;
        _mapTransition = new MapTransitionOverlay();
        _toast = new ToastAlert();
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
        var result = GameplayInitializer.Run(savedState, _registry, _loadTexture, _graphicsDevice);
        _currentMap = result.Map;
        _player = result.Player;
        _camera = result.Camera;
        _autoSaveTimer = 0f;

        // Load or create persisted map entity state (HP, alive/dead)
        _session.LoadOrCreateMapState(result.Map.MapId, result.Map, savedState);

        // Set world bounds once at map load (not per-frame)
        _camera.SetWorldBounds(_currentMap);

        _mapTransition.Start(result.MapName);
        _toast.Show(LocaleManager.Format("ui", "entered_map", result.MapName));
    }

    public void SaveState()
    {
        _session.SavePlayer(_player, _currentMap?.MapId ?? GameConstants.StartMap);
        if (_currentMap != null)
            _session.SaveMapEntities(_currentMap);
    }

    public ScreenTransition Update(GameTime gameTime)
    {
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
                _toast.Show(LocaleManager.Get("ui", "auto_saved", "Auto-saved"));
            }
        }

        var keyboard = KeyboardExtended.GetState();
        if (keyboard.WasKeyPressed(Keys.Escape))
            return ScreenTransition.To(GameState.Paused);

        _player.Update(gameTime);
        _currentMap.Update(dt);
        _camera.Update(_player);

        return ScreenTransition.None;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        DrawWorld(spriteBatch);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _toast.Draw(spriteBatch);
        if (_mapTransition.IsActive)
            _mapTransition.Draw(spriteBatch);
        spriteBatch.End();
    }

    public void DrawWorld(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            transformMatrix: _camera.TransformMatrix,
            samplerState: SamplerState.PointClamp);
        _currentMap.Draw(spriteBatch, _camera);
        _player.Draw(spriteBatch);
        _currentMap.DrawEntityInfo(spriteBatch, _player.GridPosition);
        spriteBatch.End();
    }
}
