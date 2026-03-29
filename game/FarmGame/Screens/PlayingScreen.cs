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
using FarmGame.Entities;
using FarmGame.Persistence.Models;
using FarmGame.Screens.HUD;
using FarmGame.World;

namespace FarmGame.Screens;

public class PlayingScreen : IScreen
{
    private readonly Func<string, Texture2D> _loadTexture;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly DataRegistry _registry;
    private readonly MapTransitionOverlay _mapTransition;
    private readonly ToastAlert _toast;

    private GameMap _currentMap;
    private Player _player;
    private Camera2D _camera;

    public Player Player => _player;
    public GameMap CurrentMap => _currentMap;

    public PlayingScreen(
        GraphicsDevice graphicsDevice,
        DataRegistry registry,
        Func<string, Texture2D> loadTexture)
    {
        _graphicsDevice = graphicsDevice;
        _registry = registry;
        _loadTexture = loadTexture;
        _mapTransition = new MapTransitionOverlay();
        _toast = new ToastAlert();
    }

    public void Initialize() { }
    public void Rebuild() { }
    public void OnEnter(GameState fromState) { }

    public void StartGame(PlayerState savedState)
    {
        var result = GameplayInitializer.Run(savedState, _registry, _loadTexture, _graphicsDevice);
        _currentMap = result.Map;
        _player = result.Player;
        _camera = result.Camera;

        _mapTransition.Start(result.MapName);
        _toast.Show(LocaleManager.Format("ui", "entered_map", result.MapName));
    }

    public ScreenTransition Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mapTransition.Update(dt);
        _toast.Update(dt);

        var keyboard = KeyboardExtended.GetState();
        if (keyboard.WasKeyPressed(Keys.Escape))
            return ScreenTransition.To(GameState.Paused);

        _player.Update(gameTime);
        _camera.Update(_player, _currentMap);

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
        spriteBatch.End();
    }
}
