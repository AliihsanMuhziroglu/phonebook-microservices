using System.Net.Http.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using ReportService.Data;
using ReportService.Models;

namespace ReportService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ---- Postgres ----
            var conn = builder.Configuration.GetConnectionString("Postgres")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                       ?? "Host=localhost;Port=5432;Database=phonebookdb;Username=phonebook;Password=phonebookpwd";
            builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

            // ---- ContactService HTTP client ----
            var contactBase = builder.Configuration["Services:ContactBaseUrl"]
                             ?? Environment.GetEnvironmentVariable("Services__ContactBaseUrl")
                             ?? "http://localhost:8081";
            builder.Services.AddHttpClient("contacts", c => c.BaseAddress = new Uri(contactBase));

            // ---- Kafka ----
            var bootstrap = builder.Configuration["Kafka:BootstrapServers"]
                            ?? Environment.GetEnvironmentVariable("Kafka__BootstrapServers")
                            ?? "localhost:9092";
            builder.Services.AddSingleton(new ProducerConfig { BootstrapServers = bootstrap });
            builder.Services.AddSingleton(new ConsumerConfig
            {
                BootstrapServers = bootstrap,
                GroupId = "report-workers",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            });

            // ---- Worker (ENV ile kontrol) ----
            var enableWorker = (Environment.GetEnvironmentVariable("ENABLE_WORKER") ?? "false")
                               .Equals("true", StringComparison.OrdinalIgnoreCase);
            if (enableWorker)
                builder.Services.AddHostedService<ReportWorker>();

            var app = builder.Build();

            // Basit log
            app.Logger.LogInformation("ReportService started. ENABLE_WORKER={Enable}", enableWorker);
            app.Logger.LogInformation("ContactBase={Base}", contactBase);
            app.Logger.LogInformation("DB={Conn}", conn.Contains("postgres") ? "docker-postgres" : "local");

            // Health
            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            // Rapor talebi: DB kaydet + Kafka'ya istek
            app.MapPost("/api/reports/request", async (AppDbContext db, ProducerConfig pc) =>
            {
                var report = new Report(); // Preparing
                db.Reports.Add(report);
                await db.SaveChangesAsync();

                using var producer = new ProducerBuilder<string, string>(pc).Build();
                await producer.ProduceAsync("report-requests",
                    new Message<string, string> { Key = report.UUID.ToString(), Value = report.UUID.ToString() });

                return Results.Accepted($"/api/reports/{report.UUID}", new { report.UUID, report.RequestDate, report.Status });
            });

            // Liste
            app.MapGet("/api/reports", async (AppDbContext db) =>
                await db.Reports.AsNoTracking()
                    .Select(r => new { r.UUID, r.RequestDate, r.Status })
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync());

            // Detay
            app.MapGet("/api/reports/{id:guid}", async (AppDbContext db, Guid id) =>
            {
                var report = await db.Reports.Include(r => r.Items)
                    .AsNoTracking().FirstOrDefaultAsync(r => r.UUID == id);
                return report is null ? Results.NotFound() : Results.Ok(report);
            });

            app.Run();
        }
    }

    // ===== Worker (retry + log) =====
    public class ReportWorker : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ConsumerConfig _cc;
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ReportWorker> _logger;

        public ReportWorker(IServiceProvider sp, ConsumerConfig cc, IHttpClientFactory http, ILogger<ReportWorker> logger)
            => (_sp, _cc, _http, _logger) = (sp, cc, http, logger);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                IConsumer<string, string>? consumer = null;

                try
                {
                    consumer = new ConsumerBuilder<string, string>(_cc).Build();
                    consumer.Subscribe("report-requests");
                    _logger.LogInformation("Kafka subscribed to 'report-requests'.");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var cr = consumer.Consume(TimeSpan.FromMilliseconds(500));
                            if (cr == null) continue;

                            _logger.LogInformation("Message: {Val}", cr.Message.Value);

                            if (!Guid.TryParse(cr.Message.Value, out var reportId))
                                continue;

                            using var scope = _sp.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            var client = _http.CreateClient("contacts");

                            var people = await client.GetFromJsonAsync<List<PersonDto>>("/api/people/full", cancellationToken: stoppingToken) ?? new();

                            var groups = people
                                .Select(p => new
                                {
                                    p.UUID,
                                    Locations = p.ContactInfos.Where(ci => ci.Type == "Location").Select(ci => ci.Value).Distinct().ToList(),
                                    PhoneCount = p.ContactInfos.Count(ci => ci.Type == "Phone")
                                })
                                .SelectMany(p => p.Locations.Select(loc => new { loc, p.UUID, p.PhoneCount }))
                                .GroupBy(x => x.loc)
                                .Select(g => new ReportItem
                                {
                                    ReportUUID = reportId,
                                    Location = g.Key,
                                    PersonCount = g.Select(x => x.UUID).Distinct().Count(),
                                    PhoneCount = g.Sum(x => x.PhoneCount)
                                })
                                .ToList();

                            var report = await db.Reports.FirstOrDefaultAsync(r => r.UUID == reportId, stoppingToken);
                            if (report != null)
                            {
                                db.ReportItems.RemoveRange(db.ReportItems.Where(i => i.ReportUUID == reportId));
                                await db.SaveChangesAsync(stoppingToken);

                                db.ReportItems.AddRange(groups);
                                report.Status = ReportStatus.Completed;
                                await db.SaveChangesAsync(stoppingToken);
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[Worker-Process] error");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Worker-Connect] error. Retry in 2s");
                    await Task.Delay(2000, stoppingToken);
                }
                finally
                {
                    try { consumer?.Close(); } catch { }
                    consumer?.Dispose();
                }
            }
        }

        private record PersonDto(Guid UUID, string FirstName, string LastName, string? Company, List<ContactInfoDto> ContactInfos);
        private record ContactInfoDto(Guid UUID, string Type, string Value);
    }
}
