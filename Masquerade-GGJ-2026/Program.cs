using Masquerade_GGJ_2026.Notifiers;
using Masquerade_GGJ_2026.Factories;
using Masquerade_GGJ_2026.Repositories;

var builder = WebApplication.CreateBuilder(args);
// Add services to the cont
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        string[] allowedOrigins = new string[] { "http://localhost:4200", "https://gatecrasher.mufinek.pl"};
        foreach(var domain in allowedOrigins)
            policy.WithOrigins(domain)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
}); 

builder.Services.AddScoped<PlayerFactory>();

builder.Services.AddScoped<PlayerNotifier>();
builder.Services.AddSingleton<GameNotifier>();
builder.Services.AddSingleton<RoomSelectionNotifier>();

builder.Services.AddScoped<GameOrchestrator>();
builder.Services.AddScoped<GameRoomOrchestrator>();

builder.Services.AddSingleton<IGameStore, MemoryGameStore>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapControllers();

// Mapuj hub Game
app.MapHub<Masquerade_GGJ_2026.Hubs.GameHub>("/hubs/game");
app.MapHub<Masquerade_GGJ_2026.Hubs.GameHub>("/hubs/rooms");

app.Run();
