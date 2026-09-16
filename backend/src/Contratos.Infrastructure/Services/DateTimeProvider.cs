using Contratos.Domain.Services;

namespace Contratos.Infrastructure.Services;

/// <summary>
/// Reloj del sistema anclado a la zona horaria del negocio.
///
/// El identificador IANA "America/Guayaquil" funciona en Linux y, desde .NET 6,
/// tambien en Windows gracias a ICU. Aun asi se incluye el identificador de
/// Windows como respaldo y, como ultimo recurso, un offset fijo de -05:00:
/// Ecuador continental no aplica horario de verano, por lo que el offset es estable.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo ZonaEcuador = ResolverZonaEcuador();

    public DateOnly Hoy =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ZonaEcuador).DateTime);

    public DateTimeOffset AhoraUtc => DateTimeOffset.UtcNow;

    private static TimeZoneInfo ResolverZonaEcuador()
    {
        foreach (var id in new[] { "America/Guayaquil", "SA Pacific Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Se intenta el siguiente identificador.
            }
            catch (InvalidTimeZoneException)
            {
                // Se intenta el siguiente identificador.
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            id: "America/Guayaquil (respaldo)",
            baseUtcOffset: TimeSpan.FromHours(-5),
            displayName: "Ecuador (UTC-05:00)",
            standardDisplayName: "ECT");
    }
}
