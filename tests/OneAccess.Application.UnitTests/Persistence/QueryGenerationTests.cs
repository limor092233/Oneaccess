using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OneAccess.Domain.Entities;
using OneAccess.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace OneAccess.Application.UnitTests.Persistence;

public class QueryGenerationTests
{
    private readonly ITestOutputHelper _output;

    public QueryGenerationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private OneAccessDbContext CreateSqlServerDbContext()
    {
        var options = new DbContextOptionsBuilder<OneAccessDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=OneAccessTest;Trusted_Connection=True;")
            .Options;

        return new OneAccessDbContext(options);
    }

    [Fact]
    public void SectionLookup_BySectionId_GeneratesParameterizedWhereClause()
    {
        using var db = CreateSqlServerDbContext();
        var targetSectionId = Guid.NewGuid();

        var query = db.Sections.Where(s => s.Id == targetSectionId);
        var sql = query.ToQueryString();

        _output.WriteLine("=== Section Lookup SQL ===");
        _output.WriteLine(sql);

        sql.Should().Contain("WHERE");
        sql.Should().Contain("[s].[Id] = @__targetSectionId_0");
    }

    [Fact]
    public void DivisionUniqueness_ByName_GeneratesParameterizedWhereClause()
    {
        using var db = CreateSqlServerDbContext();
        var targetName = "engineering";

        var query = db.Divisions.Where(d => d.Name.ToLower() == targetName);
        var sql = query.ToQueryString();

        _output.WriteLine("=== Division Uniqueness SQL ===");
        _output.WriteLine(sql);

        sql.Should().Contain("WHERE");
        sql.Should().Contain("LOWER([d].[Name]) = @__targetName_0");
    }

    [Fact]
    public void SectionUniquenessInDivision_GeneratesParameterizedWhereClause()
    {
        using var db = CreateSqlServerDbContext();
        var sectionId = Guid.NewGuid();
        var divisionId = Guid.NewGuid();
        var targetName = "backend";

        var query = db.Sections.Where(s => s.Id != sectionId && s.DivisionId == divisionId && s.Name.ToLower() == targetName);
        var sql = query.ToQueryString();

        _output.WriteLine("=== Section Uniqueness in Division SQL ===");
        _output.WriteLine(sql);

        sql.Should().Contain("WHERE");
        sql.Should().Contain("[s].[Id] <> @__sectionId_0");
        sql.Should().Contain("[s].[DivisionId] = @__divisionId_1");
        sql.Should().Contain("LOWER([s].[Name]) = @__targetName_2");
    }
}
