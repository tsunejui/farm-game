// =============================================================================
// ScreenManager.cs — Scene orchestrator
//
// Manages game scenes (GameState). Each scene defines which controllers
// are active. On scene transition, ScreenManager swaps the active set
// in ControllerManager.
//
// Scenes:
//   TitleScreen → TitleController only
//   Settings    → SettingsController only
//   Loading     → LoadingController only
//   Playing     → Background + World + Particle + UI + Network
// =============================================================================

using System;
using System.Collections.Generic;
using Serilog;

namespace FarmGame.Core;

/// <summary>
/// A scene definition: which controllers to activate for a given GameState.
/// </summary>
public class SceneDefinition
{
    public GameState State { get; init; }
    public Func<IController[]> ControllerFactory { get; init; }
}

/// <summary>
/// Orchestrates scene transitions by swapping active controllers.
/// </summary>
public class ScreenManager
{
    private readonly Dictionary<GameState, SceneDefinition> _scenes = new();
    private readonly ControllerManager _controllerManager;
    private GameState _currentState;

    public GameState CurrentState => _currentState;

    public ScreenManager(ControllerManager controllerManager)
    {
        _controllerManager = controllerManager;
    }

    /// <summary>Register a scene definition for a game state.</summary>
    public void RegisterScene(GameState state, Func<IController[]> controllerFactory)
    {
        _scenes[state] = new SceneDefinition { State = state, ControllerFactory = controllerFactory };
    }

    /// <summary>
    /// Transition to a new scene. Deactivates all controllers,
    /// then activates only the ones defined for the target scene.
    /// </summary>
    public void TransitionTo(GameState target)
    {
        var from = _currentState;
        _currentState = target;

        _controllerManager.DeactivateAll();

        if (_scenes.TryGetValue(target, out var scene))
        {
            var controllers = scene.ControllerFactory();
            foreach (var c in controllers)
                _controllerManager.Activate(c);
        }

        Log.Information("[ScreenManager] {From} → {To}", from, target);
    }

    /// <summary>Initialize the first scene.</summary>
    public void Initialize(GameState initialState)
    {
        _currentState = initialState;
        TransitionTo(initialState);
    }
}
