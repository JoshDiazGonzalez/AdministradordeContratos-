using Contratos.Api.Extensions;
using Contratos.Api.Middleware;
using Contratos.Infrastructure;
using Contratos.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Permite sobrescribir cualquier valor de appsettings con variables de entorno,
// usando el separador "__" (ej. ConnectionStrings__DefaultConnection).
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Los estados viajan como texto ("PorVencer"), no como numero: el cliente
        // no deberia depender del valor ordinal del enum.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSwaggerConAutorizacion();

// Acceso a datos, autenticacion y servicios de aplicacion.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutenticacionJwt(builder.Configuration);

// CORS restringido al origen del frontend (configurable por entorno).
var frontendUrl = builder.Configuration["Cors:FrontendUrl"] ?? "http://localhost:4200";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(frontendUrl)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Traza el destino de la base sin exponer la contrasena.
// Se ejecuta una sola vez al arrancar, por eso se evalua de forma anticipada.
var destinoBaseDatos = builder.Configuration.GetConnectionString("DefaultConnection").Ofuscar();
LogMessages.ConexionConfigurada(app.Logger, destinoBaseDatos);

// Crea el usuario administrador inicial si no existe. Es idempotente.
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SembrarAsync();
}

// Primero en la cadena: captura cualquier excepcion de los middlewares siguientes.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Contratos API v1"));
}

app.UseCors("Frontend");

// El orden importa: primero se identifica quien es (authentication),
// despues si puede hacerlo (authorization).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint de diagnostico para verificar que la API responde (y para el healthcheck de Docker).
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }))
   .WithName("Health")
   .WithTags("Diagnostico");

app.Run();
