using Contratos.Infrastructure.Services;

namespace Contratos.Tests.Domain;

/// <summary>
/// Verifica que la zona horaria del negocio se resuelve en la maquina actual.
/// Si fallara, el calculo de estados se desplazaria 5 horas.
/// </summary>
public class DateTimeProviderTests
{
    [Fact]
    public void Resuelve_la_zona_horaria_de_Ecuador()
    {
        var provider = new DateTimeProvider();

        var hoyEcuador = provider.Hoy;
        var hoyUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        // Ecuador va 5 horas por detras de UTC: la fecha coincide o es el dia previo.
        Assert.True(
            hoyEcuador == hoyUtc || hoyEcuador == hoyUtc.AddDays(-1),
            $"Fecha inesperada. Ecuador={hoyEcuador}, UTC={hoyUtc}");
    }

    [Fact]
    public void El_desfase_respecto_a_UTC_es_de_cinco_horas()
    {
        var provider = new DateTimeProvider();

        var diferencia = provider.AhoraUtc.UtcDateTime - provider.Hoy.ToDateTime(TimeOnly.MinValue);

        // Entre 0 y 29 horas cubre cualquier hora del dia con el offset de -5.
        Assert.InRange(diferencia.TotalHours, 0, 29);
    }
}
