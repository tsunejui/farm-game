using System;
using FarmGame.Persistence.Entities;

namespace FarmGame.Persistence.Repositories;

public class SettingRepository
{
    private readonly DatabaseBootstrapper _db;

    public SettingRepository(DatabaseBootstrapper db)
    {
        _db = db;
    }

    public string Get(string key, string defaultValue = null)
    {
        try
        {
            using var db = _db.CreateConnection();
            var setting = db.Find<Setting>(key);
            return setting?.Value ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public DatabaseResult Set(string key, string value)
    {
        try
        {
            using var db = _db.CreateConnection();
            db.InsertOrReplace(new Setting { Key = key, Value = value });
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save setting '{key}': {ex.Message}");
        }
    }
}
