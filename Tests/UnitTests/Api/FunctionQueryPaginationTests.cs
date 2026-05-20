using Vela.Api.Functions.Support;
using Microsoft.Extensions.Primitives;

namespace Vela.Tests.UnitTests.Api;

public sealed class FunctionQueryPaginationTests
{
    [Fact]
    public void TryOptionalPagination_WhenPaginationIsAbsent_ReturnsNoOptions()
    {
        var query = new Dictionary<string, StringValues>();

        var ok = FunctionQuery.TryOptionalPagination(query, out var pagination, out var error);

        ok.Should().BeTrue();
        pagination.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryOptionalPagination_WhenLimitAndOffsetAreValid_ReturnsOptions()
    {
        var query = new Dictionary<string, StringValues>
        {
            ["limit"] = "25",
            ["offset"] = "50"
        };

        var ok = FunctionQuery.TryOptionalPagination(query, out var pagination, out var error);

        ok.Should().BeTrue();
        pagination.Should().Be(new PaginationOptions(25, 50));
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    public void TryOptionalPagination_WhenLimitIsOutOfRange_ReturnsValidationError(string limit)
    {
        var query = new Dictionary<string, StringValues>
        {
            ["limit"] = limit
        };

        var ok = FunctionQuery.TryOptionalPagination(query, out var pagination, out var error);

        ok.Should().BeFalse();
        pagination.Should().BeNull();
        error.Should().Contain("Invalid limit");
    }

    [Fact]
    public void TryOptionalPagination_WhenOffsetIsNegative_ReturnsValidationError()
    {
        var query = new Dictionary<string, StringValues>
        {
            ["offset"] = "-1"
        };

        var ok = FunctionQuery.TryOptionalPagination(query, out var pagination, out var error);

        ok.Should().BeFalse();
        pagination.Should().BeNull();
        error.Should().Be("Invalid offset. Expected a value greater than or equal to 0.");
    }
}
