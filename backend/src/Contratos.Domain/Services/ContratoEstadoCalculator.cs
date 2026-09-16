using Contratos.Domain.Enums;

namespace Contratos.Domain.Services;

/// <summary>
/// Unica fuente de verdad de las reglas de vigencia.
///
/// El estado NO se almacena en la base de datos: se deriva de las fechas en cada
/// consulta. Un estado persistido quedaria obsoleto cada medianoche y obligaria a
/// un proceso de recalculo. Lo unico que se persiste es la bandera "inactivo",
/// que es una decision explicita de negocio y no depende del calendario.
///
/// El frontend nunca recalcula: recibe el estado ya resuelto por el backend.
/// </summary>
public static class ContratoEstadoCalculator
{
    /// <summary>Ventana de aviso previo al vencimiento, en dias.</summary>
    public const int DiasAvisoVencimiento = 30;

    /// <summary>
    /// Determina el estado de vigencia de un contrato.
    /// </summary>
    /// <param name="inactivo">Bandera de desactivacion manual.</param>
    /// <param name="fechaVencimiento">Fecha de vencimiento del contrato.</param>
    /// <param name="hoy">Fecha actual en la zona horaria del negocio.</param>
    public static ContratoEstado Calcular(bool inactivo, DateOnly fechaVencimiento, DateOnly hoy)
    {
        // La desactivacion manual prevalece sobre cualquier calculo de fechas.
        if (inactivo)
        {
            return ContratoEstado.Inactivo;
        }

        if (fechaVencimiento < hoy)
        {
            return ContratoEstado.Vencido;
        }

        if (fechaVencimiento <= hoy.AddDays(DiasAvisoVencimiento))
        {
            return ContratoEstado.PorVencer;
        }

        // Incluye los contratos cuya fecha de inicio aun no llega: se consideran
        // vigentes. El requerimiento no define un estado "pendiente de inicio" y
        // no se inventa uno para no desviarse del enunciado.
        return ContratoEstado.Activo;
    }
}
