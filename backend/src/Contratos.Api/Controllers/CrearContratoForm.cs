using Contratos.Application.Contratos;
using Contratos.Application.Storage;

namespace Contratos.Api.Controllers;

/// <summary>
/// Enlace del formulario multipart. Vive en la capa de API porque IFormFile es
/// un tipo de ASP.NET Core; la capa de aplicacion recibe un ArchivoSubido neutro.
/// </summary>
public class CrearContratoForm
{
    public string NombreProveedor { get; set; } = string.Empty;

    public decimal MontoContrato { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public IFormFile? Archivo { get; set; }

    public CrearContratoRequest ARequest(Stream contenido) => new()
    {
        NombreProveedor = NombreProveedor,
        MontoContrato = MontoContrato,
        FechaInicio = FechaInicio,
        FechaVencimiento = FechaVencimiento,
        Descripcion = Descripcion,
        Archivo = Archivo is null
            ? null
            : new ArchivoSubido(
                Archivo.FileName,
                Archivo.ContentType,
                Archivo.Length,
                contenido)
    };
}
