using Atlas.Backoffice.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Backoffice.Api.Infrastructure.Persistence;

public sealed class BackofficeDbContext : DbContext
{
    public BackofficeDbContext(DbContextOptions<BackofficeDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReviewCase> ReviewCases => Set<ReviewCase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reviewCase = modelBuilder.Entity<ReviewCase>();
        reviewCase.ToTable("ReviewCases");
        reviewCase.HasKey(c => c.Id);
        reviewCase.Property(c => c.ApplicationId).HasMaxLength(35).IsUnicode(false);
        reviewCase.Property(c => c.Market).HasMaxLength(2).IsUnicode(false);
        reviewCase.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        reviewCase.Property(c => c.Outcome).HasConversion<string>().HasMaxLength(20);
        reviewCase.Property(c => c.DecidedBy).HasMaxLength(100);
        reviewCase.HasIndex(c => c.ApplicationId).IsUnique(); // one case per application, even if the event arrives twice
        reviewCase.HasIndex(c => new { c.Market, c.Status, c.DueBy });
    }
}

/// <summary>Creates the Backoffice database on startup (local convenience; production would use migrations).</summary>
internal sealed class BackofficeDatabaseInitializer : IHostedService
{
    private const int MaxAttempts = 10;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<BackofficeDatabaseInitializer> _logger;

    public BackofficeDatabaseInitializer(IServiceScopeFactory scopes, ILogger<BackofficeDatabaseInitializer> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<BackofficeDbContext>();
                await db.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Max}), retrying", attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
