using Microsoft.EntityFrameworkCore;
using ContactService.Models;

namespace ContactService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<ContactInfo> ContactInfos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contact>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
                b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
                b.HasMany(x => x.ContactInfos)
                 .WithOne(ci => ci.Contact)
                 .HasForeignKey(ci => ci.ContactId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ContactInfo>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Value).IsRequired();
                b.Property(x => x.Type).IsRequired();
            });
        }
    }
}
