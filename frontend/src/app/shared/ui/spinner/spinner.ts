import { Component, input } from '@angular/core';

/**
 * Indicador de carga.
 *
 * Anuncia el texto a lectores de pantalla con role="status": sin esto, una
 * persona ciega no sabria que la pantalla esta esperando una respuesta.
 */
@Component({
  selector: 'app-spinner',
  templateUrl: './spinner.html',
  styleUrl: './spinner.css',
})
export class Spinner {
  /** Texto accesible. Por defecto solo lo leen los lectores de pantalla. */
  readonly etiqueta = input('Cargando');

  /** Muestra el texto junto al indicador. */
  readonly mostrarTexto = input(false);

  readonly tamano = input<'sm' | 'md'>('md');
}
