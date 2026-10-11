using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_exposes_value()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_carries_error_and_hides_value()
    {
        Result<int> result = Error.NotFound("Station.NotFound", "Station was not found.");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }
}
