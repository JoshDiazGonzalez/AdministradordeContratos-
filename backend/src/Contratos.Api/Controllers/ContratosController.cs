using Contratos.Application.Common;
using Contratos.Application.Contratos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contratos.Api.Controllers;

[ApiController]
[Route("api/contratos")]
[Authorize]
public class ContratosController : ControllerBase
{
    private readonly IContratoService _contratos;

    public ContratosController(IContratoService contratos) => _contratos = contratos;

    /// <summary>
    /// Lista contratos con filtros combinables y paginacion.
    /// </summary>
    /// <remarks>
    /// Ejemplo: /api/contratos?proveedor=software&amp;estado=PorVencer&amp;page=1&amp;pageSize=10
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<ContratoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResultadoPaginado<ContratoDto>>> Listar(
        [FromQuery] ContratoFiltro filtro,
        CancellationToken cancellationToken)
    {
        var resultado = await _contratos.BuscarAsync(filtro, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>Conteo de contratos por estado, para el dashboard.</summary>
    [HttpGet("resumen")]
    [ProducesResponseType<ContratoResumenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ContratoResumenDto>> Resumen(CancellationToken cancellationToken)
    {
        var resumen = await _contratos.ResumirAsync(cancellationToken);
        return Ok(resumen);
    }

    /// <summary>Obtiene un contrato por su identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ContratoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContratoDto>> ObtenerPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var contrato = await _contratos.ObtenerPorIdAsync(id, cancellationToken);
        return Ok(contrato);
    }
}
