namespace Contratos.Application.Common;

/// <summary>Credenciales invalidas. Se traduce a HTTP 401.</summary>
public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException()
        : base("Usuario o contraseña incorrectos.")
    {
    }
}

/// <summary>Recurso inexistente. Se traduce a HTTP 404.</summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje)
        : base(mensaje)
    {
    }
}

/// <summary>Entrada invalida. Se traduce a HTTP 400 con el detalle por campo.</summary>
public class ValidacionException : Exception
{
    public ValidacionException(IDictionary<string, string[]> errores)
        : base("Se encontraron errores de validación.")
    {
        Errores = errores;
    }

    public IDictionary<string, string[]> Errores { get; }
}
