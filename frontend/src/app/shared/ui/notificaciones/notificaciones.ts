import { Component, inject } from '@angular/core';

import { NotificacionesService } from '../../../core/services/notificaciones.service';

/** Pila de avisos en la esquina de la pantalla. Se coloca una vez en la raiz. */
@Component({
  selector: 'app-notificaciones',
  templateUrl: './notificaciones.html',
  styleUrl: './notificaciones.css',
})
export class Notificaciones {
  protected readonly servicio = inject(NotificacionesService);
}
