using Contratos.Domain.Enums;
using Contratos.Domain.Services;

namespace Contratos.Domain.Entities;

/// <summary>
/// Contrato de un proveedor, con su documento asociado.
/// </summary>
public class Contrato
{
    /// <summary>Constructor privado requerido por EF Core para materializar entidades.</summary>
    private Contrato()
    {
        NombreProveedor = string.Empty;
        Descripcion = string.Empty;
        ArchivoNombre = string.Empty;
        ArchivoRuta = string.Empty;
        ArchivoContentType = string.Empty;
    }

    public Guid Id { get; private set; }

    public string NombreProveedor { get; private set; }

    public decimal MontoContrato { get; private set; }

    public DateOnly FechaInicio { get; private set; }

    public DateOnly FechaVencimiento { get; private set; }

    /// <summary>
    /// Desactivacion manual por negocio. El resto de estados se derivan de las fechas,
    /// por eso esta bandera es lo unico que se persiste sobre el estado.
    /// </summary>
    public bool Inactivo { get; private set; }

    public string Descripcion { get; private set; }

    /// <summary>Nombre original del archivo, solo para mostrar y descargar.</summary>
    public string ArchivoNombre { get; private set; }

    /// <summary>Clave generada en el almacenamiento. Nunca el nombre original.</summary>
    public string ArchivoRuta { get; private set; }

    public string ArchivoContentType { get; private set; }

    public long ArchivoTamanoBytes { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public DateTimeOffset? FechaActualizacion { get; private set; }

    /// <summary>
    /// Estado de vigencia calculado. No se persiste: se resuelve en cada lectura.
    /// </summary>
    public ContratoEstado EstadoEn(DateOnly hoy) =>
        ContratoEstadoCalculator.Calcular(Inactivo, FechaVencimiento, hoy);

    /// <summary>
    /// Crea un contrato. Las validaciones de entrada del usuario viven en la capa
    /// de aplicacion; aqui solo se protegen los invariantes del dominio.
    /// </summary>
    public static Contrato Crear(
        string nombreProveedor,
        decimal montoContrato,
        DateOnly fechaInicio,
        DateOnly fechaVencimiento,
        string descripcion,
        string archivoNombre,
        string archivoRuta,
        string archivoContentType,
        long archivoTamanoBytes,
        DateTimeOffset ahoraUtc)
    {
        GuardarInvariantes(montoContrato, fechaInicio, fechaVencimiento);

        return new Contrato
        {
            // UUIDv7: ordenado por tiempo, evita la fragmentacion de indice que
            // produce UUIDv4 al insertar claves aleatorias.
            Id = Guid.CreateVersion7(),
            NombreProveedor = nombreProveedor.Trim(),
            MontoContrato = montoContrato,
            FechaInicio = fechaInicio,
            FechaVencimiento = fechaVencimiento,
            Descripcion = descripcion.Trim(),
            ArchivoNombre = archivoNombre,
            ArchivoRuta = archivoRuta,
            ArchivoContentType = archivoContentType,
            ArchivoTamanoBytes = archivoTamanoBytes,
            Inactivo = false,
            FechaCreacion = ahoraUtc
        };
    }

    /// <summary>Actualiza los datos de negocio. El documento se cambia aparte.</summary>
    public void Actualizar(
        string nombreProveedor,
        decimal montoContrato,
        DateOnly fechaInicio,
        DateOnly fechaVencimiento,
        string descripcion,
        DateTimeOffset ahoraUtc)
    {
        GuardarInvariantes(montoContrato, fechaInicio, fechaVencimiento);

        NombreProveedor = nombreProveedor.Trim();
        MontoContrato = montoContrato;
        FechaInicio = fechaInicio;
        FechaVencimiento = fechaVencimiento;
        Descripcion = descripcion.Trim();
        FechaActualizacion = ahoraUtc;
    }

    /// <summary>Reemplaza el documento asociado.</summary>
    public void ReemplazarArchivo(
        string archivoNombre,
        string archivoRuta,
        string archivoContentType,
        long archivoTamanoBytes,
        DateTimeOffset ahoraUtc)
    {
        ArchivoNombre = archivoNombre;
        ArchivoRuta = archivoRuta;
        ArchivoContentType = archivoContentType;
        ArchivoTamanoBytes = archivoTamanoBytes;
        FechaActualizacion = ahoraUtc;
    }

    /// <summary>Activa o desactiva el contrato explicitamente.</summary>
    public void CambiarInactivo(bool inactivo, DateTimeOffset ahoraUtc)
    {
        Inactivo = inactivo;
        FechaActualizacion = ahoraUtc;
    }

    private static void GuardarInvariantes(
        decimal montoContrato,
        DateOnly fechaInicio,
        DateOnly fechaVencimiento)
    {
        if (montoContrato <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(montoContrato), montoContrato, "El monto del contrato debe ser mayor que 0.");
        }

        if (fechaVencimiento < fechaInicio)
        {
            throw new ArgumentException(
                "La fecha de vencimiento no puede ser anterior a la fecha de inicio.",
                nameof(fechaVencimiento));
        }
    }
}
