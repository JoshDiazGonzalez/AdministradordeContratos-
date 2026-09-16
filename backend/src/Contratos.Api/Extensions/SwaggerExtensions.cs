using Microsoft.OpenApi;

namespace Contratos.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConAutorizacion(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API de Administracion de Contratos",
                Version = "v1",
                Description = "Administracion y control de vigencia de contratos de proveedores."
            });

            // Permite pegar el JWT desde la UI de Swagger y probar endpoints protegidos.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Pegue unicamente el token devuelto por /api/auth/login."
            });

            // En Swashbuckle 10 el requisito se construye con el documento ya
            // resuelto, para que la referencia apunte al esquema registrado arriba.
            options.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = []
            });
        });

        return services;
    }
}
