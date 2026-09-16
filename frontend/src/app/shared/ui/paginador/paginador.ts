import { Component, computed, input, output } from '@angular/core';

/**
 * Navegacion entre paginas de un listado.
 *
 * No conoce la API ni el router: recibe el estado y emite la pagina o el
 * tamano elegidos. Quien lo usa decide que hacer (en el listado, actualizar la
 * URL para que la pagina sobreviva a recargar y al boton atras).
 */
@Component({
  selector: 'app-paginador',
  templateUrl: './paginador.html',
  styleUrl: './paginador.css',
})
export class Paginador {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalItems = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly opcionesTamano = input<readonly number[]>([10, 20, 50]);
  /** Nombre de lo que se lista, en plural, para el resumen ("contratos"). */
  readonly elementos = input('resultados');
  readonly deshabilitado = input(false);

  readonly cambioPagina = output<number>();
  readonly cambioTamano = output<number>();

  protected readonly desde = computed(() =>
    this.totalItems() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );

  protected readonly hasta = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalItems()),
  );

  protected readonly hayAnterior = computed(() => this.page() > 1);
  protected readonly haySiguiente = computed(() => this.page() < this.totalPages());

  protected irA(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPages() && pagina !== this.page()) {
      this.cambioPagina.emit(pagina);
    }
  }

  protected cambiarTamano(evento: Event): void {
    const valor = Number((evento.target as HTMLSelectElement).value);
    if (this.opcionesTamano().includes(valor)) {
      this.cambioTamano.emit(valor);
    }
  }
}
