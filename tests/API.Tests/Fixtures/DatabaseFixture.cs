using API.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace API.Tests.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        public ProjectContext Context { get; private set; }

        public DatabaseFixture()
        {
            var options = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            Context = new ProjectContext(options);
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            // Here you can add seed data that should be available for all tests
            // This is useful for reference data that most tests will need
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
