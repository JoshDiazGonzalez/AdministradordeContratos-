import { InjectionToken } from '@angular/core';

import { environment } from '../../../environments/environment';

/**
 * URL base de la API.
 *
 * Se inyecta en lugar de importar environment en cada servicio: asi los tests
 * pueden sustituirla sin depender del archivo de entorno.
 */
export const API_URL = new InjectionToken<string>('API_URL', {
  providedIn: 'root',
  factory: () => environment.apiUrl,
});
