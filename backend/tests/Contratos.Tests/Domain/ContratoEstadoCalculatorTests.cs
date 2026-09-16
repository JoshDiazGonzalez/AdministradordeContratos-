using Contratos.Domain.Enums;
using Contratos.Domain.Services;

namespace Contratos.Tests.Domain;

/// <summary>
/// Reglas de vigencia. Se usa una fecha fija para que los tests no dependan
/// del dia en que se ejecuten.
/// </summary>
public class ContratoEstadoCalculatorTests
{
    private static readonly DateOnly Hoy = new(2026, 6, 15);

    [Fact]
    public void Vencimiento_dentro_de_10_dias_es_PorVencer()
    {
        var estado = ContratoEstadoCalculator.Calcular(
            inactivo: false, fechaVencimiento: Hoy.AddDays(10), hoy: Hoy);

        Assert.Equal(ContratoEstado.PorVencer, estado);
    }

    [Fact]
    public void Vencimiento_pasado_es_Vencido()
    {
        var estado = ContratoEstadoCalculator.Calcular(
            inactivo: false, fechaVencimiento: Hoy.AddDays(-1), hoy: Hoy);

        Assert.Equal(ContratoEstado.Vencido, estado);
    }

    [Fact]
    public void Vencimiento_lejano_es_Activo()
    {
        var estado = ContratoEstadoCalculator.Calcular(
            inactivo: false, fechaVencimiento: Hoy.AddDays(180), hoy: Hoy);

        Assert.Equal(ContratoEstado.Activo, estado);
    }

    [Fact]
    public void Inactivo_prevalece_sobre_el_calculo_de_fechas()
    {
        // Aunque este vencido por fechas, la desactivacion manual manda.
        var estado = ContratoEstadoCalculator.Calcular(
            inactivo: true, fechaVencimiento: Hoy.AddDays(-100), hoy: Hoy);

        Assert.Equal(ContratoEstado.Inactivo, estado);
    }

    [Fact]
    public void Vencimiento_hoy_es_PorVencer_no_Vencido()
    {
        // El limite exacto: vence hoy, todavia no esta vencido.
        var estado = ContratoEstadoCalculator.Calcular(
            inactivo: false, fechaVencimiento: Hoy, hoy: Hoy);

        Assert.Equal(ContratoEstado.PorVencer, estado);
    }

    [Theory]
    [InlineData(30, ContratoEstado.PorVencer)]  // ultimo dia dentro de la ventana
    [InlineData(31, ContratoEstado.Activo)]     // primer dia fuera de la ventana
    public void Frontera_de_los_30_dias(int diasHastaVencimiento, ContratoEstado esperado)
    {
        var estado = ContratoEstadoCalculator.Calcular(
            inactivo: false, fechaVencimiento: Hoy.AddDays(diasHastaVencimiento), hoy: Hoy);

        Assert.Equal(esperado, estado);
    }

    [Fact]
    public void Contrato_que_aun_no_inicia_se_considera_Activo()
    {
        // Ambiguedad documentada: el enunciado no define un estado "pendiente de
        // inicio", asi que un contrato futuro con vencimiento lejano es Activo.
        var estado = ContratoEstadoCalculator.Calcular(
            inactivo: false, fechaVencimiento: Hoy.AddDays(400), hoy: Hoy);

        Assert.Equal(ContratoEstado.Activo, estado);
    }
}
