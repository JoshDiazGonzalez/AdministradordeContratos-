using System.Globalization;
using Contratos.Api.Extensions;
using Contratos.Api.Middleware;
using Contratos.Infrastructure;
using Contratos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// En desarrollo se carga el archivo .env de la raiz del repositorio antes de
// construir la configuracion. Asi basta con copiar .env.example a .env y
// ejecutar; no hace falta exportar variables a mano ni tocar el IDE.
// En produccion las variables las inyecta el entorno y esto no se ejecuta.
string? archivoEnv = null;
if (builder.Environment.IsDevelopment())
{
    archivoEnv = EnvFileLoader.Cargar(builder.Environment.ContentRootPath);
}

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

// En desarrollo, si no hay secreto JWT configurado se genera uno aleatorio en
// memoria para que la aplicacion arranque sin configurar nada. Cambia en cada
// ejecucion (las sesiones no sobreviven a un reinicio) y nunca se escribe a
// disco, asi que no puede filtrarse al repositorio.
if (builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Secret"]))
{
    builder.Configuration["Jwt:Secret"] =
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
}

// Acceso a datos, autenticacion y servicios de aplicacion.
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
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

// Cultura invariante para toda la API.
//
// El binding de modelos usa la cultura del servidor: en una maquina con locale
// espanol, "15000.50" se interpreta como 1500050 porque el punto se toma como
// separador de miles. Un contrato quedaria registrado por cien veces su valor.
// Una API REST debe interpretar numeros y fechas igual en cualquier servidor.
var localizacion = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = [CultureInfo.InvariantCulture],
    SupportedUICultures = [CultureInfo.InvariantCulture]
};
app.UseRequestLocalization(localizacion);

// Traza el destino de la base sin exponer la contrasena.
// Se ejecuta una sola vez al arrancar, por eso se evalua de forma anticipada.
var destinoBaseDatos = builder.Configuration.GetConnectionString("DefaultConnection").Ofuscar();
LogMessages.ConexionConfigurada(app.Logger, destinoBaseDatos);

if (archivoEnv is not null)
{
    LogMessages.ArchivoEnvCargado(app.Logger, archivoEnv);
}

// Prepara el esquema y crea el usuario administrador. Ambas cosas son idempotentes.
using (var scope = app.Services.CreateScope())
{
    var proveedor = scope.ServiceProvider.GetRequiredService<ProveedorDeDatos>();

    if (!proveedor.UsaPostgreSql)
    {
        // Sin PostgreSQL configurado se usa una base local de desarrollo. El
        // esquema se crea desde el modelo de EF porque las migraciones
        // versionadas contienen SQL especifico de PostgreSQL.
        var contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await contexto.Database.EnsureCreatedAsync();

        LogMessages.BaseLocalEnUso(app.Logger, proveedor.RutaBaseLocal!);
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SembrarAsync();

    // Contratos ficticios para demostrar los cuatro estados. Solo si
    // Seed__DatosDemo=true y la tabla esta vacia.
    var demo = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await demo.SembrarAsync();
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
