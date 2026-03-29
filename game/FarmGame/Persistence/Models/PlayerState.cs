using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmGame.Persistence.Models;

// =============================================================================
// PlayerState.cs — Player state model for persistence
//
// Serialized as JSON into the database. The "version" field tracks the model
// schema version, enabling forward migration of saved data independently
// from the database schema version.
//
// Functions:
//   - ToJson()              : Serialize this state to a JSON string.
//   - FromJson(string json) : Deserialize a JSON string to PlayerState, with version migration.
//   - Migrate(state)        : Upgrade older model versions to the current version.
// =============================================================================
public class PlayerState
{
    public const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = "";

    [JsonPropertyName("position_x")]
    public int PositionX { get; set; }

    [JsonPropertyName("position_y")]
    public int PositionY { get; set; }

    [JsonPropertyName("facing_direction")]
    public string FacingDirection { get; set; } = "Down";

    [JsonPropertyName("current_map")]
    public string CurrentMap { get; set; } = "farm_home";

    [JsonPropertyName("play_time_seconds")]
    public double PlayTimeSeconds { get; set; }

    // UUID linking to the map_state table; null means first visit to this map
    [JsonPropertyName("current_map_state_id")]
    public string CurrentMapStateId { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static PlayerState FromJson(string json)
    {
        var state = JsonSerializer.Deserialize<PlayerState>(json, JsonOptions);
        if (state == null)
            return new PlayerState();

        return Migrate(state);
    }

    private static PlayerState Migrate(PlayerState state)
    {
        if (state.Version < 2)
        {
            // v1 → v2: added current_map_state_id (null = first visit)
            state.CurrentMapStateId = null;
            state.Version = 2;
        }

        state.Version = CurrentVersion;
        return state;
    }
}
