using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;

namespace BusBooking.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddBusBookingSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Bus Booking API",
                Version = "v1",
                Description =
                    "ASP.NET Core API for the Bus Booking System. Use **Public** for anonymous endpoints, " +
                    "or authorize with JWT (roles: User, Operator, Admin). Click **Authorize** and enter `Bearer {token}`."
            });

            c.TagActionsBy(api => new[] { InferRoleTag(api) });
            c.OrderActionsBy(api => $"{InferRoleTag(api)}_{api.RelativePath}");

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste: Bearer {your_access_token}"
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    private static string InferRoleTag(ApiDescription api)
    {
        var md = api.ActionDescriptor?.EndpointMetadata;
        if (md == null)
            return "Other";

        if (md.OfType<AllowAnonymousAttribute>().Any())
            return "Public";

        var auth = md.OfType<AuthorizeAttribute>().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(auth?.Roles))
            return "Authenticated";

        var roles = auth!.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (roles.Contains("Admin"))
            return "Admin";
        if (roles.Contains("Operator"))
            return "Operator";
        if (roles.Contains("User"))
            return "User";
        return string.Join(", ", roles);
    }
}
