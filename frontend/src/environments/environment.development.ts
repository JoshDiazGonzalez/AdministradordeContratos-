/**
 * Configuracion de desarrollo (ng serve).
 *
 * Apunta a la API .NET local. El backend permite este origen mediante CORS
 * (Cors__FrontendUrl=http://localhost:4200).
 */
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5080/api',
};
