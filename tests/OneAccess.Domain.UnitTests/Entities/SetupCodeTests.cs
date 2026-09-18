using FluentAssertions;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Domain.UnitTests.Entities;

public class SetupCodeTests
{
    [Fact]
    public void IsValid_WhenUnconsumedAndUnexpired_ShouldReturnTrue()
    {
        var code = new SetupCode
        {
            CodeHash = "somehash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        code.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenConsumed_ShouldReturnFalse()
    {
        var code = new SetupCode
        {
            CodeHash = "somehash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            ConsumedAt = DateTime.UtcNow
        };

        code.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var code = new SetupCode
        {
            CodeHash = "somehash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };

        code.IsValid.Should().BeFalse();
    }
}
