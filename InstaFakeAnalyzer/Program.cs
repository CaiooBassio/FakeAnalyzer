using InstaFakeAnalyzer.Data;
using InstaFakeAnalyzer.Services;
using InstaFakeAnalyzer.Services.Analyzers;
using InstaFakeAnalyzer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddSingleton<InstagramMessageDeduplicator>();
builder.Services.AddSingleton<InstagramMessageQueue>();
builder.Services.AddHostedService(serviceProvider =>
    serviceProvider.GetRequiredService<InstagramMessageQueue>());

builder.Services.AddHttpClient("InstagramInternalApi", client =>
{
    var apiUrl = builder.Configuration["ApiUrl"] ?? builder.Configuration["Urls:ApiUrl"];
    if (!string.IsNullOrWhiteSpace(apiUrl))
        client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddHttpClient("InstagramGraphApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<DeepSeekAnalyzer>(client =>
{
    var baseUrl = builder.Configuration["AI:DeepSeek:BaseUrl"] ?? builder.Configuration["DEEPSEEK_BASE_URL"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<GeminiAnalyzer>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient<IClaimResearcher, BraveSearchClaimResearcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<INoticiaAnalyzer>(serviceProvider =>
{
    var primaryProvider = builder.Configuration["AI:Provider"] ?? "DeepSeek";
    var fallbackProviders = builder.Configuration
        .GetSection("AI:FallbackProviders")
        .Get<string[]>() ?? [];
    var providerNames = new[] { primaryProvider }.Concat(fallbackProviders)
        .Select(name => name.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var providers = providerNames.Select(name => name.ToUpperInvariant() switch
    {
        "DEEPSEEK" => (Name: name, Analyzer: (INoticiaAnalyzer)serviceProvider.GetRequiredService<DeepSeekAnalyzer>()),
        "GEMINI" => (Name: name, Analyzer: (INoticiaAnalyzer)serviceProvider.GetRequiredService<GeminiAnalyzer>()),
        _ => throw new InvalidOperationException(
            $"Provedor de IA '{name}' não reconhecido. Use 'DeepSeek' ou 'Gemini'.")
    });

    return new FailoverNoticiaAnalyzer(
        providers,
        serviceProvider.GetRequiredService<ILogger<FailoverNoticiaAnalyzer>>(),
        serviceProvider.GetRequiredService<IClaimResearcher>());
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