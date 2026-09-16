namespace Contratos.Domain.Enums;

/// <summary>
/// Estado de vigencia de un contrato.
///
/// Nota de implementacion: el requerimiento original menciona "Vendido", que se
/// interpreta como un error tipografico de "Vencido". Se implementa Vencido.
/// </summary>
public enum ContratoEstado
{
    /// <summary>Vigente y sin vencimiento proximo.</summary>
    Activo = 1,

    /// <summary>Vence dentro de los proximos 30 dias.</summary>
    PorVencer = 2,

    /// <summary>La fecha de vencimiento ya paso.</summary>
    Vencido = 3,

    /// <summary>Desactivado explicitamente por negocio. Prevalece sobre las fechas.</summary>
    Inactivo = 4
}
