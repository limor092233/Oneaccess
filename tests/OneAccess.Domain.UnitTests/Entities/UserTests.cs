using FluentAssertions;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Domain.UnitTests.Entities;

public class UserTests
{
    [Fact]
    public void NewUser_ShouldHaveDefaultValues()
    {
        // Act
        var user = new User
        {
            Username = "johndoe",
            Email = "john@example.com",
            FullName = "John Doe"
        };

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Status.Should().Be(UserStatus.Active);
        user.IsSystemAdministrator.Should().BeFalse();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeNull();
        user.DivisionId.Should().BeNull();
        user.SectionId.Should().BeNull();
        user.UserRoles.Should().BeEmpty();
    }
}
