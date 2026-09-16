import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';

import { DialogoConfirmacionService } from './dialogo-confirmacion.service';

/**
 * Dialogo modal de confirmacion. Se coloca una sola vez en la raiz de la aplicacion.
 *
 * Usa el elemento nativo <dialog> con showModal(): el navegador ya resuelve lo
 * dificil de un modal accesible (bloquear el resto de la pagina, atrapar el
 * foco, cerrar con Escape y devolver el foco al cerrar).
 */
@Component({
  selector: 'app-dialogo-confirmacion',
  templateUrl: './dialogo-confirmacion.html',
  styleUrl: './dialogo-confirmacion.css',
})
export class DialogoConfirmacion {
  protected readonly servicio = inject(DialogoConfirmacionService);

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');
  private readonly botonCancelar = viewChild<ElementRef<HTMLButtonElement>>('cancelar');

  constructor() {
    effect(() => {
      const elemento = this.dialogo().nativeElement;
      const abierta = this.servicio.solicitud() !== null;

      if (abierta && !elemento.open) {
        elemento.showModal();
        // El foco inicial va a "cancelar": ante una accion que descarta datos,
        // pulsar Enter por inercia no debe ejecutarla.
        queueMicrotask(() => this.botonCancelar()?.nativeElement.focus());
      } else if (!abierta && elemento.open) {
        elemento.close();
      }
    });
  }

  protected responder(confirmado: boolean): void {
    this.servicio.solicitud()?.responder(confirmado);
  }

  /** Escape dispara "cancel": equivale a responder que no. */
  protected alCancelar(evento: Event): void {
    evento.preventDefault();
    this.responder(false);
  }

  /** Clic en el fondo oscuro (fuera del contenido) cierra como cancelar. */
  protected alPulsarFondo(evento: MouseEvent): void {
    if (evento.target === this.dialogo().nativeElement) {
      this.responder(false);
    }
  }
}
