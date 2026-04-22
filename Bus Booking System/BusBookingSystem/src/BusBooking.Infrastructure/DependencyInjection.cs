using BusBooking.Application.Interfaces;
using BusBooking.Domain.Interfaces;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Persistence.Repositories;
using BusBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Unit of Work + Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISeatLockService, SeatLockService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}