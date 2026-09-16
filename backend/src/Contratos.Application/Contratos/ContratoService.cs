using Contratos.Application.Common;
using Contratos.Application.Storage;
using Contratos.Domain.Entities;
using Contratos.Domain.Services;
using FluentValidation;

namespace Contratos.Application.Contratos;

public interface IContratoService
{
    Task<ResultadoPaginado<ContratoDto>> BuscarAsync(
        ContratoFiltro filtro, CancellationToken cancellationToken);

    Task<ContratoDto> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ContratoResumenDto> ResumirAsync(CancellationToken cancellationToken);

    Task<ContratoDto> CrearAsync(CrearContratoRequest request, CancellationToken cancellationToken);

    Task<ArchivoDescargado> ObtenerArchivoAsync(Guid id, CancellationToken cancellationToken);
}

public class ContratoService : IContratoService
{
    private readonly IContratoRepository _repositorio;
    private readonly IDateTimeProvider _reloj;
    private readonly IValidator<ContratoFiltro> _validator;
    private readonly IValidator<CrearContratoRequest> _validatorCreacion;
    private readonly IFileStorageService _almacenamiento;
    private readonly ValidadorDeArchivo _validadorArchivo;

    public ContratoService(
        IContratoRepository repositorio,
        IDateTimeProvider reloj,
        IValidator<ContratoFiltro> validator,
        IValidator<CrearContratoRequest> validatorCreacion,
        IFileStorageService almacenamiento,
        ValidadorDeArchivo validadorArchivo)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _validator = validator;
        _validatorCreacion = validatorCreacion;
        _almacenamiento = almacenamiento;
        _validadorArchivo = validadorArchivo;
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

    public async Task<ContratoDto> CrearAsync(
        CrearContratoRequest request,
        CancellationToken cancellationToken)
    {
        var validacion = await _validatorCreacion.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            throw new ValidacionException(validacion.ToDictionary());
        }

        // El archivo se valida despues de los campos: si los datos son invalidos
        // no tiene sentido leer el contenido del documento.
        await _validadorArchivo.ValidarAsync(request.Archivo, cancellationToken);
        var archivo = request.Archivo!;

        var ahora = _reloj.AhoraUtc;

        // Primero se guarda el documento: si fallara despues la insercion en base,
        // quedaria un archivo huerfano (recuperable). El orden inverso dejaria una
        // fila apuntando a un archivo inexistente, que si rompe la descarga.
        var ruta = await _almacenamiento.GuardarAsync(archivo, cancellationToken);

        try
        {
            var contrato = Contrato.Crear(
                request.NombreProveedor,
                request.MontoContrato,
                request.FechaInicio,
                request.FechaVencimiento,
                request.Descripcion,
                Path.GetFileName(archivo.NombreOriginal),
                ruta,
                archivo.ContentType,
                archivo.TamanoBytes,
                ahora);

            await _repositorio.AgregarAsync(contrato, cancellationToken);

            return ContratoDto.Desde(contrato, _reloj.Hoy);
        }
        catch
        {
            // Se limpia el archivo recien subido para no dejar basura acumulada.
            await _almacenamiento.EliminarAsync(ruta, CancellationToken.None);
            throw;
        }
    }

    public async Task<ArchivoDescargado> ObtenerArchivoAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var contrato = await _repositorio.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNoEncontradoException($"No existe un contrato con id {id}.");

        var contenido = await _almacenamiento.AbrirAsync(contrato.ArchivoRuta, cancellationToken)
            ?? throw new RecursoNoEncontradoException(
                "El documento del contrato no esta disponible en el almacenamiento.");

        return new ArchivoDescargado(contenido, contrato.ArchivoContentType, contrato.ArchivoNombre);
    }
}
