using Microsoft.EntityFrameworkCore;
using ReportService.Models;

namespace ReportService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportItem> ReportItems => Set<ReportItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Report>().HasKey(x => x.UUID);
        b.Entity<ReportItem>().HasKey(x => x.UUID);
        b.Entity<Report>()
          .HasMany(x => x.Items)
          .WithOne()
          .HasForeignKey(x => x.ReportUUID)
          .OnDelete(DeleteBehavior.Cascade);
        b.Entity<ReportItem>().Property(x => x.Location).HasMaxLength(200).IsRequired();
    }
}
