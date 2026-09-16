namespace Contratos.Domain.Services;

/// <summary>
/// Abstrae el reloj del sistema.
///
/// Existe por dos razones concretas:
///  - El servidor corre en UTC (Supabase tambien), pero el negocio es ecuatoriano
///    (UTC-5). Sin esto, un contrato que vence hoy se marcaria vencido 5 horas antes.
///  - Permite tests deterministas de las reglas de estado sin depender del dia real.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Fecha actual en la zona horaria del negocio (America/Guayaquil).</summary>
    DateOnly Hoy { get; }

    /// <summary>Instante actual en UTC, para marcas de auditoria.</summary>
    DateTimeOffset AhoraUtc { get; }
}
