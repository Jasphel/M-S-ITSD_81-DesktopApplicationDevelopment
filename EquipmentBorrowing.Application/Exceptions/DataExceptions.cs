namespace EquipmentBorrowing.Application.Exceptions;

/// <summary>
/// The write was rejected because the stored data changed underneath us
/// (unique / foreign-key violation, stale row, serialization failure).
/// Nothing from the rejected operation was saved (BR-15).
/// </summary>
public class DataConflictException : Exception
{
    public DataConflictException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// A known failed write. The database statement failed, so the surrounding
/// transaction is rolled back and no partial change remains (BR-18).
/// </summary>
public class DataAccessException : Exception
{
    public DataAccessException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// The commit itself failed (for example the connection dropped) so we
/// cannot know whether PostgreSQL applied it. The caller must NOT assert a
/// rollback or retry automatically; staff re-read the data first (BR-18).
/// </summary>
public class CommitOutcomeUnknownException : Exception
{
    public CommitOutcomeUnknownException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
