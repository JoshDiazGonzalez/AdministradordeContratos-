using Contratos.Application.Contratos;
using Contratos.Domain.Enums;
using Contratos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Contratos.Tests.Integracion;

/// <summary>
/// Comprueba que los filtros se traducen a SQL y no se aplican en memoria.
///
/// Importa porque el estado no es una columna: si se filtrara en memoria habria
/// que traer la tabla completa, y el conteo total y la paginacion serian erroneos.
/// </summary>
public class ConsultaSqlTests : IClassFixture<ContratosApiFactory>
{
    private readonly ContratosApiFactory _factory;

    public ConsultaSqlTests(ContratosApiFactory factory) => _factory = factory;

    private string SqlDe(ContratoFiltro filtro)
    {
        using var scope = _factory.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repositorio = new ContratoRepository(contexto);

        return repositorio.ConsultaFiltrada(filtro, new DateOnly(2026, 6, 15)).ToQueryString();
    }

    /// <summary>
    /// Devuelve solo la clausula WHERE. Sin esto, las aserciones darian falsos
    /// positivos: los nombres de columna aparecen tambien en la lista del SELECT.
    /// </summary>
    private string WhereDe(ContratoFiltro filtro)
    {
        var sql = SqlDe(filtro);
        var indice = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        return indice < 0 ? string.Empty : sql[indice..];
    }

    [Fact]
    public void El_filtro_de_estado_Vencido_genera_un_WHERE_sobre_fecha_vencimiento()
    {
        var where = WhereDe(new ContratoFiltro { Estado = ContratoEstado.Vencido });

        Assert.NotEmpty(where);
        Assert.Contains("fecha_vencimiento", where, StringComparison.Ordinal);
        Assert.Contains("inactivo", where, StringComparison.Ordinal);
    }

    [Fact]
    public void El_filtro_de_estado_PorVencer_genera_un_rango_de_fechas()
    {
        var sql = SqlDe(new ContratoFiltro { Estado = ContratoEstado.PorVencer });

        // Dos comparaciones: limite inferior (hoy) y superior (hoy + 30 dias).
        var comparaciones = sql.Split("fecha_vencimiento").Length - 1;
        Assert.True(comparaciones >= 2, $"Se esperaban 2 comparaciones. SQL:\n{sql}");
    }

    [Fact]
    public void El_filtro_de_estado_Inactivo_no_toca_las_fechas()
    {
        var where = WhereDe(new ContratoFiltro { Estado = ContratoEstado.Inactivo });

        Assert.Contains("inactivo", where, StringComparison.Ordinal);
        Assert.DoesNotContain("fecha_vencimiento", where, StringComparison.Ordinal);
    }

    [Fact]
    public void El_filtro_de_proveedor_genera_una_busqueda_parcial_en_SQL()
    {
        var where = WhereDe(new ContratoFiltro { Proveedor = "alpha" });

        Assert.Contains("nombre_proveedor", where, StringComparison.Ordinal);
        // La busqueda parcial se construye en SQL. Cada motor la expresa distinto:
        // SQLite usa instr(), PostgreSQL usa strpos() o LIKE. Lo que importa es
        // que la condicion viaje a la base, no como la escriba cada proveedor.
        var esBusquedaParcial =
            where.Contains("instr", StringComparison.OrdinalIgnoreCase) ||
            where.Contains("strpos", StringComparison.OrdinalIgnoreCase) ||
            where.Contains("LIKE", StringComparison.OrdinalIgnoreCase) ||
            where.Contains("position", StringComparison.OrdinalIgnoreCase);

        Assert.True(esBusquedaParcial, $"No se genero busqueda parcial. WHERE: {where}");
    }

    [Fact]
    public void Los_filtros_combinados_generan_un_unico_WHERE_con_todas_las_condiciones()
    {
        var where = WhereDe(new ContratoFiltro
        {
            Proveedor = "alpha",
            Estado = ContratoEstado.Activo,
            FechaInicioDesde = new DateOnly(2026, 1, 1)
        });

        Assert.Contains("nombre_proveedor", where, StringComparison.Ordinal);
        Assert.Contains("fecha_inicio", where, StringComparison.Ordinal);
        Assert.Contains("fecha_vencimiento", where, StringComparison.Ordinal);
        Assert.Contains("inactivo", where, StringComparison.Ordinal);
    }

    [Fact]
    public void Sin_filtros_no_se_genera_WHERE()
    {
        var sql = SqlDe(new ContratoFiltro());

        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }
}
