using Contratos.Api.Filters;
using Contratos.Application.Common;
using Contratos.Application.Contratos;
using Contratos.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contratos.Api.Controllers;

[ApiController]
[Route("api/contratos")]
[Authorize]
public class ContratosController : ControllerBase
{
    private readonly IContratoService _contratos;

    /// <summary>
    /// Tope de la peticion completa. Es mayor que el limite del archivo (10 MB)
    /// para dar margen al resto de campos del formulario multipart.
    /// </summary>
    private const int TamanoMaximoPeticion = 12 * 1024 * 1024;

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

    /// <summary>Crea un contrato con su documento adjunto.</summary>
    /// <remarks>Se envia como multipart/form-data. El documento es obligatorio.</remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TamanoMaximoPeticion)]
    [LimitarTamanoPeticion(TamanoMaximoPeticion, "El documento supera el tamaño máximo permitido de 10 MB.")]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ContratoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ContratoDto>> Crear(
        [FromForm] CrearContratoForm formulario,
        CancellationToken cancellationToken)
    {
        await using var contenido = formulario.Archivo?.OpenReadStream() ?? Stream.Null;

        var request = formulario.ARequest(contenido);
        var creado = await _contratos.CrearAsync(request, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    /// <summary>
    /// Devuelve el documento del contrato.
    /// </summary>
    /// <param name="id">Identificador del contrato.</param>
    /// <param name="download">
    /// true fuerza la descarga; false (por defecto) permite visualizarlo en el navegador.
    /// </param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    [HttpGet("{id:guid}/archivo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archivo(
        Guid id,
        [FromQuery] bool download,
        CancellationToken cancellationToken)
    {
        var archivo = await _contratos.ObtenerArchivoAsync(id, cancellationToken);

        // Con download=false el navegador puede mostrar el PDF en linea.
        // El nombre se pasa a File() para que ASP.NET lo codifique de forma segura
        // en la cabecera Content-Disposition.
        return download
            ? File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo)
            : File(archivo.Contenido, archivo.ContentType);
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
