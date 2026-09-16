using Contratos.Application.Common;
using Contratos.Domain.Entities;

namespace Contratos.Application.Contratos;

public interface IContratoRepository
{
    Task<ResultadoPaginado<Contrato>> BuscarAsync(
        ContratoFiltro filtro,
        DateOnly hoy,
        CancellationToken cancellationToken);

    Task<Contrato?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ContratoResumenDto> ResumirAsync(DateOnly hoy, CancellationToken cancellationToken);

    Task AgregarAsync(Contrato contrato, CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene el contrato con seguimiento de cambios, para modificarlo y
    /// persistirlo con GuardarCambiosAsync.
    /// </summary>
    Task<Contrato?> ObtenerParaActualizarAsync(Guid id, CancellationToken cancellationToken);

    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
