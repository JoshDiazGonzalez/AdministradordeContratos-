import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { API_URL } from '../services/api-url.token';
import { AuthService } from '../services/auth.service';

/**
 * Anade el token JWT a las peticiones hacia la API y reacciona a un 401.
 *
 * El token solo se envia a la API propia: si algun dia se llama a un servicio
 * externo, adjuntarle la cabecera Authorization le entregaria la credencial.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const apiUrl = inject(API_URL);

  if (!esPeticionALaApi(request.url, apiUrl)) {
    return next(request);
  }

  const token = auth.token();
  const peticion = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(peticion).pipe(
    catchError((error: unknown) => {
      // Un 401 en el propio login significa credenciales incorrectas: lo
      // gestiona la pantalla. En cualquier otro endpoint significa que el
      // token dejo de ser valido (expirado, API reiniciada con otro secreto...).
      const sesionRechazada =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !request.url.endsWith('/auth/login');

      if (sesionRechazada) {
        auth.cerrarSesion('no-autorizado', router.url);
      }

      return throwError(() => error);
    }),
  );
};

function esPeticionALaApi(url: string, apiUrl: string): boolean {
  // apiUrl puede ser absoluta (desarrollo) o relativa, "/api" (produccion).
  // Se exige el separador para que "/api" no coincida con "/apiextra".
  return url === apiUrl || url.startsWith(`${apiUrl}/`);
}
