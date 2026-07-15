using Microsoft.EntityFrameworkCore;

namespace BesqlPersistencePoc.Data;

// Minimal DbContext — deliberately stripped of everything production's ClientDbContext couples in:
//   * no IObjectMapper / ICurrentUser / IValidationService ctor deps — plain options-only ctor
//   * no overridden SaveChangesAsync audit logic — plain SaveChangesAsync so failures surface loudly
//     (production swallows DbUpdateConcurrencyException and returns 0; we do NOT)
//   * no production entities / JSON HasConversion mappings — one trivial Note entity
public class PocDbContext : DbContext
{
    public PocDbContext(DbContextOptions<PocDbContext> options) : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Intentionally empty.
    }
}
