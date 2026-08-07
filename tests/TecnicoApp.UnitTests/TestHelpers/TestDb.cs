using Microsoft.EntityFrameworkCore;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.UnitTests.TestHelpers;

public static class TestDb
{
    // Each call gets its own isolated in-memory database, so tests never see each other's data.
    public static AppDbContext Create() => Create(Guid.NewGuid().ToString());

    // Opens a new context against a named in-memory database. Passing the same name as an
    // earlier Create() call gives a second, independently-tracked context over the same
    // underlying store — use this for an Act-phase context when a test needs to seed data
    // and then exercise a handler without the handler's queries returning entities already
    // tracked by the Arrange-phase context (mirrors production, where every request gets a
    // fresh scoped DbContext rather than reusing one across unrelated operations).
    public static AppDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }
}
