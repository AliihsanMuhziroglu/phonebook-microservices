using ContactService.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<ContactInfo> ContactInfos => Set<ContactInfo>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Person>().HasKey(x => x.UUID);
        b.Entity<Person>()
            .Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Entity<Person>()
            .Property(x => x.LastName).HasMaxLength(100).IsRequired();

        b.Entity<ContactInfo>().HasKey(x => x.UUID);
        b.Entity<ContactInfo>()
            .Property(x => x.Value).HasMaxLength(200).IsRequired();

        b.Entity<Person>()
            .HasMany(x => x.ContactInfos)
            .WithOne(x => x.Person)
            .HasForeignKey(x => x.PersonUUID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
