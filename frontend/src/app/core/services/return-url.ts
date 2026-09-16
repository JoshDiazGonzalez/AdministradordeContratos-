/** Destino por defecto tras iniciar sesion. */
export const RUTA_INICIO = '/contratos';

/**
 * Devuelve una ruta interna segura a la que volver tras iniciar sesion.
 *
 * returnUrl llega en la URL, asi que lo controla cualquiera que envie un enlace.
 * Sin esta comprobacion, un enlace como
 *   /login?returnUrl=https://sitio-falso.com
 * llevaria al usuario, recien autenticado y confiado, a una pagina externa que
 * imita al sistema (redireccion abierta).
 *
 * Solo se aceptan rutas relativas al propio sitio.
 */
export function returnUrlSegura(valor: string | null | undefined): string {
  if (!valor) {
    return RUTA_INICIO;
  }

  const ruta = valor.trim();

  const esInterna =
    ruta.startsWith('/') &&
    // "//dominio.com" es una URL externa relativa al protocolo.
    !ruta.startsWith('//') &&
    // Los navegadores tratan "\" como "/": "/\dominio.com" tambien sale del sitio.
    !ruta.includes('\\') &&
    // Evita volver al propio login y quedar en un bucle.
    !ruta.startsWith('/login');

  return esInterna ? ruta : RUTA_INICIO;
}
