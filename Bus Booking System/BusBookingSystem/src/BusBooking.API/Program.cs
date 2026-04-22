using BusBooking.API.Extensions;
using BusBooking.Application;
using BusBooking.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Application & Infrastructure ─────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Auth ──────────────────────────────────────────────────────
builder.Services.AddJwtAuthentication(builder.Configuration);

// ── Controllers ───────────────────────────────────────────────
builder.Services.AddControllers();

// ── SignalR ───────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ── Swagger ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "Bus Booking API v1");
    });
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Seed Admin & Default Config ───────────────────────────────
await BusBooking.Infrastructure.Persistence.Seed.AdminSeeder.SeedAsync(app.Services);

app.Run();