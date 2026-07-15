using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using BesqlPersistencePoc;
using BesqlPersistencePoc.Data;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Blazorise — wired exactly as production does.
builder.Services
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddBlazorise(options => options.Immediate = true);

// BeSQL DbContext factory — this is the behaviour under test.
// Keep the DB file name "clikclientdb.sqlite3" so the durable copy lands at the SAME Cache Storage
// key as production: cache "bit-Besql", entry "/data/cache/clikclientdb.sqlite3".
builder.Services.AddBesqlDbContextFactory<PocDbContext>(
    opts => opts.UseSqlite("Data Source=clikclientdb.sqlite3"));

var host = builder.Build();

// Startup initialisation — replicates production's pattern, including the fallback.
// The fallback can WIPE the DB, so the POC behaves the same way: otherwise a "data gone" result
// could be misread as cache eviction rather than a migration reset.
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var factory = services.GetRequiredService<IDbContextFactory<PocDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }
}

await host.RunAsync();
