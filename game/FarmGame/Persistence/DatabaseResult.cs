namespace FarmGame.Persistence;

public enum DatabaseErrorKind
{
    None,
    DirectoryCreationFailed,
    PermissionDenied,
    DiskSpaceInsufficient,
    DatabaseCorrupted,
    MigrationFailed,
    ConnectionFailed
}

public class DatabaseResult
{
    public bool Success { get; }
    public DatabaseErrorKind ErrorKind { get; }
    public string ErrorMessage { get; }

    protected DatabaseResult(bool success, DatabaseErrorKind errorKind, string errorMessage)
    {
        Success = success;
        ErrorKind = errorKind;
        ErrorMessage = errorMessage;
    }

    public static DatabaseResult Ok() => new(true, DatabaseErrorKind.None, null);

    public static DatabaseResult Fail(DatabaseErrorKind kind, string message) =>
        new(false, kind, message);
}

public class DatabaseResult<T> : DatabaseResult
{
    public T Value { get; }

    private DatabaseResult(bool success, DatabaseErrorKind errorKind, string errorMessage, T value)
        : base(success, errorKind, errorMessage)
    {
        Value = value;
    }

    public static DatabaseResult<T> Ok(T value) =>
        new(true, DatabaseErrorKind.None, null, value);

    public new static DatabaseResult<T> Fail(DatabaseErrorKind kind, string message) =>
        new(false, kind, message, default);
}
