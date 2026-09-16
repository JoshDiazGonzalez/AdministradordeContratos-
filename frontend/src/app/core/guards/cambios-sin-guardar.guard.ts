import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';

import { DialogoConfirmacionService } from '../../shared/ui/dialogo-confirmacion/dialogo-confirmacion.service';

/** Pantallas que pueden perder datos escritos por el usuario. */
export interface ConCambiosSinGuardar {
  tieneCambiosSinGuardar(): boolean;
}

/**
 * Pide confirmacion antes de abandonar un formulario con datos escritos.
 *
 * Rellenar un contrato lleva tiempo; un clic accidental en el menu no deberia
 * descartarlo sin avisar.
 */
export const cambiosSinGuardarGuard: CanDeactivateFn<ConCambiosSinGuardar> = (componente) => {
  if (!componente.tieneCambiosSinGuardar()) {
    return true;
  }

  return inject(DialogoConfirmacionService).confirmar({
    titulo: '¿Salir sin guardar?',
    mensaje: 'Los datos del contrato que escribió se perderán.',
    confirmar: 'Salir sin guardar',
    cancelar: 'Seguir editando',
    peligroso: true,
  });
};
