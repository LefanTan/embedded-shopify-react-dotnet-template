using Microsoft.EntityFrameworkCore;
using backend.Models;

public class SessionContext : DbContext
{
    public DbSet<Session> Sessions { get; set; } = null!;

    public string DbPath { get; }

    public SessionContext()
    {
        var path = Directory
            .GetParent(AppDomain.CurrentDomain.BaseDirectory)
            ?.Parent?.Parent?.Parent?.FullName;
        // Creates a db in /backend
        DbPath = Path.Join(path, "app.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={DbPath}");
}
