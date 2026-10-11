using Eshhanly.Domain.Common;

namespace Eshhanly.Application.Common;

public static class AppErrors
{
    public static readonly Error NotAuthenticated = Error.Forbidden("Auth.NotAuthenticated", "You must be signed in.");
    public static readonly Error NotAllowed = Error.Forbidden("Auth.NotAllowed", "You are not allowed to perform this action.");
    public static readonly Error NotHost = Error.Forbidden("Auth.NotHost", "Only hosts can perform this action.");
}
