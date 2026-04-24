using BusBooking.API.Extensions;
using BusBooking.API.Filters;
using BusBooking.API.Hubs;
using BusBooking.API.Middleware;
using BusBooking.Application;
using BusBooking.Infrastructure;
using BusBooking.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddScoped<ApiDtoValidationFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<ApiDtoValidationFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddBusBookingSwagger();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "db", "ready" });

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagContext, httpContext) =>
    {
        if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var cid) && cid is string s)
            diagContext.Set("CorrelationId", s);
    };
});

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bus Booking API v1");
        c.DocumentTitle = "Bus Booking API";
    });
}

app.UseCors("AllowAngular");
// Avoid redirecting the Angular dev client (http://localhost:4200 → API on http://5153) to HTTPS when no HTTPS URL is in use.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// This host is the REST API only (no SPA). Browsers hitting http://localhost:5153/ otherwise see 404.
if (app.Environment.IsDevelopment())
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
else
    app.MapGet("/", () => Results.Text("Bus Booking API — JSON under /api/…  Health: GET /health", "text/plain")).AllowAnonymous();

app.MapControllers();
app.MapHub<SeatHub>("/hubs/seats");

app.MapHealthChecks("/health");

await BusBooking.Infrastructure.Persistence.Seed.AdminSeeder.SeedAsync(app.Services);

app.Run();