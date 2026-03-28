using System;
using Microsoft.Data.Sqlite;
using FarmGame.Persistence.Models;

namespace FarmGame.Persistence;

// =============================================================================
// PlayerStateRepository.cs — Save and load PlayerState from SQLite
//
// Functions:
//   - Save(slotName, state, gameVersion) : Insert or update a player state JSON blob.
//   - Load(slotName)                     : Read and deserialize a player state by slot name.
//   - Delete(slotName)                   : Remove a saved player state.
//   - Exists(slotName)                   : Check if a save slot exists.
// =============================================================================
public class PlayerStateRepository
{
    private readonly DatabaseBootstrapper _db;

    public PlayerStateRepository(DatabaseBootstrapper db)
    {
        _db = db;
    }

    public DatabaseResult Save(string slotName, PlayerState state, string gameVersion)
    {
        try
        {
            using var connection = _db.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"
                INSERT INTO player_state (slot_name, state_json, game_version)
                VALUES (@slot, @json, @ver)
                ON CONFLICT(slot_name) DO UPDATE SET
                    state_json = @json,
                    game_version = @ver,
                    updated_at = datetime('now');
            ";
            cmd.Parameters.AddWithValue("@slot", slotName);
            cmd.Parameters.AddWithValue("@json", state.ToJson());
            cmd.Parameters.AddWithValue("@ver", gameVersion);
            cmd.ExecuteNonQuery();

            transaction.Commit();
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save player state: {ex.Message}");
        }
    }

    public DatabaseResult<PlayerState> Load(string slotName)
    {
        try
        {
            using var connection = _db.CreateConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();

            cmd.CommandText = "SELECT state_json FROM player_state WHERE slot_name = @slot;";
            cmd.Parameters.AddWithValue("@slot", slotName);

            var result = cmd.ExecuteScalar();
            if (result is null or DBNull)
                return DatabaseResult<PlayerState>.Fail(DatabaseErrorKind.None,
                    $"No save found for slot '{slotName}'.");

            var state = PlayerState.FromJson((string)result);
            return DatabaseResult<PlayerState>.Ok(state);
        }
        catch (Exception ex)
        {
            return DatabaseResult<PlayerState>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load player state: {ex.Message}");
        }
    }

    public DatabaseResult Delete(string slotName)
    {
        try
        {
            using var connection = _db.CreateConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();

            cmd.CommandText = "DELETE FROM player_state WHERE slot_name = @slot;";
            cmd.Parameters.AddWithValue("@slot", slotName);
            cmd.ExecuteNonQuery();

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to delete player state: {ex.Message}");
        }
    }

    public bool Exists(string slotName)
    {
        try
        {
            using var connection = _db.CreateConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();

            cmd.CommandText = "SELECT COUNT(*) FROM player_state WHERE slot_name = @slot;";
            cmd.Parameters.AddWithValue("@slot", slotName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
        catch
        {
            return false;
        }
    }
}
