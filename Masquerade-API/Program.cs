using Masquerade.Factories;
using Masquerade.Notifiers;
using Masquerade.Orchestrators;
using Masquerade.Repositories;
using Masquerade.Hubs;

var builder = WebApplication.CreateBuilder(args);
// Add services to the cont
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        string[] allowedOrigins = new string[] { "http://localhost:4200", "https://gatecrasher.mufinek.pl", "https://maska.mufinek.pl" };
        foreach(var domain in allowedOrigins)
        policy.WithOrigins(domain)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
}); 

builder.Services.AddScoped<PlayerFactory>();
builder.Services.AddScoped<PlayerNotifier>();
builder.Services.AddScoped<GameOrchestrator>();

builder.Services.AddSingleton<GameFactory>();
builder.Services.AddSingleton<GameNotifier>();
builder.Services.AddSingleton<IGameStore, MemoryGameStore>();
builder.Services.AddSingleton<IPlayersStore, PlayersStore>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapControllers();

// Mapuj hub Game
app.MapHub<GameHub>("/hubs/game");
app.MapHub<MainHub>("/hubs/main");

app.Run();
