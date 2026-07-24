using Microsoft.EntityFrameworkCore;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.UnitTests.TestHelpers;

public static class TestDb
{
    // Each call gets its own isolated in-memory database, so tests never see each other's data.
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
