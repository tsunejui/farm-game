// =============================================================================
// PlayerStateSaver.cs — Saves player state to database
// =============================================================================

using FarmGame.Core;
using FarmGame.Entities;
using FarmGame.Persistence.Models;
using FarmGame.Persistence.Repositories;
using Serilog;

namespace FarmGame.Persistence;

public class PlayerStateSaver
{
    private readonly PlayerStateRepository _repo;
    private readonly string _playerUuid;

    public PlayerStateSaver(PlayerStateRepository repo, string playerUuid)
    {
        _repo = repo;
        _playerUuid = playerUuid;
    }

    public void Save(Player player, string currentMap)
    {
        if (player == null) return;

        var state = new PlayerState
        {
            Uuid = _playerUuid,
            PositionX = player.GridPosition.X,
            PositionY = player.GridPosition.Y,
            FacingDirection = player.FacingDirection.ToString(),
            CurrentMap = currentMap,
        };

        var result = _repo.Save(_playerUuid, state, GameConstants.GameTitle);
        if (result.Success)
            Log.Information("Player state saved: pos=({X},{Y}), dir={Dir}",
                state.PositionX, state.PositionY, state.FacingDirection);
        else
            Log.Error("Failed to save player state: {Error}", result.ErrorMessage);
    }
}
