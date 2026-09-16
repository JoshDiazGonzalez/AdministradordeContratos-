import { Injectable, signal } from '@angular/core';

export interface OpcionesConfirmacion {
  titulo: string;
  mensaje: string;
  /** Texto del boton que confirma. Debe describir la accion ("Desactivar"), no "Aceptar". */
  confirmar: string;
  cancelar?: string;
  /** Muestra la accion con estilo de peligro (descartar datos, desactivar...). */
  peligroso?: boolean;
}

export interface SolicitudConfirmacion extends OpcionesConfirmacion {
  responder(confirmado: boolean): void;
}

/**
 * Pide confirmacion al usuario con un dialogo propio en lugar de window.confirm,
 * que no se puede adaptar a la identidad visual ni a los textos del sistema.
 *
 * Se usa como una promesa: const ok = await dialogo.confirmar({...}).
 */
@Injectable({ providedIn: 'root' })
export class DialogoConfirmacionService {
  readonly solicitud = signal<SolicitudConfirmacion | null>(null);

  confirmar(opciones: OpcionesConfirmacion): Promise<boolean> {
    // Si ya habia un dialogo abierto se cancela: nunca quedan dos promesas
    // esperando una respuesta que no llegara.
    this.solicitud()?.responder(false);

    return new Promise<boolean>((resolver) => {
      this.solicitud.set({
        ...opciones,
        responder: (confirmado) => {
          this.solicitud.set(null);
          resolver(confirmado);
        },
      });
    });
  }
}
