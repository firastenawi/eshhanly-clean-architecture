namespace Eshhanly.Domain.Common;

/// <summary>Half-open UTC interval <c>[Start, End)</c>. Normalised to UTC because PostgreSQL timestamptz requires it.</summary>
public sealed record TimeSlot
{
    private TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public TimeSpan Duration => End - Start;

    public static Result<TimeSlot> Create(DateTimeOffset start, DateTimeOffset end)
    {
        var utcStart = start.ToUniversalTime();
        var utcEnd = end.ToUniversalTime();

        return utcEnd <= utcStart
            ? Error.Validation("TimeSlot.Invalid", "End time must be after start time.")
            : new TimeSlot(utcStart, utcEnd);
    }

    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;

    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;
}
