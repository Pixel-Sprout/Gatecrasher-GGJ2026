using Masquerade_GGJ_2026.Orchestrators;

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

builder.Services.AddScoped<GameOrchestrator>();
builder.Services.AddScoped<GameNotifier>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapControllers();

// Mapuj hub Game
app.MapHub<Masquerade_GGJ_2026.Hubs.GameHub>("/hubs/game");

app.Run();
