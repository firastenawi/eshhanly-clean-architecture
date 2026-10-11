namespace Eshhanly.Application.Common.Exceptions;

/// <summary>
/// Thrown by SaveChanges when the database exclusion constraint rejects an overlapping booking
/// (another request took the slot between our check and our insert).
/// </summary>
public sealed class SlotUnavailableException : Exception
{
    public SlotUnavailableException() { }

    public SlotUnavailableException(string message) : base(message) { }

    public SlotUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}
