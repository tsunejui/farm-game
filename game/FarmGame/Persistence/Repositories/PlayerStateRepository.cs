using System;
using System.Linq;
using FarmGame.Models;
using FarmGame.Persistence.Entities;

namespace FarmGame.Persistence.Repositories;

public class PlayerStateRepository
{
    private readonly DatabaseManager _dbManager;

    public PlayerStateRepository(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    public DatabaseResult Save(string playerUuid, PlayerState state, string gameVersion)
    {
        try
        {
            using var db = _dbManager.Connect();
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
            using var db = _dbManager.Connect();
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
            using var db = _dbManager.Connect();
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
            using var db = _dbManager.Connect();
            return db.Table<PlayerStateRecord>()
                .Any(r => r.PlayerUuid == playerUuid);
        }
        catch
        {
            return false;
        }
    }
}
