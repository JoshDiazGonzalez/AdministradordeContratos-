namespace Contratos.Application.Contratos;

/// <summary>Conteo por estado para el dashboard.</summary>
public record ContratoResumenDto(
    int Total,
    int Activos,
    int PorVencer,
    int Vencidos,
    int Inactivos);
