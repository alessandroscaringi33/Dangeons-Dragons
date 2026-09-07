using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DndCompanion.Infrastructure.Data;

/// <summary>
/// Design-time factory used by the EF Core CLI (<c>dotnet ef</c>) to create a
/// context when generating migrations, since this assembly is a class library
/// with no startup host.
/// </summary>
public sealed class CampaignDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CampaignDbContext>
{
    public CampaignDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new CampaignDbContext(options);
    }
}
