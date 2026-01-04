using Microsoft.EntityFrameworkCore;
using NodeGraph.Api.Data.Entities;

namespace NodeGraph.Api.Data;

/// <summary>
/// グラフデータベースコンテキスト
/// </summary>
public class GraphDbContext : DbContext
{
    public GraphDbContext(DbContextOptions<GraphDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// グラフエンティティのDbSet
    /// </summary>
    public DbSet<GraphEntity> Graphs => Set<GraphEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GraphEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ModifiedAt);
        });
    }
}
