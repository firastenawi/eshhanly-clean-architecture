using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Reviews;

public enum ReviewTarget
{
    /// <summary>Guest reviews the station (and therefore its host).</summary>
    Station = 0,
    /// <summary>Host reviews the guest.</summary>
    Guest = 1,
}

/// <summary>One review per author per completed booking. Rating 1..5.</summary>
public sealed class Review : AggregateRoot
{
    public const int CommentMaxLength = 1000;

    private Review() { }

    public Guid BookingId { get; private set; }
    public Guid AuthorId { get; private set; }
    public ReviewTarget Target { get; private set; }

    /// <summary>Station id or guest user id, depending on <see cref="Target"/>.</summary>
    public Guid TargetId { get; private set; }

    public short Rating { get; private set; }
    public string? Comment { get; private set; }
    public bool IsDeleted { get; private set; }

    public static Result<Review> Create(Guid bookingId, Guid authorId, ReviewTarget target, Guid targetId, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
        {
            return Error.Validation("Review.InvalidRating", "Rating must be between 1 and 5.");
        }

        if (comment?.Length > CommentMaxLength)
        {
            return Error.Validation("Review.CommentTooLong", $"Comment must be at most {CommentMaxLength} characters.");
        }

        return new Review
        {
            BookingId = bookingId,
            AuthorId = authorId,
            Target = target,
            TargetId = targetId,
            Rating = (short)rating,
            Comment = comment?.Trim(),
        };
    }

    public void Delete() => IsDeleted = true;
}
