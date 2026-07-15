# BeSQL Persistence POC

A deliberately tiny standalone **Blazor WebAssembly PWA** whose only job is to test whether a
[Bit.Besql](https://github.com/bitfoundation/bitplatform) SQLite database **persists to Cache
Storage** across page reloads and full app restarts.

No authentication, no API calls, no ABP framework, no backend. One entity (`Note`), one page.

## What it proves

Bit.Besql keeps the working SQLite database in memory and writes a durable copy into the browser's
**Cache Storage** under:

```
cache:  bit-Besql
entry:  /data/cache/clikclientdb.sqlite3
```

The DB file name is deliberately kept as `clikclientdb.sqlite3` so the durable copy lands at the
**same Cache Storage key as production**. Add a note, then reload (or fully close and reopen the
browser/PWA). If the notes come back, the database survived.

### How to run the test

1. Open the app, add a few notes.
2. Watch the status panel (row count + most-recent `CreatedAtUtc`).
3. Click **Reload page** (in-session check) — notes should persist.
4. For a real restart test: close the tab/app entirely and reopen it. On startup the app reads
   existing rows straight from the DB; if they render, Cache Storage persistence works.
5. Inspect directly in DevTools → **Application → Cache Storage → `bit-Besql`**.

## Key mechanics (the behaviour under test)

- **Registration** (`Program.cs`) — production's portable pattern:
  ```csharp
  builder.Services.AddBesqlDbContextFactory<PocDbContext>(
      opts => opts.UseSqlite("Data Source=clikclientdb.sqlite3"));
  ```
- **Startup init** replicates production, including the fallback that can wipe-and-recreate the DB.
  This matters: if a migration reset ever nukes the data, the POC must fail the *same* way so a
  "data gone" result isn't misread as cache eviction.
  ```csharp
  try { await db.Database.MigrateAsync(); }
  catch { await db.Database.EnsureDeletedAsync(); await db.Database.MigrateAsync(); }
  ```
  Uses `MigrateAsync` (not `EnsureCreated`), so the repo ships one EF Core migration (`Initial`).
- **`PocDbContext`** is intentionally minimal: plain options-only ctor, plain `SaveChangesAsync`
  (no swallowed exceptions — failures surface loudly), empty `OnModelCreating`, one `Note` entity.

## Versions

| Component | Version |
|---|---|
| Target framework | `net10.0` (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`) |
| Bit.Besql | 10.4.3 |
| Blazorise (Bootstrap5 + FontAwesome) | 2.2.1 |
| Microsoft.EntityFrameworkCore.Sqlite / .Tools | 10.0.8 |

> **⚠️ Verify the EF SQLite version against production.** Production's
> `src/shared/Clik.Client.Data/Clik.Client.Data.csproj` was not available in this workspace, so
> the SQLite provider is pinned to **10.0.8** (matching the locally installed `dotnet ef` CLI).
> Bump it to production's exact `10.0.x` if they differ.

> **Note — Blazorise 2.x rename:** the text box uses `<TextInput @bind-Value="…">`, not the
> Blazorise 1.x `<TextEdit @bind-Text="…">`. In Blazorise 2.0 the `*Edit` inputs were renamed to
> `*Input` and their value parameter to `Value`/`ValueChanged`.

> **Security note:** `dotnet restore` reports `NU1903` for the transitive
> `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (pulled in by the EF SQLite provider). It's a known advisory
> in a transitive dependency; acceptable for a throwaway POC, but worth noting.

## Run locally

```bash
dotnet run
# or, with hot reload:
dotnet watch
```

Then browse to the URL printed in the console (e.g. `https://localhost:5xxx`).

> Cache Storage requires a **secure context** — `localhost` and `https` both qualify, so
> persistence works in local dev.

## Publish (static site)

```bash
dotnet publish -c Release -o publish
```

The deployable static site is produced at **`publish/wwwroot`** (contains `index.html`,
`_framework/`, the PWA `manifest.webmanifest` + `service-worker.js`, etc.).

## Deploy — GitHub Pages

Pushing to `main` runs [.github/workflows/deploy.yml](.github/workflows/deploy.yml), which:

1. Installs .NET `10.0.x` and the `wasm-tools` workload.
2. `dotnet publish -c Release -o publish`.
3. Rewrites `<base href="/">` → `/<repo-name>/` (project Pages are served from a sub-path).
4. Copies `index.html` → `404.html` (SPA deep-link fallback).
5. Adds `.nojekyll` (so Pages doesn't strip the `_framework` folder).
6. Uploads `publish/wwwroot` and deploys it.

**One-time setup:** in the repo, **Settings → Pages → Build and deployment → Source: GitHub
Actions**.

> **Why no `vercel.json` / `_redirects`?** Those are Vercel/Netlify SPA-fallback files and are
> ignored by GitHub Pages. On Pages the fallback is the `404.html` copy the workflow makes, so they
> were intentionally left out.

## PWA

Uses the **stock** Blazor WASM PWA service worker (`manifest.webmanifest` + `service-worker.js` /
`service-worker.published.js`) purely for app-asset caching. Production's custom offline/token
service worker is out of scope — BeSQL's Cache Storage persistence is independent of it.

## Project layout

```
Program.cs                         Blazorise + BeSQL registration + migrate/fallback init
Data/Note.cs                       the one entity (Id, Text, CreatedAtUtc)
Data/PocDbContext.cs               minimal DbContext (DbSet<Note>)
Data/PocDbContextDesignTimeFactory design-time factory so `dotnet ef` can build the model
Migrations/                        EF Core "Initial" migration + model snapshot
Pages/Home.razor                   the whole UI (add note, list, status panel, reload)
wwwroot/                           index.html, PWA manifest + service worker, icons
.github/workflows/deploy.yml       GitHub Pages CI/CD
```

### Regenerating the migration

`dotnet ef` can't execute a browser-wasm build, so migrations aren't generated in-project. To
regenerate: compile `Data/*.cs` from a temporary **desktop** class library (`Microsoft.NET.Sdk`,
`net10.0`, referencing `Microsoft.EntityFrameworkCore.Sqlite` + `.Design`) and run
`dotnet ef migrations add <Name> --output-dir <this-repo>/Migrations --namespace
BesqlPersistencePoc.Migrations` against it. The generated files only reference EF Core + the
entity, so they compile unchanged in the WASM project.
