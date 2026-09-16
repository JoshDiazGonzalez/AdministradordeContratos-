using Contratos.Application.Storage;

namespace Contratos.Application.Contratos;

/// <summary>Datos de negocio para crear un contrato. El archivo va aparte.</summary>
public class CrearContratoRequest
{
    public string NombreProveedor { get; set; } = string.Empty;

    public decimal MontoContrato { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Documento adjunto. Obligatorio al crear.</summary>
    public ArchivoSubido? Archivo { get; set; }
}
