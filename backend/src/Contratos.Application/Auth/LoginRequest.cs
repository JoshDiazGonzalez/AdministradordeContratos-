namespace Contratos.Application.Auth;

/// <summary>Credenciales enviadas al endpoint de login.</summary>
public record LoginRequest(string Username, string Password);
