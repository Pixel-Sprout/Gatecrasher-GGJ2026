using Masquerade_GGJ_2026.Orchestrators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dodaj SignalR
builder.Services.AddSignalR();

// Prosta polityka CORS (przykład dla Angular dev servera)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
}); 

builder.Services.AddScoped<GameOrchestrator>();
builder.Services.AddScoped<GameNotifier>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapControllers();

// Mapuj hub Echo
app.MapHub<Masquerade_GGJ_2026.Hubs.EchoHub>("/hubs/echo");

// Mapuj hub Game
app.MapHub<Masquerade_GGJ_2026.Hubs.GameHub>("/hubs/game");

app.Run();
