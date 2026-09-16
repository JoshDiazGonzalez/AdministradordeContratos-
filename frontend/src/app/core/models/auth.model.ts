/** Credenciales enviadas a POST /api/auth/login. */
export interface LoginRequest {
  username: string;
  password: string;
}

/**
 * Datos publicos del usuario. La API envia deliberadamente lo minimo para
 * pintar la interfaz; nunca llega informacion sensible.
 */
export interface UsuarioAutenticado {
  username: string;
  nombreCompleto: string;
}

/** Respuesta de un login correcto. */
export interface LoginResponse {
  token: string;
  /** Instante ISO 8601 en que expira el token. */
  expiresAt: string;
  user: UsuarioAutenticado;
}
