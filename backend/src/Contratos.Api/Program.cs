using Contratos.Api.Extensions;
using Contratos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Permite sobrescribir cualquier valor de appsettings con variables de entorno,
// usando el separador "__" (ej. ConnectionStrings__DefaultConnection).
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "API de Administracion de Contratos",
        Version = "v1",
        Description = "Administracion y control de vigencia de contratos de proveedores."
    });
});

// Acceso a datos (EF Core + PostgreSQL/Supabase).
builder.Services.AddInfrastructure(builder.Configuration);

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Contratos API v1"));
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

// Endpoint de diagnostico para verificar que la API responde (y para el healthcheck de Docker).
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }))
   .WithName("Health")
   .WithTags("Diagnostico");

app.Run();
