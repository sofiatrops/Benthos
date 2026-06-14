using Bep.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Bep.Api.Authentication;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configura la autenticación JWT contra Keycloak como IdP (ADR-003). La API
    /// actúa como Resource Server: valida emisor, audiencia, expiración y firma.
    /// </summary>
    public static IServiceCollection AddBepAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloak = configuration.GetSection("Keycloak");
        var authority = keycloak["Authority"];
        var audience = keycloak["Audience"] ?? "bep-api";

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                // En desarrollo se permite HTTP hacia Keycloak; en producción
                // siempre HTTPS (RNF-SEG-001).
                options.RequireHttpsMetadata = keycloak.GetValue("RequireHttpsMetadata", true);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    // Keycloak debe emitir un claim plano 'roles' (mapper de roles de
                    // realm) en el access token para que RequireRole funcione.
                    RoleClaimType = "roles",
                };
            });

        services.AddAuthorization();
        return services;
    }
}
