using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BesqlPersistencePoc.Data;

// Design-time only. `dotnet ef` cannot run the Blazor WASM host to resolve the runtime
// BeSQL factory, so it uses this to build the model when generating/applying migrations.
// Not used at runtime.
public class PocDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PocDbContext>
{
    public PocDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PocDbContext>()
            .UseSqlite("Data Source=clikclientdb.sqlite3")
            .Options;
        return new PocDbContext(options);
    }
}
