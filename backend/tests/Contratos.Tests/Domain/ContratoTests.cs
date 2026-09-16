using Contratos.Domain.Entities;

namespace Contratos.Tests.Domain;

/// <summary>
/// Invariantes de la entidad Contrato.
/// </summary>
public class ContratoTests
{
    private static readonly DateOnly Inicio = new(2026, 1, 1);
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Contrato CrearValido(
        decimal monto = 12_500.00m,
        DateOnly? fechaInicio = null,
        DateOnly? fechaVencimiento = null) =>
        Contrato.Crear(
            nombreProveedor: "Proveedor Alpha",
            montoContrato: monto,
            fechaInicio: fechaInicio ?? Inicio,
            fechaVencimiento: fechaVencimiento ?? Inicio.AddYears(1),
            descripcion: "Contrato de servicios de software.",
            archivoNombre: "contrato.pdf",
            archivoRuta: "contratos/2026/01/abc123.pdf",
            archivoContentType: "application/pdf",
            archivoTamanoBytes: 1024,
            ahoraUtc: Ahora);

    [Fact]
    public void Monto_cero_es_invalido()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CrearValido(monto: 0m));
        Assert.Equal("montoContrato", ex.ParamName);
    }

    [Fact]
    public void Monto_negativo_es_invalido()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrearValido(monto: -1m));
    }

    [Fact]
    public void Fecha_vencimiento_anterior_a_inicio_es_invalida()
    {
        var ex = Assert.Throws<ArgumentException>(() => CrearValido(
            fechaInicio: Inicio,
            fechaVencimiento: Inicio.AddDays(-1)));

        Assert.Equal("fechaVencimiento", ex.ParamName);
    }

    [Fact]
    public void Fecha_vencimiento_igual_a_inicio_es_valida()
    {
        var contrato = CrearValido(fechaInicio: Inicio, fechaVencimiento: Inicio);

        Assert.Equal(Inicio, contrato.FechaVencimiento);
    }

    [Fact]
    public void Contrato_nuevo_nace_activo_y_con_fecha_de_creacion()
    {
        var contrato = CrearValido();

        Assert.False(contrato.Inactivo);
        Assert.Equal(Ahora, contrato.FechaCreacion);
        Assert.Null(contrato.FechaActualizacion);
    }

    [Fact]
    public void El_id_generado_es_un_UUID_version_7()
    {
        var contrato = CrearValido();

        // El nibble de version esta en el digito 13 de la representacion hexadecimal.
        Assert.Equal('7', contrato.Id.ToString("N")[12]);
    }

    [Fact]
    public async Task Los_ids_generados_son_crecientes_en_el_tiempo()
    {
        // Propiedad clave de UUIDv7: los primeros 48 bits son la marca de tiempo en
        // milisegundos, y son crecientes. Eso da localidad de indice y evita la
        // fragmentacion de UUIDv4. Dentro de un mismo milisegundo el resto es
        // aleatorio, asi que se espacian las generaciones para comparar marcas.
        var prefijos = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            prefijos.Add(CrearValido().Id.ToString("N")[..12]);
            await Task.Delay(2);
        }

        Assert.Equal(prefijos.OrderBy(p => p, StringComparer.Ordinal), prefijos);
        Assert.True(prefijos.Distinct().Count() > 1, "Las marcas de tiempo no avanzaron.");
    }

    [Fact]
    public void Cambiar_inactivo_marca_fecha_de_actualizacion()
    {
        var contrato = CrearValido();
        var despues = Ahora.AddHours(1);

        contrato.CambiarInactivo(inactivo: true, ahoraUtc: despues);

        Assert.True(contrato.Inactivo);
        Assert.Equal(despues, contrato.FechaActualizacion);
    }

    [Fact]
    public void Actualizar_con_monto_invalido_no_modifica_la_entidad()
    {
        var contrato = CrearValido();

        Assert.Throws<ArgumentOutOfRangeException>(() => contrato.Actualizar(
            nombreProveedor: "Otro",
            montoContrato: 0m,
            fechaInicio: Inicio,
            fechaVencimiento: Inicio.AddYears(1),
            descripcion: "Nueva descripcion",
            ahoraUtc: Ahora));

        Assert.Equal("Proveedor Alpha", contrato.NombreProveedor);
        Assert.Null(contrato.FechaActualizacion);
    }
}
