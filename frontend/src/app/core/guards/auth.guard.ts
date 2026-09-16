import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { RUTA_INICIO } from '../services/return-url';

/**
 * Protege las pantallas que requieren sesion.
 *
 * Si no hay sesion, envia al login recordando la pagina pedida para volver a
 * ella despues. Es una comodidad de navegacion, no la barrera de seguridad:
 * la API rechaza con 401 cualquier peticion sin un token valido.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.token()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/** Evita mostrar el login a quien ya tiene la sesion iniciada. */
export const invitadoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.token() ? router.parseUrl(RUTA_INICIO) : true;
};
