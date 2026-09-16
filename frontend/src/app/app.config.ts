import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

/**
 * Configuracion raiz de la aplicacion.
 *
 * Angular 21 es zoneless por defecto: la deteccion de cambios la dirigen los
 * signals, sin zone.js. Por eso el estado de las pantallas se modela con signals.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // withComponentInputBinding: los parametros de ruta (:id) llegan a los
    // componentes como inputs, sin inyectar ActivatedRoute.
    provideRouter(routes, withComponentInputBinding()),
    // withFetch usa la API fetch del navegador en lugar de XMLHttpRequest.
    // authInterceptor anade el JWT y cierra la sesion si la API responde 401.
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
  ],
};
