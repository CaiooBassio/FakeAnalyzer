using InstaFakeAnalyzer.Data;
using InstaFakeAnalyzer.Services;
using InstaFakeAnalyzer.Services.Analyzers;
using InstaFakeAnalyzer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (databaseProvider.ToLowerInvariant())
    {
        case "sqlite":
            options.UseSqlite(connectionString);
            break;

        case "mysql":
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            break;

        case "postgres":
        case "postgresql":
        default:
            options.UseNpgsql(connectionString);
            break;
    }
});

builder.Services.AddControllers();

builder.Services.AddHttpClient<INoticiaAnalyzer, DeepSeekAnalyzer>(client =>
{
    var baseUrl = builder.Configuration["AI:DeepSeek:BaseUrl"] ?? builder.Configuration["DEEPSEEK_BASE_URL"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
});

builder.Services.AddScoped<NoticiasService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowAll");
app.MapControllers();

app.Run();