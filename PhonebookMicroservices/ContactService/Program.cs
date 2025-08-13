using Microsoft.EntityFrameworkCore;
using ContactService.Data;
using ContactService.Models;

namespace ContactService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ConnectionString: önce appsettings, yoksa Docker ENV (ConnectionStrings__Postgres)
            var conn = builder.Configuration.GetConnectionString("Postgres")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");

            builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

            var app = builder.Build();

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            var people = app.MapGroup("/api/people");

            // Kiþi oluþtur
            people.MapPost("/", async (AppDbContext db, Person p) =>
            {
                db.Persons.Add(p);
                await db.SaveChangesAsync();
                return Results.Created($"/api/people/{p.UUID}", p);
            });

            // Kiþi sil
            people.MapDelete("/{id:guid}", async (AppDbContext db, Guid id) =>
            {
                var person = await db.Persons.FindAsync(id);
                if (person is null) return Results.NotFound();
                db.Persons.Remove(person);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // Kiþileri listele
            people.MapGet("/", async (AppDbContext db) =>
                await db.Persons.AsNoTracking().Select(x => new {
                    x.UUID,
                    x.FirstName,
                    x.LastName,
                    x.Company
                }).ToListAsync());

            // Kiþi detay (+ iletiþim bilgileri)
            people.MapGet("/{id:guid}", async (AppDbContext db, Guid id) =>
            {
                var person = await db.Persons
                    .Include(x => x.ContactInfos)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UUID == id);
                return person is null ? Results.NotFound() : Results.Ok(person);
            });

            // Ýletiþim bilgisi ekle
            people.MapPost("/{id:guid}/contacts", async (AppDbContext db, Guid id, ContactInfo info) =>
            {
                var person = await db.Persons.FindAsync(id);
                if (person is null) return Results.NotFound();

                info.PersonUUID = person.UUID;
                db.ContactInfos.Add(info);
                await db.SaveChangesAsync();
                return Results.Created($"/api/people/{id}/contacts/{info.UUID}", info);
            });

            // Ýletiþim bilgisi sil
            people.MapDelete("/{id:guid}/contacts/{infoId:guid}", async (AppDbContext db, Guid id, Guid infoId) =>
            {
                var info = await db.ContactInfos.FirstOrDefaultAsync(x => x.UUID == infoId && x.PersonUUID == id);
                if (info is null) return Results.NotFound();
                db.ContactInfos.Remove(info);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // Tüm kiþileri iletiþim bilgileriyle ver (rapor için)
            people.MapGet("/full", async (AppDbContext db) =>
            {
                var data = await db.Persons
                    .Include(p => p.ContactInfos)
                    .AsNoTracking()
                    .Select(p => new {
                        p.UUID,
                        p.FirstName,
                        p.LastName,
                        p.Company,
                        ContactInfos = p.ContactInfos.Select(ci => new {
                            ci.UUID,
                            Type = ci.Type.ToString(), // Worker string bekliyor
                            ci.Value
                        }).ToList()
                    })
                    .ToListAsync();

                return Results.Ok(data);
            });


            app.Run();
        }
    }
}
