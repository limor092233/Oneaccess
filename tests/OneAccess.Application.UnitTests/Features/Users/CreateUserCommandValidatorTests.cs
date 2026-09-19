using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Users.Commands.CreateUser;
using OneAccess.Application.UnitTests.Common;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Users;

public class CreateUserCommandValidatorTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _validator = new CreateUserCommandValidator(_readDbContext);
    }

    [Fact]
    public async Task Validate_WhenSectionProvidedWithoutDivision_ShouldFail()
    {
        var command = new CreateUserCommand("user", "user@test.com", "Password123!", "Full Name", null, Guid.NewGuid());
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("DivisionId must be specified"));
    }

    [Fact]
    public async Task Validate_WhenSectionDoesNotBelongToDivision_ShouldFail()
    {
        var divisionId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var wrongDivisionId = Guid.NewGuid();

        var section = new Section { Id = sectionId, DivisionId = wrongDivisionId, Name = "Sec1" };
        _readDbContext.Sections.Returns(new List<Section> { section }.BuildMockQueryable());

        var command = new CreateUserCommand("user", "user@test.com", "Password123!", "Full Name", divisionId, sectionId);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SectionId" && e.ErrorMessage.Contains("Section does not belong to the specified Division"));
    }

    [Fact]
    public async Task Validate_WhenSectionBelongsToDivision_ShouldPass()
    {
        var divisionId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        var section = new Section { Id = sectionId, DivisionId = divisionId, Name = "Sec1" };
        _readDbContext.Sections.Returns(new List<Section> { section }.BuildMockQueryable());

        var command = new CreateUserCommand("user", "user@test.com", "Password123!", "Full Name", divisionId, sectionId);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Short1!", "Password must be at least 12 characters.")]
    [InlineData("alllowercase123!", "Password must contain at least one uppercase letter.")]
    [InlineData("ALLUPPERCASE123!", "Password must contain at least one lowercase letter.")]
    [InlineData("NoDigitsHere!!", "Password must contain at least one digit.")]
    [InlineData("NoSpecialChar123", "Password must contain at least one non-alphanumeric character.")]
    public async Task Validate_WhenPasswordFailsPolicy_ShouldFailWithSpecificMessage(string password, string expectedError)
    {
        var command = new CreateUserCommand("user", "user@test.com", password, "Full Name", null, null);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedError);
    }
}
