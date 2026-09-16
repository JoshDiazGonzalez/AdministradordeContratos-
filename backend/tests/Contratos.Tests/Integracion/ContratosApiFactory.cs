using Contratos.Domain.Entities;
using Contratos.Infrastructure.Persistence;
using Contratos.Infrastructure.Services;

namespace Contratos.Tests.Integracion;

/// <summary>
/// API de pruebas con contratos que cubren los cuatro estados.
///
/// Las fechas se calculan respecto al "hoy" real del negocio, no a constantes:
/// de lo contrario los tests caducarian con el paso del tiempo.
/// </summary>
public class ContratosApiFactory : ApiFactory
{
    public const int TotalContratos = 7;

    protected override async Task SembrarDatosAsync(AppDbContext contexto)
    {
        if (contexto.Contratos.Any())
        {
            return;
        }

        var reloj = new DateTimeProvider();
        var hoy = reloj.Hoy;
        var ahora = reloj.AhoraUtc;
        var inicio = hoy.AddDays(-400);

        Contrato Crear(string proveedor, int diasHastaVencimiento) => Contrato.Crear(
            nombreProveedor: proveedor,
            montoContrato: 12_500.00m,
            fechaInicio: inicio,
            fechaVencimiento: hoy.AddDays(diasHastaVencimiento),
            descripcion: $"Contrato de {proveedor}.",
            archivoNombre: "contrato.pdf",
            archivoRuta: $"contratos/{Guid.CreateVersion7()}.pdf",
            archivoContentType: "application/pdf",
            archivoTamanoBytes: 2048,
            ahoraUtc: ahora);

        // Activo: vence mas alla de la ventana de 30 dias.
        var activoUno = Crear("Proveedor Alpha", 200);
        var activoDos = Crear("Proveedor Beta", 365);

        // Por vencer: dentro de los proximos 30 dias.
        var porVencerUno = Crear("Proveedor Gamma", 5);
        var porVencerDos = Crear("Proveedor Delta", 25);

        // Vencido: la fecha ya paso.
        var vencidoUno = Crear("Proveedor Epsilon", -10);
        var vencidoDos = Crear("Proveedor Zeta", -100);

        // Inactivo: vencido por fechas, pero desactivado por negocio.
        // Sirve para comprobar que la bandera prevalece sobre el calculo.
        var inactivo = Crear("Proveedor Eta", -50);
        inactivo.CambiarInactivo(inactivo: true, ahoraUtc: ahora);

        contexto.Contratos.AddRange(
            activoUno, activoDos,
            porVencerUno, porVencerDos,
            vencidoUno, vencidoDos,
            inactivo);

        await contexto.SaveChangesAsync();
    }
}
