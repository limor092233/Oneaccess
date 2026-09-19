using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Users.Commands.CreateUser;
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
        _readDbContext.Sections.Returns(new List<Section> { section }.AsQueryable());

        var command = new CreateUserCommand("user", "user@test.com", "Password123!", "Full Name", divisionId, sectionId);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Section does not belong to the specified Division"));
    }

    [Fact]
    public async Task Validate_WhenSectionBelongsToDivision_ShouldPass()
    {
        var divisionId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        var section = new Section { Id = sectionId, DivisionId = divisionId, Name = "Sec1" };
        _readDbContext.Sections.Returns(new List<Section> { section }.AsQueryable());

        var command = new CreateUserCommand("user", "user@test.com", "Password123!", "Full Name", divisionId, sectionId);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
