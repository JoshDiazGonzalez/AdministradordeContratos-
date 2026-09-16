import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, filter, map, merge } from 'rxjs';

import {
  CONTRATO_ESTADOS,
  CONTRATO_ESTADO_ETIQUETAS,
  ContratoEstado,
} from '../../../core/models/contrato.model';
import { CriteriosBusqueda, contarCriterios, errorDeRangos, mismosCriterios } from '../contratos-filtro-url';

/** Espera tras la ultima tecla antes de buscar por proveedor. */
export const PAUSA_BUSQUEDA_MS = 350;

/**
 * Panel de filtros del listado.
 *
 * No consulta la API ni conoce el router: recibe los criterios actuales y emite
 * los nuevos. La pantalla los lleva a la URL, que es la unica fuente de verdad.
 */
@Component({
  selector: 'app-contratos-filtros',
  imports: [ReactiveFormsModule],
  templateUrl: './contratos-filtros.html',
  styleUrl: './contratos-filtros.css',
})
export class ContratosFiltros {
  /** Criterios aplicados, leidos de la URL. */
  readonly criterios = input.required<CriteriosBusqueda>();

  readonly cambio = output<CriteriosBusqueda>();

  protected readonly estados = CONTRATO_ESTADOS;
  protected readonly etiquetas = CONTRATO_ESTADO_ETIQUETAS;

  protected readonly formulario = inject(NonNullableFormBuilder).group({
    proveedor: [''],
    estado: ['' as ContratoEstado | ''],
    fechaInicioDesde: [''],
    fechaInicioHasta: [''],
    fechaVencimientoDesde: [''],
    fechaVencimientoHasta: [''],
  });

  /** Panel desplegable en movil. En escritorio siempre esta visible. */
  protected readonly abierto = signal(false);

  /**
   * Copia del valor del formulario en un signal.
   *
   * No se usa toSignal(valueChanges): las actualizaciones silenciosas
   * (emitEvent:false, al sincronizar con la URL o al limpiar) no emiten, y el
   * mensaje de rango invertido quedaria desfasado. Se actualiza a mano en esos
   * casos.
   */
  private readonly valor = signal(this.formulario.getRawValue());

  protected readonly errorRango = computed(() => errorDeRangos(aCriterios(this.valor())));
  protected readonly cantidadActivos = computed(() => contarCriterios(this.criterios()));

  /**
   * Hay algo que limpiar si hay filtros aplicados o si se escribio algo que aun
   * no se aplico (por ejemplo, durante la pausa de la busqueda por proveedor).
   */
  protected readonly hayQueLimpiar = computed(
    () => this.cantidadActivos() > 0 || contarCriterios(aCriterios(this.valor())) > 0,
  );

  constructor() {
    // Sincroniza el formulario cuando la URL cambia desde fuera (boton atras,
    // enlace compartido). emitEvent:false evita un bucle formulario -> URL.
    effect(() => {
      const criterios = this.criterios();
      const actual = this.formulario.getRawValue();

      // Tipo explicito: en un literal mutable, TypeScript ampliaria
      // ContratoEstado | '' a string y setValue lo rechazaria.
      const siguiente: typeof actual = {
        // No se pisa lo que el usuario esta escribiendo: si solo difiere en
        // espacios al final ("beta " frente a "beta"), se conserva el campo.
        proveedor:
          actual.proveedor.trim() === (criterios.proveedor ?? '')
            ? actual.proveedor
            : (criterios.proveedor ?? ''),
        estado: criterios.estado ?? '',
        fechaInicioDesde: criterios.fechaInicioDesde ?? '',
        fechaInicioHasta: criterios.fechaInicioHasta ?? '',
        fechaVencimientoDesde: criterios.fechaVencimientoDesde ?? '',
        fechaVencimientoHasta: criterios.fechaVencimientoHasta ?? '',
      };

      if (JSON.stringify(siguiente) !== JSON.stringify(actual)) {
        this.formulario.setValue(siguiente, { emitEvent: false });
        this.valor.set(siguiente);
      }
    });

    this.formulario.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.valor.set(this.formulario.getRawValue()));

    const controles = this.formulario.controls;

    merge(
      // El texto espera a que se deje de escribir: sin la pausa, cada tecla
      // lanzaria una consulta a la API.
      controles.proveedor.valueChanges.pipe(debounceTime(PAUSA_BUSQUEDA_MS)),
      // Selectores y fechas se aplican al instante.
      controles.estado.valueChanges,
      controles.fechaInicioDesde.valueChanges,
      controles.fechaInicioHasta.valueChanges,
      controles.fechaVencimientoDesde.valueChanges,
      controles.fechaVencimientoHasta.valueChanges,
    )
      .pipe(
        // Se lee el formulario completo en el momento de emitir, no el valor que
        // disparo el evento: una busqueda pendiente usa siempre lo ultimo escrito.
        map(() => aCriterios(this.formulario.getRawValue())),
        // Un rango invertido no se envia: la API lo rechazaria.
        filter((criterios) => errorDeRangos(criterios) === null),
        // Se compara con lo que ya esta aplicado en la URL, no con lo ultimo
        // emitido: si la URL cambio desde fuera (boton atras) y el usuario vuelve
        // a escribir la busqueda anterior, debe aplicarse de nuevo.
        filter((criterios) => !mismosCriterios(criterios, this.criterios())),
        takeUntilDestroyed(),
      )
      .subscribe((criterios) => this.cambio.emit(criterios));
  }

  protected alternar(): void {
    this.abierto.update((abierto) => !abierto);
  }

  protected limpiar(): void {
    this.formulario.reset(
      {
        proveedor: '',
        estado: '',
        fechaInicioDesde: '',
        fechaInicioHasta: '',
        fechaVencimientoDesde: '',
        fechaVencimientoHasta: '',
      },
      { emitEvent: false },
    );
    this.valor.set(this.formulario.getRawValue());
    this.cambio.emit({});
  }
}

/** Valores del formulario a criterios, sin campos vacios. */
function aCriterios(valor: Partial<Record<string, string>>): CriteriosBusqueda {
  const criterios: CriteriosBusqueda = {};

  const proveedor = valor['proveedor']?.trim();
  if (proveedor) criterios.proveedor = proveedor;
  if (valor['estado']) criterios.estado = valor['estado'] as ContratoEstado;
  if (valor['fechaInicioDesde']) criterios.fechaInicioDesde = valor['fechaInicioDesde'];
  if (valor['fechaInicioHasta']) criterios.fechaInicioHasta = valor['fechaInicioHasta'];
  if (valor['fechaVencimientoDesde']) criterios.fechaVencimientoDesde = valor['fechaVencimientoDesde'];
  if (valor['fechaVencimientoHasta']) criterios.fechaVencimientoHasta = valor['fechaVencimientoHasta'];

  return criterios;
}
