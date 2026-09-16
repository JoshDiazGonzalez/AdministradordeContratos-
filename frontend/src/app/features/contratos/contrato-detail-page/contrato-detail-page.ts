import { Component, input } from '@angular/core';

@Component({
  selector: 'app-contrato-detail-page',
  imports: [],
  templateUrl: './contrato-detail-page.html',
  styleUrl: './contrato-detail-page.css',
})
export class ContratoDetailPage {
  /**
   * Identificador tomado de la ruta /contratos/:id.
   * Llega como input gracias a withComponentInputBinding() en app.config.ts.
   */
  readonly id = input.required<string>();
}
