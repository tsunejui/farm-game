using System;
using System.Linq;
using FarmGame.Models;
using FarmGame.Persistence.Entities;

namespace FarmGame.Persistence.Repositories;

// =============================================================================
// PlayerStateRepository.cs — Save and load PlayerState from SQLite
//
// Functions:
//   - Save(playerUuid, state, gameVersion) : Insert or update a player state by UUID.
//   - Load(playerUuid)                     : Read and deserialize a player state by UUID.
//   - Delete(playerUuid)                   : Remove a saved player state.
//   - Exists(playerUuid)                   : Check if a save exists for this UUID.
// =============================================================================
public class PlayerStateRepository
{
    private readonly DatabaseBootstrapper _db;

    public PlayerStateRepository(DatabaseBootstrapper db)
    {
        _db = db;
    }

    public DatabaseResult Save(string playerUuid, PlayerState state, string gameVersion)
    {
        try
        {
            using var db = _db.CreateConnection();
            var existing = db.Table<PlayerStateRecord>()
                .FirstOrDefault(r => r.PlayerUuid == playerUuid);

            var now = DateTime.UtcNow.ToString("o");

            if (existing != null)
            {
                existing.StateJson = state.ToJson();
                existing.GameVersion = gameVersion;
                existing.MaxHp = state.MaxHp;
                existing.CurrentHp = state.CurrentHp;
                existing.Strength = state.Strength;
                existing.Dexterity = state.Dexterity;
                existing.WeaponAtk = state.WeaponAtk;
                existing.BuffPercent = state.BuffPercent;
                existing.CritRate = state.CritRate;
                existing.CritDamage = state.CritDamage;
                existing.UpdatedAt = now;
                db.Update(existing);
            }
            else
            {
                db.Insert(new PlayerStateRecord
                {
                    PlayerUuid = playerUuid,
                    StateJson = state.ToJson(),
                    GameVersion = gameVersion,
                    MaxHp = state.MaxHp,
                    CurrentHp = state.CurrentHp,
                    Strength = state.Strength,
                    Dexterity = state.Dexterity,
                    WeaponAtk = state.WeaponAtk,
                    BuffPercent = state.BuffPercent,
                    CritRate = state.CritRate,
                    CritDamage = state.CritDamage,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save player state: {ex.Message}");
        }
    }

    public DatabaseResult<PlayerState> Load(string playerUuid)
    {
        try
        {
            using var db = _db.CreateConnection();
            var record = db.Table<PlayerStateRecord>()
                .FirstOrDefault(r => r.PlayerUuid == playerUuid);

            if (record == null)
                return DatabaseResult<PlayerState>.Fail(DatabaseErrorKind.None,
                    $"No save found for player '{playerUuid}'.");

            var state = PlayerState.FromJson(record.StateJson);
            return DatabaseResult<PlayerState>.Ok(state);
        }
        catch (Exception ex)
        {
            return DatabaseResult<PlayerState>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load player state: {ex.Message}");
        }
    }

    public DatabaseResult Delete(string playerUuid)
    {
        try
        {
            using var db = _db.CreateConnection();
            db.Table<PlayerStateRecord>()
                .Delete(r => r.PlayerUuid == playerUuid);
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to delete player state: {ex.Message}");
        }
    }

    public bool Exists(string playerUuid)
    {
        try
        {
            using var db = _db.CreateConnection();
            return db.Table<PlayerStateRecord>()
                .Any(r => r.PlayerUuid == playerUuid);
        }
        catch
        {
            return false;
        }
    }
}
