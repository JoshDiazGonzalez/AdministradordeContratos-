namespace Contratos.Application.Auth;

/// <summary>Respuesta de un login correcto.</summary>
public record LoginResponse(string Token, DateTimeOffset ExpiresAt, UsuarioAutenticado User);

/// <summary>
/// Datos publicos del usuario. Deliberadamente minimo: no se envia al navegador
/// nada que no haga falta para pintar la interfaz.
/// </summary>
public record UsuarioAutenticado(string Username, string NombreCompleto);
