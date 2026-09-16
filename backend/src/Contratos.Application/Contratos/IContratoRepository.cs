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
}
