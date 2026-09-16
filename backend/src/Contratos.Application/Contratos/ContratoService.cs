using Contratos.Application.Common;
using Contratos.Domain.Services;
using FluentValidation;

namespace Contratos.Application.Contratos;

public interface IContratoService
{
    Task<ResultadoPaginado<ContratoDto>> BuscarAsync(
        ContratoFiltro filtro, CancellationToken cancellationToken);

    Task<ContratoDto> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ContratoResumenDto> ResumirAsync(CancellationToken cancellationToken);
}

public class ContratoService : IContratoService
{
    private readonly IContratoRepository _repositorio;
    private readonly IDateTimeProvider _reloj;
    private readonly IValidator<ContratoFiltro> _validator;

    public ContratoService(
        IContratoRepository repositorio,
        IDateTimeProvider reloj,
        IValidator<ContratoFiltro> validator)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _validator = validator;
    }

    public async Task<ResultadoPaginado<ContratoDto>> BuscarAsync(
        ContratoFiltro filtro,
        CancellationToken cancellationToken)
    {
        var validacion = await _validator.ValidateAsync(filtro, cancellationToken);
        if (!validacion.IsValid)
        {
            throw new ValidacionException(validacion.ToDictionary());
        }

        // "Hoy" se resuelve una sola vez por peticion: si se recalculara por fila,
        // una consulta que cruzara la medianoche podria clasificar dos contratos
        // identicos de forma distinta.
        var hoy = _reloj.Hoy;

        var pagina = await _repositorio.BuscarAsync(filtro, hoy, cancellationToken);

        return new ResultadoPaginado<ContratoDto>(
            pagina.Items.Select(c => ContratoDto.Desde(c, hoy)).ToList(),
            pagina.Page,
            pagina.PageSize,
            pagina.TotalItems);
    }

    public async Task<ContratoDto> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var contrato = await _repositorio.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNoEncontradoException($"No existe un contrato con id {id}.");

        return ContratoDto.Desde(contrato, _reloj.Hoy);
    }

    public Task<ContratoResumenDto> ResumirAsync(CancellationToken cancellationToken) =>
        _repositorio.ResumirAsync(_reloj.Hoy, cancellationToken);
}
