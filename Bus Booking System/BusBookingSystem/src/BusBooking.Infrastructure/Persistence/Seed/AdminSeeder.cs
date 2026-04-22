using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Persistence.Seed;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // Ensure DB is up to date
            await context.Database.MigrateAsync();

            // Seed Admin
            var adminEmail = configuration["AdminSettings:Email"] ?? "admin@busbooking.com";
            var adminPassword = configuration["AdminSettings:Password"] ?? "Admin@123456";

            var existingAdmin = await context.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail && u.Role == UserRole.Admin);

            if (existingAdmin == null)
            {
                var admin = new User
                {
                    FullName = "System Administrator",
                    Email = adminEmail.ToLower().Trim(),
                    PhoneNumber = "0000000000",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = UserRole.Admin,
                    IsActive = true
                };

                await context.Users.AddAsync(admin);
                logger.LogInformation("Admin user seeded: {Email}", adminEmail);
            }
            else
            {
                logger.LogInformation("Admin user already exists: {Email}", adminEmail);
            }

            // Seed default PlatformConfig if not exists
            var configExists = await context.PlatformConfigs.AnyAsync();
            if (!configExists)
            {
                var platformConfig = new PlatformConfig
                {
                    ConvenienceFeePercentage = 5.0m,
                    SeatLockDurationMinutes = 10,
                    UpdatedByAdminId = "system"
                };

                await context.PlatformConfigs.AddAsync(platformConfig);
                logger.LogInformation("Default platform config seeded.");
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
