using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Infrastructure.Persistence;

/// <summary>
/// Design-time DbContext factory for generating EF Core migrations.
/// </summary>
public class OneAccessDbContextFactory : IDesignTimeDbContextFactory<OneAccessDbContext>
{
    public OneAccessDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OneAccessDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=OneAccessDb;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new OneAccessDbContext(optionsBuilder.Options);
    }
}
