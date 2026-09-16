using Contratos.Domain.Entities;
using Contratos.Domain.Enums;

namespace Contratos.Application.Contratos;

/// <summary>
/// Contrato tal como lo ve el cliente. El estado llega ya calculado por el
/// backend: el frontend nunca reaplica las reglas de vigencia.
/// </summary>
public record ContratoDto(
    Guid Id,
    string NombreProveedor,
    decimal MontoContrato,
    DateOnly FechaInicio,
    DateOnly FechaVencimiento,
    ContratoEstado Estado,
    string Descripcion,
    string ArchivoNombre,
    string ArchivoContentType,
    long ArchivoTamanoBytes,
    DateTimeOffset FechaCreacion,
    DateTimeOffset? FechaActualizacion)
{
    public static ContratoDto Desde(Contrato contrato, DateOnly hoy) => new(
        contrato.Id,
        contrato.NombreProveedor,
        contrato.MontoContrato,
        contrato.FechaInicio,
        contrato.FechaVencimiento,
        contrato.EstadoEn(hoy),
        contrato.Descripcion,
        contrato.ArchivoNombre,
        contrato.ArchivoContentType,
        contrato.ArchivoTamanoBytes,
        contrato.FechaCreacion,
        contrato.FechaActualizacion);
}
