namespace Contratos.Application.Contratos;

/// <summary>
/// Activa o desactiva un contrato por decision de negocio.
///
/// Solo se expone la bandera "inactivo": los estados Activo, PorVencer y Vencido
/// se calculan a partir de las fechas y no pueden fijarse a mano. Permitir
/// "estado = Activo" dejaria marcar como vigente un contrato ya vencido.
/// </summary>
public record CambiarEstadoRequest(bool Inactivo);
