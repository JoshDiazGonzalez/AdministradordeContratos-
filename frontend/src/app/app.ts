import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { DialogoConfirmacion } from './shared/ui/dialogo-confirmacion/dialogo-confirmacion';
import { Notificaciones } from './shared/ui/notificaciones/notificaciones';

/**
 * Raiz de la aplicacion. Los avisos y el dialogo de confirmacion viven aqui, una
 * sola vez, para que sobrevivan a la navegacion entre pantallas.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Notificaciones, DialogoConfirmacion],
  template: `
    <router-outlet />
    <app-notificaciones />
    <app-dialogo-confirmacion />
  `,
})
export class App {}
