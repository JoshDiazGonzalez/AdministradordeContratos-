import { Injectable, signal } from '@angular/core';

export type TipoNotificacion = 'exito' | 'error';

export interface Notificacion {
  readonly id: number;
  readonly tipo: TipoNotificacion;
  readonly mensaje: string;
}

/** Tiempo visible de un aviso de exito. Los errores esperan a que se cierren. */
export const DURACION_EXITO_MS = 5000;

/**
 * Avisos breves que sobreviven a la navegacion.
 *
 * Ejemplo: al guardar un contrato se vuelve al listado; el mensaje "Contrato
 * registrado" debe verse en la pantalla de destino, no en el formulario que se
 * acaba de cerrar.
 */
@Injectable({ providedIn: 'root' })
export class NotificacionesService {
  private siguienteId = 1;
  private readonly temporizadores = new Map<number, ReturnType<typeof setTimeout>>();

  readonly notificaciones = signal<readonly Notificacion[]>([]);

  exito(mensaje: string): void {
    const id = this.agregar('exito', mensaje);
    this.temporizadores.set(id, setTimeout(() => this.cerrar(id), DURACION_EXITO_MS));
  }

  /**
   * Los errores no desaparecen solos: quien lee despacio o usa lector de
   * pantalla perderia el mensaje antes de entender que salio mal.
   */
  error(mensaje: string): void {
    this.agregar('error', mensaje);
  }

  cerrar(id: number): void {
    clearTimeout(this.temporizadores.get(id));
    this.temporizadores.delete(id);
    this.notificaciones.update((lista) => lista.filter((n) => n.id !== id));
  }

  private agregar(tipo: TipoNotificacion, mensaje: string): number {
    const id = this.siguienteId++;
    // Se conservan como mucho tres avisos: una cola larga taparia la pantalla.
    this.notificaciones.update((lista) => [...lista, { id, tipo, mensaje }].slice(-3));
    return id;
  }
}
