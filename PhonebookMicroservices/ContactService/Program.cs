using ContactService.Data;
using Microsoft.EntityFrameworkCore;

namespace ContactService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // PostgreSQL bağlantısı
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

            // Controller tabanlı API
            builder.Services.AddControllers();

            // Swagger (API dokümantasyonu)
            builder.Services.AddEndpointsApiExplorer(); 

            var app = builder.Build();

            // Migrationları otomatik uygula
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

     

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
