/**
 * Error estandar de la API (RFC 9457, ProblemDetails).
 *
 * El middleware de errores del backend responde siempre con esta forma, asi
 * que el frontend puede mostrar mensajes sin conocer cada endpoint.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  /** Errores de validacion por campo (solo en respuestas 400). */
  errors?: Record<string, string[]>;
}
