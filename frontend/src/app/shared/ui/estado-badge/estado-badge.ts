import { Component, computed, input } from '@angular/core';

import { CONTRATO_ESTADO_ETIQUETAS, ContratoEstado } from '../../../core/models/contrato.model';

/**
 * Indicador del estado de vigencia de un contrato.
 *
 * Siempre muestra el texto del estado ademas del color: una persona con
 * dificultad para distinguir verde de rojo tiene que poder leerlo igual.
 * El punto de color es un refuerzo visual, no la informacion.
 */
@Component({
  selector: 'app-estado-badge',
  templateUrl: './estado-badge.html',
  styleUrl: './estado-badge.css',
  host: {
    '[class]': 'claseEstado()',
  },
})
export class EstadoBadge {
  readonly estado = input.required<ContratoEstado>();

  protected readonly etiqueta = computed(() => CONTRATO_ESTADO_ETIQUETAS[this.estado()]);

  protected readonly claseEstado = computed(() => `badge badge-${this.estado()}`);
}
