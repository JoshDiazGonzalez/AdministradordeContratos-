/**
 * Configuracion de produccion.
 *
 * La URL de la API es relativa: en Docker el frontend y la API se publican
 * detras del mismo origen (nginx reenvia /api al backend), asi que no hay que
 * reconstruir la imagen para cambiar de dominio y se evita CORS.
 */
export const environment = {
  production: true,
  apiUrl: '/api',
};
